using Content.Goobstation.Common.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Goobstation.Shared.MartialArts.Events;
using Content.Shared.Gibbing.Components;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Atmos.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Goobstation.Shared.MartialArts;

public partial class SharedMartialArtsSystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private void InitializePlasmaFist()
    {
        SubscribeLocalEvent<CanPerformComboComponent, TornadoSweepPerformedEvent>(OnTornadoSweep);
        SubscribeLocalEvent<CanPerformComboComponent, ThrowbackPerformedEvent>(OnThrowback);
        SubscribeLocalEvent<CanPerformComboComponent, ThePlasmaFistPerformedEvent>(OnThePlasmaFist);

        SubscribeLocalEvent<GrantPlasmaFistComponent, UseInHandEvent>(OnGrantCQCUse);
        SubscribeLocalEvent<GrantPlasmaFistComponent, ExaminedEvent>(OnGrantCQCExamine);

    }

    #region Generic Methods

    private void OnGrantCQCUse(EntityUid ent, GrantPlasmaFistComponent comp, UseInHandEvent args)
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
    }
    #endregion

    #region Combo Methods

    private void OnTornadoSweep(Entity<CanPerformComboComponent> ent, ref TornadoSweepPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        dir *= 1f / dir.Length();

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);

        var xform = _entityManager.GetComponent<TransformComponent>(ent);
        var mapId = xform.MapID;
        var position = _transform.GetMapCoordinates(ent).Position;

        var physicsQuery = _entityManager.GetEntityQuery<PhysicsComponent>();
        var movedByPressureQuery = _entityManager.GetEntityQuery<MovedByPressureComponent>();

        // Use the entity lookup system to find entities within the radius.
        // Iterate over all entities with PhysicsComponent and TransformComponent
        var entities = new List<EntityUid>();
        var query = _entityManager.AllEntityQueryEnumerator<PhysicsComponent, TransformComponent>();
        float repulseRadius = 10f;
        while (query.MoveNext(out var uid, out var phys, out var trans))
        {
            //if (trans.MapID != mapId)
            //    continue;

            if (uid == ent.Owner)
                break;

            if ((position - _transform.GetMapCoordinates(uid).Position).LengthSquared() <= repulseRadius * repulseRadius)
            {
                entities.Add(uid);
            }
        }

        foreach (var entity in entities)
        {
            if (!physicsQuery.TryGetComponent(entity, out var physics))
                continue;

            if (movedByPressureQuery.TryGetComponent(entity, out var movedPressure) && !movedPressure.Enabled) //Ignore magboots users
                continue;

            // Calculate the direction from the source to the entity.
            var entityXform = _entityManager.GetComponent<TransformComponent>(entity);
            var direction = _transform.GetMapCoordinates(entity).Position - position;
            var distance = direction.Length();

            // Normalize the direction.
            if (distance > 0)
            {
                direction /= distance;
            }
            else
            {
                // If the entity is at the exact same position, apply a random direction.
                direction = _random.NextVector2().Normalized();
            }
            // Apply the impulse.
            _entityManager.System<SharedPhysicsSystem>().ApplyLinearImpulse(entity, direction * 1000, body: physics);
        }
        var ev = new MartialArtSaying("plasmafist-saying-tornado");
        RaiseLocalEvent(ent, ev);
        ComboPopup(ent, target, proto.Name);
    }

    private void OnThrowback(Entity<CanPerformComboComponent> ent, ref ThrowbackPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;

        var mapPos = _transform.GetMapCoordinates(ent).Position;
        var hitPos = _transform.GetMapCoordinates(target).Position;
        var dir = hitPos - mapPos;
        dir *= 1f / dir.Length();

        if (TryComp<PullableComponent>(target, out var pullable))
            _pulling.TryStopPull(target, pullable, ent, true);
        _grabThrowing.Throw(target, ent, dir, 50f);

        ComboPopup(ent, target, proto.Name);

        var items = _hands.EnumerateHeld(ent);
        foreach (var item in items)
        {
            if (!_hands.TryDrop(ent, item))
                break;
            _throwing.TryThrow(item, dir, 50f, ent, 0);
        }
        items = _hands.EnumerateHeld(target);
        foreach (var item in items)
        {
            if (!_hands.TryDrop(target, item))
                break;
            _throwing.TryThrow(item, dir, 50f, ent, 0);
        }
    }
    private void OnThePlasmaFist(Entity<CanPerformComboComponent> ent, ref ThePlasmaFistPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed))
            return;
        if (TryComp<BodyComponent>(ent, out BodyComponent? outBody) && outBody is not null)
        {
            var mapPos = _transform.GetMapCoordinates(ent).Position;
            var hitPos = _transform.GetMapCoordinates(target).Position;
            var dir = hitPos - mapPos;
            dir *= 1f / dir.Length();
            _bodySystem.GibBody(target, splatDirection: dir, splatModifier: 100);
        }
        var ev = new MartialArtSaying("plasmafist-saying-fist");
        RaiseLocalEvent(ent, ev);
        ComboPopup(ent, target, proto.Name);
    }
    #endregion
}
