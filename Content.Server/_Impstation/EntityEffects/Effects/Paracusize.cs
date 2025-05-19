using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Content.Shared.Traits.Assorted;
using Content.Server.Traits.Assorted;
using Robust.Shared.Audio;

namespace Content.Server._Impstation.EntityEffects.Effects;

public sealed partial class Paracusize : EntityEffect
{
    /// <summary>
    ///     Gives the entity paracusia
    /// </summary>
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes long-term paracusia";
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;
        var paraSys = entityManager.System<ParacusiaSystem>();
        var paracusiaSounds = new SoundCollectionSpecifier("Paracusia");
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        if (!entityManager.EnsureComponent<ParacusiaComponent>(uid, out var paracusia)) //make sure they have paracusia, populate it with sounds and a timer if they didnt have it before
        {
            paraSys.SetSounds(uid, paracusiaSounds, paracusia);
            paraSys.SetTime(uid, 0.1f, 300f, paracusia);
            paraSys.SetDistance(uid, 7f, paracusia);
        }
    }
}