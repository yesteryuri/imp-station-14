using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Content.Server.EntityEffects.Effects;
public sealed partial class ChangeHair : EntityEffect
{
//    [DataField(required: true)]
//    public string Key = default!;
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-hair", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityMa = args.EntityManager;
        var humanoidAp = entityMa.System<HumanoidAppearanceSystem>();
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        if (!entityMa.TryGetComponent<HumanoidAppearanceComponent>(args.TargetEntity, out var appearance))
            return;
        if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var category))
            return;
        for (var i = 0; i < category.Count; i++)
        {
            humanoidAp.RemoveMarking(args.TargetEntity, MarkingCategories.Hair, i);
        }
    }
}
