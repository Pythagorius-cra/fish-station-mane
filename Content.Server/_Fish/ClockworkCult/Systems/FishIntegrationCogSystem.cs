using Content.Server.Power.Components;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Integration Cog: РІСЃС‚Р°РІРєР° РІ APC Рё РїР°СЃСЃРёРІРЅС‹Р№ drain ~5 W/s РІ РїСѓР» РєСѓР»СЊС‚Р°.
/// </summary>
public sealed partial class FishIntegrationCogSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private FishClockworkPowerSystem _power = default!;
    [Dependency] private FishClockworkServantSystem _servant = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FishIntegrationCogComponent, AfterInteractEvent>(OnAfterInteract);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FishIntegrationCogComponent>();
        while (query.MoveNext(out var uid, out var cog))
        {
            if (!cog.Installed || cog.HostApc == null || !Exists(cog.HostApc.Value))
                continue;

            var drain = cog.DrainWattsPerSecond * frameTime;
            if (TryComp(cog.HostApc.Value, out BatteryComponent? battery) &&
                _battery.TryUseCharge((cog.HostApc.Value, battery), drain))
            {
                _power.TryAdjustPower(drain, quiet: true);
            }
        }
    }

    private void OnAfterInteract(Entity<FishIntegrationCogComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!_servant.IsServant(args.User))
            return;

        TryInstall(ent, args.User, args.Target.Value);
        args.Handled = true;
    }

    public bool TryInstall(Entity<FishIntegrationCogComponent> cog, EntityUid user, EntityUid target)
    {
        if (!CanInstall(cog, user, target))
            return false;

        DoInstall(cog, user, target);
        return true;
    }

    public bool CanInstall(Entity<FishIntegrationCogComponent> cog, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (cog.Comp.Installed)
            return false;

        if (!HasComp<ApcComponent>(target))
        {
            if (!quiet)
                _popup.PopupEntity(Loc.GetString("fish-clockwork-cog-need-apc"), user, user);
            return false;
        }

        return true;
    }

    private void DoInstall(Entity<FishIntegrationCogComponent> cog, EntityUid user, EntityUid apc)
    {
        cog.Comp.Installed = true;
        cog.Comp.HostApc = apc;
        Dirty(cog);

        // В«РџСЂСЏС‡РµРјВ» С€РµСЃС‚РµСЂС‘РЅРєСѓ РІ APC вЂ” РїСЂРёРєСЂРµРїР»СЏРµРј РєРѕРѕСЂРґРёРЅР°С‚С‹ Рє APC.
        _transform.SetCoordinates(cog.Owner, Transform(apc).Coordinates);

        _audio.PlayPvs(
            new SoundPathSpecifier("/Audio/_Fish/ClockworkCult/machines/clockcult/integration_cog_install.ogg"),
            apc);
        _popup.PopupEntity(Loc.GetString("fish-clockwork-cog-installed"), user, user);
    }
}

