using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;
using Content.Shared.EntityEffects;
using Content.Server.Humanoid;
using Content.Shared.Humanoid;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class ChangeHair : EntityEffect
{
    /// <summary>
    /// Checks humanoid appearance component and sets a random hairstyle and color if possible
    /// </summary>

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-change-hair", ("chance", Probability));
    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityMa = args.EntityManager;
        var humanoidAp = entityMa.System<HumanoidAppearanceSystem>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var markingManager = IoCManager.Resolve<MarkingManager>();
        var uid = args.TargetEntity;
        if (args is EntityEffectReagentArgs reagentArgs)
            if (reagentArgs.Scale != 1f)
                return;
        if (!entityMa.TryGetComponent<HumanoidAppearanceComponent>(uid, out var appearance))
            return;
        if (!appearance.MarkingSet.TryGetCategory(MarkingCategories.Hair, out var category))
            return;
        for (var i = 0; i < category.Count; i++)
        {
            humanoidAp.RemoveMarking(uid, MarkingCategories.Hair, i);
        }
        var hairColor = new Color(random.NextFloat(1), random.NextFloat(1), random.NextFloat(1), 1);
        var hairsPossible = markingManager.MarkingsByCategoryAndSpecies(MarkingCategories.Hair, appearance.Species).Keys.ToList();
        var newHairStyle = random.Pick(hairsPossible);
        humanoidAp.AddMarking(uid, newHairStyle, hairColor);
    }
}
