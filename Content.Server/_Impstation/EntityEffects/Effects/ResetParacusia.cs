using Content.Server.Traits.Assorted;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Server.EntityEffects.Effects;

[UsedImplicitly]
public sealed partial class ResetParacusia : EntityEffect
{
    /// <summary>
    /// im kinda stupid so rn this sets paracusia min timer to 10 minutes and max to 20
    /// i copied resetnarcolepsy for this
    /// i wish that this would run the calculation for next hallucination time immediately with the new timers set but idk how to access client/para sys
    /// </summary>
    [DataField("TimerReset")]
    public int TimerMinReset = 600;
    public int TimerMaxReset = 1200;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-paracusia", ("chance", Probability)); 

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;

        args.EntityManager.EntitySysManager.GetEntitySystem<ParacusiaSystem>().SetTime(args.TargetEntity, TimerMinReset, TimerMaxReset);
    }
}
