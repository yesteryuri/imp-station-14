using Content.Server.EntityEffects.Effects;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Drugs;
using Content.Shared.Drunk;
using Content.Shared.Heretic;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Random;

namespace Content.Server._Goobstation.Clothing;

public sealed partial class MadnessMaskSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly StaminaSystem _stamina = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffect = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MadnessMaskComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<MadnessMaskComponent, ClothingGotUnequippedEvent>(OnUnequip);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MadnessMaskComponent>();
        while (query.MoveNext(out var uid, out var mask))
        {
            if (!mask.Equipped)
                continue;
            mask.UpdateAccumulator += frameTime;
            if (mask.UpdateAccumulator < mask.UpdateTimer)
                continue;

            mask.UpdateAccumulator = 0;

            var lookup = _lookup.GetEntitiesInRange(uid, 5f);
            foreach (var look in lookup)
            {
                // heathens exclusive
                if (HasComp<HereticComponent>(look)
                || HasComp<GhoulComponent>(look))
                    continue;

                if (HasComp<StaminaComponent>(look) && _random.Prob(.4f))
                    _stamina.TakeStaminaDamage(look, 10f, visual: false);

                if (_random.Prob(.4f))
                    _jitter.DoJitter(look, TimeSpan.FromSeconds(.5f), true, amplitude: 5, frequency: 10);

                if (_random.Prob(.25f))
                    _statusEffect.TryAddStatusEffect<SeeingRainbowsComponent>(look, "SeeingRainbows", TimeSpan.FromSeconds(10f), false);
            }
        }
    }

    private void OnEquip(Entity<MadnessMaskComponent> mask, ref ClothingGotEquippedEvent args)
    {
        mask.Comp.Equipped = true;
    }

    private void OnUnequip(Entity<MadnessMaskComponent> mask, ref ClothingGotUnequippedEvent args)
    {
        mask.Comp.Equipped = false;
    }
}
