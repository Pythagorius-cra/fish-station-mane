using System.Linq;
using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Popups;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Fish.Shipyard;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Station.Components;
using Content.Shared.UserInterface;
using Robust.Server.Audio;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._Fish.Shipyard;

/// <summary>
/// Серверная логика консоли верфи: каталог, оплата со счёта станции, спавн и стыковка.
/// </summary>
public sealed partial class ShipyardSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private CargoSystem _cargo = default!;
    [Dependency] private DockingSystem _docking = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private ShuttleSystem _shuttles = default!;
    [Dependency] private StationSystem _station = default!;

    private ISawmill _sawmill = default!;
    private float _spawnIndex;

    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("fish-shipyard");

        Subs.BuiEvents<ShipyardConsoleComponent>(ShipyardConsoleUiKey.Shipyard, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<ShipyardConsolePurchaseMessage>(OnPurchaseMessage);
        });
    }

    private void OnUiOpened(Entity<ShipyardConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnPurchaseMessage(Entity<ShipyardConsoleComponent> ent, ref ShipyardConsolePurchaseMessage args)
    {
        var actor = args.Actor;
        if (!Exists(actor))
            return;

        if (ent.Comp.Purchasing)
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-busy"));
            return;
        }

        if (TryComp(ent, out Content.Shared.Access.Components.AccessReaderComponent? accessReader) &&
            !_access.IsAllowed(actor, ent, accessReader))
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-access-denied"));
            return;
        }

        if (!_prototypes.TryIndex<VesselPrototype>(args.VesselId, out var vessel))
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-invalid-vessel"));
            return;
        }

        if (!string.Equals(vessel.Group, ent.Comp.VesselGroup, StringComparison.OrdinalIgnoreCase))
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-invalid-vessel"));
            _adminLog.Add(LogType.Action, LogImpact.Medium,
                $"{ToPrettyString(actor)} tried to buy vessel {vessel.ID} not listed on {ToPrettyString(ent)}");
            return;
        }

        if (vessel.Price <= 0 || vessel.ShuttlePath == ResPath.Empty)
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-invalid-vessel"));
            return;
        }

        var station = _station.GetOwningStation(ent);
        if (station is not { } stationUid ||
            !TryComp<StationDataComponent>(stationUid, out var stationData) ||
            !TryComp<StationBankAccountComponent>(stationUid, out var bank))
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-no-station"));
            return;
        }

        var targetGrid = _station.GetLargestGrid((stationUid, stationData));
        if (targetGrid == null)
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-no-station"));
            return;
        }

        var balance = _cargo.GetBalanceFromAccount((stationUid, bank), ent.Comp.Account);
        if (balance < vessel.Price)
        {
            Deny(ent, actor, Loc.GetString("shipyard-console-insufficient-funds", ("cost", vessel.Price)));
            return;
        }

        ent.Comp.Purchasing = true;
        UpdateUi(ent, Loc.GetString("shipyard-console-purchasing"));

        try
        {
            if (!TrySpawnAndDockShuttle(vessel, targetGrid.Value, ent.Comp.DefaultPriorityDockTag, out var shuttle, out var failReason))
            {
                Deny(ent, actor, failReason ?? Loc.GetString("shipyard-console-spawn-failed"));
                return;
            }

            // Списываем только после успешного спавна и стыковки.
            _cargo.UpdateBankAccount((stationUid, bank), -vessel.Price, ent.Comp.Account);

            var vesselName = Loc.GetString(vessel.Name);
            _metaData.SetEntityName(shuttle, vesselName);

            _adminLog.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(actor)} purchased vessel {vessel.ID} ({vesselName}) for {vessel.Price} via {ToPrettyString(ent)}; shuttle={ToPrettyString(shuttle)}");

            _audio.PlayPvs(ent.Comp.ApproveSound, ent);
            _popup.PopupEntity(Loc.GetString("shipyard-console-purchase-success", ("vessel", vesselName)), actor, actor);

            UpdateUi(ent, Loc.GetString("shipyard-console-purchase-success", ("vessel", vesselName)));
        }
        finally
        {
            ent.Comp.Purchasing = false;
            // Обновляем баланс после сброса флага.
            if (_ui.IsUiOpen(ent.Owner, ShipyardConsoleUiKey.Shipyard))
                UpdateUi(ent);
        }
    }

    /// <summary>
    /// Загружает шаттл на FTL-карту и мгновенно стыкует к станции по свободному доку.
    /// </summary>
    private bool TrySpawnAndDockShuttle(
        VesselPrototype vessel,
        EntityUid targetGrid,
        string? defaultDockTag,
        out EntityUid shuttleUid,
        out string? failReason)
    {
        shuttleUid = EntityUid.Invalid;
        failReason = null;

        var ftlMap = _shuttles.EnsureSunriseFtlMap();
        var xformMap = Transform(ftlMap);

        if (!_loader.TryLoadGrid(xformMap.MapID, vessel.ShuttlePath, out var loaded) || loaded == null)
        {
            _sawmill.Error($"Failed to load shipyard shuttle {vessel.ShuttlePath}");
            failReason = Loc.GetString("shipyard-console-spawn-failed");
            return false;
        }

        var shuttle = loaded.Value.Owner;
        if (!HasComp<ShuttleComponent>(shuttle) ||
            !TryComp<MapGridComponent>(shuttle, out var grid))
        {
            QueueDel(shuttle);
            failReason = Loc.GetString("shipyard-console-spawn-failed");
            return false;
        }

        var width = grid.LocalAABB.Width;
        var shuttleCenter = grid.LocalAABB.Center;
        var coordinates = new EntityCoordinates(ftlMap, new Vector2(500f + _spawnIndex + width / 2f, 1f) - shuttleCenter);
        _transform.SetCoordinates(shuttle, coordinates);
        _spawnIndex += width + 5f;

        var priorityTag = vessel.PriorityDockTag ?? defaultDockTag;
        var config = !string.IsNullOrEmpty(priorityTag)
            ? _docking.GetDockingConfig(shuttle, targetGrid, priorityTag)
            : null;

        // Если приоритетный док занят — любой свободный стыковочный порт станции.
        config ??= _docking.GetDockingConfig(shuttle, targetGrid);

        if (config == null)
        {
            QueueDel(shuttle);
            failReason = Loc.GetString("shipyard-console-no-dock");
            return false;
        }

        _shuttles.FTLDock((shuttle, Transform(shuttle)), config);

        shuttleUid = shuttle;
        return true;
    }

    private void Deny(Entity<ShipyardConsoleComponent> ent, EntityUid actor, string message)
    {
        _audio.PlayPvs(ent.Comp.DenySound, ent);
        _popup.PopupEntity(message, actor, actor);
        UpdateUi(ent, message);
    }

    private void UpdateUi(Entity<ShipyardConsoleComponent> ent, string? status = null)
    {
        var balance = 0;
        var station = _station.GetOwningStation(ent);
        if (station is { } stationUid && TryComp<StationBankAccountComponent>(stationUid, out var bank))
            balance = _cargo.GetBalanceFromAccount((stationUid, bank), ent.Comp.Account);

        var vessels = new List<ShipyardVesselEntry>();
        foreach (var vessel in _prototypes.EnumeratePrototypes<VesselPrototype>()
                     .Where(v => string.Equals(v.Group, ent.Comp.VesselGroup, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(v => Loc.GetString(v.Name), StringComparer.CurrentCultureIgnoreCase))
        {
            var name = Loc.GetString(vessel.Name);
            var description = string.IsNullOrWhiteSpace(vessel.Description)
                ? string.Empty
                : Loc.GetString(vessel.Description);

            string? unavailable = null;
            var canPurchase = !ent.Comp.Purchasing;
            if (vessel.Price > balance)
            {
                canPurchase = false;
                unavailable = Loc.GetString("shipyard-console-insufficient-funds", ("cost", vessel.Price));
            }

            vessels.Add(new ShipyardVesselEntry(
                vessel.ID,
                name,
                description,
                vessel.Price,
                canPurchase,
                unavailable));
        }

        _ui.SetUiState(ent.Owner, ShipyardConsoleUiKey.Shipyard,
            new ShipyardConsoleBoundUserInterfaceState(balance, status, ent.Comp.Purchasing, vessels));
    }
}
