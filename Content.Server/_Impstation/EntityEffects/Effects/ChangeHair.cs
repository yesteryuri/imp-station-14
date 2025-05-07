using Content.Server.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Server.EntityEffects.Effects;
public sealed partial class ChangeHair : EntityEffect
{
    [Dependency] private readonly MarkingSet _markingset = default!;
    [Dependency] private readonly Marking _marking = default!;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-hair", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        _markingset.Remove(MarkingCategories.Hair, _marking.MarkingId); // ok so rn nullreferenceexception on organ via entityeffect
    } //EntityEffectReagentArgs is the problem child, but i have no idea what in this ^ line specifically is chasing her to that
} //i tried changing it to a contact chem to avoid this problem but it swapped from metabolizer null to organ null. 
