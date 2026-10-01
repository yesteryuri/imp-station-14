using Content.Server._Impstation.Homunculi.Incubator;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared._Impstation.Homunculi.Components;
using Content.Shared._Impstation.Homunculi.Incubator.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Body;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.Homunculi;

public sealed class HomunculusSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IncubatorSystem _incubator = default!;
    [Dependency] private VisualBodySystem _visualBody = default!;

    private static readonly HashSet<HumanoidVisualLayers> HomunculiLayers = new ()
    {
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
    };

    public bool CreateHomunculiWithDna(Entity<IncubatorComponent?> ent, Entity<SolutionComponent> solution, MapCoordinates mapCoordinates, [NotNullWhen(true)] out EntityUid? homunculus)
    {
        // If there's no DNA data in the solution, return
        if (!_incubator.HasDnaData(solution))
        {
            homunculus = null;
            return false;
        }

        // Save a copy of the solutions reagents, can't just use it straight up
        var reagentList = solution.Comp.Solution.Contents.ToList();
        List<string?> storedDna = [];
        List<Entity<HomunculusTypeComponent>> entities = [];

        foreach (var dnaList in reagentList.Select(reagent => reagent.Reagent.EnsureReagentData().OfType<DnaData>()))
        {
            storedDna.AddRange(dnaList.Select(dna => dna.DNA));
        }

        var query = EntityQueryEnumerator<HomunculusTypeComponent, DnaComponent>();
        while (query.MoveNext(out var entityUid,out var homunculusType, out var dna))
        {
            if (!VerifyAndUseRecipe(homunculusType, solution, reagentList))
                continue;

            if (storedDna.Contains(dna.DNA))
                entities.Add((entityUid, homunculusType));
        }
        if (entities.Count > 0)
        {
            CreateHomunculiFromEntities(entities, storedDna, mapCoordinates, out var realHomunculi);
            homunculus = realHomunculi;
            return true;
        }

        homunculus = null;
        return false;
    }

    public void CreateHomunculiFromEntities(List<Entity<HomunculusTypeComponent>> entities,List<string?> dnaData, MapCoordinates mapCoordinates, out EntityUid homunculus)
    {
        homunculus = EntityManager.Spawn(entities[0].Comp.HomunculusType, mapCoordinates);
        _transform.AttachToGridOrMap(homunculus);

        EnsureComp<DnaComponent>(homunculus, out var homunculiDnaComponent);

        homunculiDnaComponent.DNA = string.Join("", dnaData);

        SetHomunculusAppearance(entities,homunculus);
    }

    public bool VerifyAndUseRecipe(HomunculusTypeComponent homunculusComp, Entity<SolutionComponent> solution, List<ReagentQuantity> reagents)
    {
        if (!SatisfiesRecipe(homunculusComp, reagents))
            return false;

        var savedSolutions = solution.Comp.Solution.Contents.ToList();
        // Go through all the reagents in the saved solution, if the reagent matches one in the recipe, remove it
        // I have to check for reagent data because it needs to be specific or else it won't drain
        foreach (var (reagent, amount) in homunculusComp.Recipe)
        {
            var match = savedSolutions.FirstOrDefault(rq => rq.Reagent.Prototype == reagent);

            if (match.Reagent.Data != null)
                _solution.RemoveReagent(solution, reagent, amount, match.Reagent.Data);
            else
                _solution.RemoveReagent(solution, reagent, amount);
        }
        return true;
    }

    private static bool SatisfiesRecipe(HomunculusTypeComponent component, List<ReagentQuantity> reagents)
    {
        foreach (var required in component.Recipe)
        {
            var available = reagents.FirstOrDefault(r => r.Reagent.Prototype == required.Key);

            if (available.Quantity < required.Value)
                return false;
        }
        return true;
    }

    private void SetHomunculusAppearance(List<Entity<HomunculusTypeComponent>> entities, EntityUid homunculi)
    {
        // TODO: Could probably just store a (Color,Color,int) instead?
        var organs = new Dictionary<ProtoId<OrganCategoryPrototype>, List<OrganProfileData>>(HomunculiLayers.Count);

        foreach (var urist in entities)
        {
            if (!_visualBody.TryGatherMarkingsData(urist.Owner, HomunculiLayers, out var datas, out _, out var markings))
                continue;

            foreach (var (key, value) in datas)
            {
                ref var list = ref CollectionsMarshal.GetValueRefOrAddDefault(organs, key, out var exists);
                if (!exists)
                    list = new ();

                list?.Add(value);
            }

            if (urist == entities.First())
                _visualBody.ApplyMarkings(homunculi, markings);
        }

        // Need to iterate twice so we can blend properly. TODO: Optimize to blend while iterating the first time. Less alloc, less operations!
        var newData = new Dictionary<ProtoId<OrganCategoryPrototype>, OrganProfileData>(HomunculiLayers.Count);
        foreach (var (key, value) in organs)
        {
            newData[key] = BlendOrgans(value);
        }

        _visualBody.ApplyProfiles(homunculi, newData);
    }

    private static OrganProfileData BlendOrgans(List<OrganProfileData> organs)
    {
        if (organs.Count == 0)
            return new OrganProfileData();

        var baseSkinColor = Color.Black;
        var baseEyeColor = Color.Black;

        foreach (var organ in organs)
        {
            baseSkinColor.R =+ organ.SkinColor.R;
            baseSkinColor.G =+ organ.SkinColor.G;
            baseSkinColor.B =+ organ.SkinColor.B;
            baseEyeColor.R =+ organ.EyeColor.R;
            baseEyeColor.G =+ organ.EyeColor.G;
            baseEyeColor.B =+ organ.EyeColor.B;
        }

        baseSkinColor.R /= organs.Count;
        baseSkinColor.G /= organs.Count;
        baseSkinColor.B /= organs.Count;
        baseEyeColor.R /= organs.Count;
        baseEyeColor.G /= organs.Count;
        baseEyeColor.B /= organs.Count;

        return new OrganProfileData
        {
            EyeColor = baseEyeColor,
            SkinColor = baseSkinColor
        };
    }
}
