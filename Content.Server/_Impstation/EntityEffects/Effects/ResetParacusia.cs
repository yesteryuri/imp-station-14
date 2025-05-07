using Content.Server.Traits.Assorted;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Random;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;
/// <summary>
/// adds time to the minimum and maximum duration between hallucinations from paracusia
/// </summary>
[UsedImplicitly]
public sealed partial class ResetParacusia : EntityEffect
{
    /// <summary>
    /// # of seconds that gets added to min and max time between hallucination. default min is 30, reagent takes a random between 500-700
    /// </summary>
    [DataField("TimerReset")]
    public float TimerReset = Random.Shared.NextFloat(500, 700);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-paracusia", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        args.EntityManager.EntitySysManager.GetEntitySystem<ParacusiaSystem>().SetTime(args.TargetEntity, 30 + TimerReset, 60 + TimerReset);
    }
}
