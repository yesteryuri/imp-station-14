using Content.Shared.Movement.Systems;

namespace Content.Shared.SnailSpeed;

/// <summary>
/// Allows mobs to produce materials using Thirst with <see cref="ExcretionComponent"/>.
/// </summary>
public abstract partial class SharedSnailSpeedSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SnailSpeedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SnailSpeedComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
    }

    private void OnMapInit(EntityUid uid, SnailSpeedComponent comp, MapInitEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(uid);
    }

    /// apply constant movespeed modifier as long as entity is not flying
	private void OnRefreshMovespeed(EntityUid uid, SnailSpeedComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_jetpack.IsUserFlying(uid))
            return;

        args.ModifySpeed(component.SnailSlowdownModifier, component.SnailSlowdownModifier);
    }

}
