using Content.Server.Traits.Assorted;
using Robust.Shared.Random;
using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio;
using Content.Server.Administration.Commands;

namespace Content.Server.EntityEffects.Effects;
/// <summary>
/// Adds time to the minimum and maximum duration between hallucinations from paracusia
/// </summary>
[UsedImplicitly]
public sealed partial class ResetParacusia : EntityEffect
{
    /// <summary>
    /// The # of seconds that gets added to min and max time between hallucinations.
    /// </summary>
    [DataField("TimerReset")]
    public float TimerReset = Random.Shared.NextFloat(200, 600);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-reset-paracusia", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entMan = args.EntityManager;
        var target = args.TargetEntity;
        var entSys = entMan.EntitySysManager;
        var paraSys = entSys.GetEntitySystem<ParacusiaSystem>();
        var paracusiaSounds = new SoundCollectionSpecifier("Paracusia");
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        if (!entMan.TryGetComponent(target, out ParacusiaComponent? paracusia)) //if dont have paracusia, return
            return;
        else if (paracusia.MinTimeBetweenIncidents > 0.1f) //if paracusia min time is already above default, return
            return;
        else
        {
            paraSys.SetTime(target, 0.1f + TimerReset, 300f + TimerReset); //default paracusia min is 0.1 and max 300
            paraSys.SetSounds(target, paracusiaSounds, paracusia);
            paraSys.SetDistance(target, 7f, paracusia);
        }
    }
}
