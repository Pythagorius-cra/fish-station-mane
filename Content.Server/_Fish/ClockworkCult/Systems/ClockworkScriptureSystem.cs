using System.Linq;
using Content.Server._Fish.ClockworkCult.GameRule;
using Content.Server.DoAfter;
using Content.Server.Popups;
using Content.Shared._Fish.ClockworkCult;
using Content.Shared._Fish.ClockworkCult.Components;
using Content.Shared._Fish.ClockworkCult.Prototypes;
using Content.Shared._Fish.ClockworkCult.UI;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Fish.ClockworkCult.Systems;

/// <summary>
/// Slab BUI + scripture unlock/invoke pipeline (data-driven handlers).
/// </summary>
public sealed class ClockworkScriptureSystem : EntitySystem
{
    [Dependency] private readonly ClockworkCultRuleSystem _cult = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly Dictionary<string, Action<EntityUid, EntityUid, ClockworkCultRuleComponent, ClockworkScripturePrototype>> _handlers = new();

    public override void Initialize()
    {
        base.Initialize();
        RegisterHandlers();

        Subs.BuiEvents<ClockworkSlabComponent>(ClockworkSlabUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnUiOpened);
            subs.Event<ClockworkSlabReciteMessage>(OnRecite);
            subs.Event<ClockworkSlabUnlockMessage>(OnUnlock);
            subs.Event<ClockworkSlabQuickbindMessage>(OnQuickbind);
        });

        SubscribeLocalEvent<ClockworkSlabComponent, ClockworkScriptureInvokeDoAfterEvent>(OnInvokeFinished);
        SubscribeLocalEvent<ClockworkSlabComponent, AfterInteractEvent>(OnSlabAfterInteract);
    }

    private void RegisterHandlers()
    {
        _handlers["spawn"] = HandleSpawn;
        _handlers["empower_kindle"] = (_, slab, _, _) => SetEmpowerment(slab, ClockworkSlabEmpowerment.Kindle);
        _handlers["empower_manacles"] = (_, slab, _, _) => SetEmpowerment(slab, ClockworkSlabEmpowerment.Manacles);
        _handlers["empower_compromise"] = (_, slab, _, _) => SetEmpowerment(slab, ClockworkSlabEmpowerment.Compromise);
        _handlers["replicant"] = HandleReplicant;
        _handlers["integration_cog"] = HandleIntegrationCog;
        _handlers["fabricator"] = HandleFabricator;
        _handlers["abscond"] = HandleAbscond;
        _handlers["armaments"] = HandleArmaments;
        _handlers["vanguard"] = HandleVanguard;
        _handlers["compromise"] = HandleCompromiseSelf;
        _handlers["summon_cogscarab"] = HandleSummonCogscarab;
        _handlers["summon_marauder"] = HandleSummonMarauder;
        _handlers["dimensional_breach"] = HandleDimensionalBreach;
        _handlers["spawn_structure"] = HandleSpawn;
    }

    private void OnUiOpened(Entity<ClockworkSlabComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnRecite(Entity<ClockworkSlabComponent> ent, ref ClockworkSlabReciteMessage args)
    {
        var user = args.Actor;
        if (!HasComp<ClockworkServantComponent>(user))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        if (!_prototypes.TryIndex<ClockworkScripturePrototype>(args.ScriptureId, out var scripture))
            return;

        if (!_cult.IsUnlocked(rule, scripture.ID) && !scripture.StartsUnlocked)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-locked"), user, user, PopupType.MediumCaution);
            return;
        }

        if (rule.Power < scripture.PowerCost || rule.Vitality < scripture.VitalityCost)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-resources"), user, user, PopupType.MediumCaution);
            return;
        }

        ent.Comp.PendingScripture = scripture.ID;
        Dirty(ent);

        var doAfter = new DoAfterArgs(EntityManager, user, scripture.InvokeTime,
            new ClockworkScriptureInvokeDoAfterEvent(), ent, used: ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-invoking", ("name", Loc.GetString(scripture.Name))), user, user);
    }

    private void OnUnlock(Entity<ClockworkSlabComponent> ent, ref ClockworkSlabUnlockMessage args)
    {
        var user = args.Actor;
        if (!HasComp<ClockworkServantComponent>(user))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        if (!_prototypes.TryIndex<ClockworkScripturePrototype>(args.ScriptureId, out var scripture))
            return;

        if (_cult.IsUnlocked(rule, scripture.ID))
            return;

        if (!_cult.TryUnlockScripture(rule, scripture.ID, scripture.CogUnlockCost))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-cogs"), user, user, PopupType.MediumCaution);
            return;
        }

        _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-unlocked", ("name", Loc.GetString(scripture.Name))), user, user);
        UpdateUi(ent);
    }

    private void OnQuickbind(Entity<ClockworkSlabComponent> ent, ref ClockworkSlabQuickbindMessage args)
    {
        if (!HasComp<ClockworkServantComponent>(args.Actor))
            return;

        if (args.Slot < 0 || args.Slot >= ent.Comp.MaxQuickbinds)
            return;

        while (ent.Comp.Quickbinds.Count <= args.Slot)
            ent.Comp.Quickbinds.Add(string.Empty);

        ent.Comp.Quickbinds[args.Slot] = args.ScriptureId;
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnInvokeFinished(EntityUid uid, ClockworkSlabComponent component, ClockworkScriptureInvokeDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var scriptureId = component.PendingScripture;
        component.PendingScripture = null;
        Dirty(uid, component);

        if (scriptureId == null)
            return;

        if (!HasComp<ClockworkServantComponent>(user))
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        if (!_prototypes.TryIndex<ClockworkScripturePrototype>(scriptureId, out var scripture))
            return;

        if (!_cult.IsUnlocked(rule, scripture.ID) && !scripture.StartsUnlocked)
            return;

        if (!_cult.TrySpendPower(rule, scripture.PowerCost))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-resources"), user, user, PopupType.MediumCaution);
            return;
        }

        if (!_cult.TrySpendVitality(rule, scripture.VitalityCost))
        {
            // Возвращаем power, если vitality не хватило.
            _cult.AddPower(rule, scripture.PowerCost);
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-resources"), user, user, PopupType.MediumCaution);
            return;
        }

        if (scripture.SetsEmpowerment != null)
            SetEmpowerment(uid, scripture.SetsEmpowerment.Value);

        if (_handlers.TryGetValue(scripture.Effect, out var handler))
            handler(user, uid, rule, scripture);
        else
            HandleSpawn(user, uid, rule, scripture);

        UpdateUi((uid, component));
    }

    private void OnSlabAfterInteract(EntityUid uid, ClockworkSlabComponent component, AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!HasComp<ClockworkServantComponent>(args.User))
            return;

        switch (component.Empowerment)
        {
            case ClockworkSlabEmpowerment.Kindle:
                ApplyKindle(args.User, args.Target.Value);
                component.Empowerment = ClockworkSlabEmpowerment.None;
                Dirty(uid, component);
                args.Handled = true;
                break;
            case ClockworkSlabEmpowerment.Manacles:
                ApplyManacles(args.User, args.Target.Value);
                component.Empowerment = ClockworkSlabEmpowerment.None;
                Dirty(uid, component);
                args.Handled = true;
                break;
            case ClockworkSlabEmpowerment.Compromise:
                ApplyCompromise(args.User, args.Target.Value);
                component.Empowerment = ClockworkSlabEmpowerment.None;
                Dirty(uid, component);
                args.Handled = true;
                break;
        }
    }

    public void UpdateUi(Entity<ClockworkSlabComponent> ent)
    {
        if (!_cult.TryGetRule(out _, out var rule))
            return;

        var entries = new List<ClockworkScriptureUiEntry>();
        foreach (var scripture in _prototypes.EnumeratePrototypes<ClockworkScripturePrototype>())
        {
            var unlocked = _cult.IsUnlocked(rule, scripture.ID) || scripture.StartsUnlocked;
            entries.Add(new ClockworkScriptureUiEntry
            {
                Id = scripture.ID,
                Name = Loc.GetString(scripture.Name),
                Description = Loc.GetString(scripture.Description),
                Category = scripture.Category,
                PowerCost = scripture.PowerCost,
                VitalityCost = scripture.VitalityCost,
                CogUnlockCost = scripture.CogUnlockCost,
                Unlocked = unlocked,
                CanAfford = unlocked && rule.Power >= scripture.PowerCost && rule.Vitality >= scripture.VitalityCost,
                CanUnlock = !unlocked && rule.Cogs >= scripture.CogUnlockCost,
                InvokeTime = scripture.InvokeTime,
            });
        }

        var state = new ClockworkSlabBoundUserInterfaceState
        {
            Power = rule.Power,
            Vitality = rule.Vitality,
            Cogs = rule.Cogs,
            ServantCount = rule.ServantCount,
            ArkPhase = rule.ArkPhase,
            ArkPhaseEndsAt = rule.ArkPhaseEndsAt,
            Scriptures = entries.OrderBy(e => e.Category).ThenBy(e => e.Name).ToList(),
            Quickbinds = ent.Comp.Quickbinds.ToList(),
            Empowerment = ent.Comp.Empowerment,
        };

        _ui.SetUiState(ent.Owner, ClockworkSlabUiKey.Key, state);
    }

    private void SetEmpowerment(EntityUid slab, ClockworkSlabEmpowerment empowerment)
    {
        if (!TryComp<ClockworkSlabComponent>(slab, out var comp))
            return;

        comp.Empowerment = empowerment;
        Dirty(slab, comp);
    }

    private void HandleSpawn(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        if (scripture.SpawnPrototype == null)
            return;

        var ent = Spawn(scripture.SpawnPrototype.Value, _transform.GetMapCoordinates(user));
        if (HasComp<ClockworkArkComponent>(ent))
            _cult.RegisterArk(ent, rule);

        if (HasComp<ClockworkCacheComponent>(ent))
            EnsureComp<ClockworkAbscondTargetComponent>(ent);
    }

    private void HandleReplicant(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn(rule.SlabPrototype, _transform.GetMapCoordinates(user));
    }

    private void HandleIntegrationCog(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn(scripture.SpawnPrototype ?? "ClockworkIntegrationCog", _transform.GetMapCoordinates(user));
    }

    private void HandleFabricator(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn(scripture.SpawnPrototype ?? "ClockworkReplicaFabricator", _transform.GetMapCoordinates(user));
    }

    private void HandleAbscond(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        EntityUid? target = null;

        if (rule.ArkEntity != null && Exists(rule.ArkEntity.Value))
            target = rule.ArkEntity;

        if (target == null)
        {
            var query = EntityQueryEnumerator<ClockworkAbscondTargetComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                target = uid;
                break;
            }
        }

        if (target == null)
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-abscond-no-target"), user, user, PopupType.MediumCaution);
            // Возврат стоимости
            _cult.AddPower(rule, scripture.PowerCost);
            return;
        }

        _transform.SetCoordinates(user, Transform(target.Value).Coordinates);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-abscond-success"), user, user);
    }

    private void HandleArmaments(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn("ClothingOuterClockworkArmor", _transform.GetMapCoordinates(user));
        Spawn("ClockworkSpear", _transform.GetMapCoordinates(user));
    }

    private void HandleVanguard(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        _stun.TryUpdateParalyzeDuration(user, TimeSpan.Zero);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-vanguard"), user, user);
    }

    private void HandleCompromiseSelf(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        ApplyCompromise(user, user);
    }

    private void HandleSummonCogscarab(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn(scripture.SpawnPrototype ?? "MobClockworkCogscarab", _transform.GetMapCoordinates(user));
    }

    private void HandleSummonMarauder(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        Spawn(scripture.SpawnPrototype ?? "MobClockworkMarauder", _transform.GetMapCoordinates(user));
    }

    private void HandleDimensionalBreach(EntityUid user, EntityUid slab, ClockworkCultRuleComponent rule, ClockworkScripturePrototype scripture)
    {
        if (!_cult.TryActivateArk(rule, force: true))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-ark-activate-failed"), user, user, PopupType.MediumCaution);
            _cult.AddPower(rule, scripture.PowerCost);
            return;
        }

        _popup.PopupEntity(Loc.GetString("clockwork-cult-ark-activated"), user, user);
    }

    private void ApplyKindle(EntityUid user, EntityUid target)
    {
        if (HasComp<ClockworkServantComponent>(target))
            return;

        _stun.TryUpdateParalyzeDuration(target, TimeSpan.FromSeconds(6));
        _popup.PopupEntity(Loc.GetString("clockwork-cult-kindle"), target, user);
    }

    private void ApplyManacles(EntityUid user, EntityUid target)
    {
        if (HasComp<ClockworkServantComponent>(target) || HasComp<MindShieldComponent>(target))
            return;

        Spawn("ClockworkManacles", _transform.GetMapCoordinates(target));
        _popup.PopupEntity(Loc.GetString("clockwork-cult-manacles"), target, user);
    }

    private void ApplyCompromise(EntityUid user, EntityUid target)
    {
        if (!HasComp<ClockworkServantComponent>(target) && user != target)
            return;

        if (!_cult.TryGetRule(out _, out var rule))
            return;

        if (!_cult.TrySpendVitality(rule, 10))
        {
            _popup.PopupEntity(Loc.GetString("clockwork-cult-scripture-no-resources"), user, user, PopupType.MediumCaution);
            return;
        }

        // Простой sustain: снимаем стан и даём feedback. Полный heal damage — через damageable API при необходимости.
        _stun.TryUpdateParalyzeDuration(target, TimeSpan.Zero);
        _popup.PopupEntity(Loc.GetString("clockwork-cult-compromise"), target, user);
    }
}
