using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Goobstation.Shared.Stunnable;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;

namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{
    [Dependency] private readonly FireProtectionSystem _fireProtectionSystem = default!;
    private void InitializeSpaceBear()
    {
        SubscribeLocalEvent<CanPerformComboComponent, BearJawsPerformedEvent>(OnBearJaws);
        SubscribeLocalEvent<CanPerformComboComponent, PawSlamPerformedEvent>(OnPawSlam);
        SubscribeLocalEvent<CanPerformComboComponent, BearSmokeyPerformedEvent>(OnBearSmokey);

        SubscribeLocalEvent<GrantSpaceBearComponent, UseInHandEvent>(OnGrantCQCUse);
        SubscribeLocalEvent<GrantSpaceBearComponent, ExaminedEvent>(OnGrantCQCExamine);

    }

    #region Generic Methods

    private void OnGrantCQCUse(EntityUid ent, GrantSpaceBearComponent comp, UseInHandEvent args)
    {
        if (!_netManager.IsServer)
            return;

        if (comp.Used)
        {
            _popupSystem.PopupEntity(Loc.GetString(comp.LearnFailMessage, ("manual", Identity.Entity(ent, EntityManager))),
                args.User,
                args.User);
            return;
        }

        if (!TryGrantMartialArt(args.User, comp))
            return;
        _popupSystem.PopupEntity(Loc.GetString(comp.LearnMessage), args.User, args.User);
        comp.Used = true;
        //_entityManager.AddComponent<FireProtectionComponent>(ent, 0.0f, Loc.GetString("spacebear-description-fireimmunity"));
        _faction.AddFaction(args.User, "SimpleHostile");
    }

    private void OnSpaceBearAttackPerformed(Entity<MartialArtsKnowledgeComponent> ent, ref ComboAttackPerformedEvent args)
    {
        if (args.Weapon != args.Performer || args.Target == args.Performer)
            return;

        switch (args.Type)
        {
            case ComboAttackType.Harm:
                DoDamage(ent, args.Target, "Slash", 10, out _);
                _stamina.TakeStaminaDamage(args.Performer, -20f, applyResistances: false);
                break;
        }
    }
    #endregion

    #region Combo Methods

    private void OnBearJaws(Entity<CanPerformComboComponent> ent, ref BearJawsPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        if (downed)
        {
            DoDamage(ent, target, "Slash", 2*proto.ExtraDamage, out _);
            _stamina.TakeStaminaDamage(ent, -20f, applyResistances: false);
        }
        else
        {
            DoDamage(ent, target, "Slash", proto.ExtraDamage, out _);
        }
        ComboPopup(ent, target, proto.Name);
    }

    private void OnPawSlam(Entity<CanPerformComboComponent> ent, ref PawSlamPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;
        if (!downed)
        {
            DoDamage(ent, target, "Slash", proto.ExtraDamage, out _);
            _stun.TryKnockdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime), true);
            _stun.TrySlowdown(target, TimeSpan.FromSeconds(proto.ParalyzeTime + 2), true);
        }
        else
        {
            DoDamage(ent, target, "Slash", 2*proto.ExtraDamage, out _);
            _stun.TrySlowdown(target, TimeSpan.FromSeconds(2), true);
        }
        ComboPopup(ent, target, proto.Name);
    }
    private void OnBearSmokey(Entity<CanPerformComboComponent> ent, ref BearSmokeyPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;
       var ev = new BurnSomeMunta(target, proto.ParalyzeTime);
        RaiseLocalEvent(ent, ev);
        ComboPopup(ent, target, proto.Name);
    }
    #endregion
}
