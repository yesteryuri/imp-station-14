using Content.Server.Traits.Assorted;
using Robust.Shared.Random;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Timing;

namespace Content.Server.EntityEffects.Effects;
/// <summary>
/// adds time to the minimum and maximum duration between hallucinations from paracusia
/// </summary>
[UsedImplicitly]
public sealed partial class ResetParacusia : EntityEffect
{
    /// <summary>
    /// The # of seconds that gets added to min and max time between hallucinations.
    /// </summary>
    [DataField("TimerReset")]
    public float TimerReset = Random.Shared.NextFloat(200, 700);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-paracusia", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var target = args.TargetEntity;
        var entSys = entMan.EntitySysManager;
        if (!entMan.TryGetComponent(target, out ParacusiaComponent? paracusia)) //resetnarcolepsy doesnt do this so it's not required i think???
            return;
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        entSys.GetEntitySystem<ParacusiaSystem>().SetTime(target, 0.1f + TimerReset, 300f + TimerReset); //player paracusia min is 0.1 and max 300
    }
}
