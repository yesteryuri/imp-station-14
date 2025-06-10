using Content.Server._Goobstation.Heretic.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Heretic.Components;
using Content.Server.Heretic.EntitySystems;
using Content.Server.Humanoid;
using Content.Server.Objectives.Components;
using Content.Server.Revolutionary.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Forensics.Components;
using Content.Shared.Heretic;
using Content.Shared.Heretic.Prototypes;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;


namespace Content.Server.Heretic.Ritual;

/// <summary>
///     Checks for a nearest dead body,
///     gibs it and gives the heretic knowledge points.
/// </summary>
// these classes should be lead out and shot
[Virtual]
public partial class RitualSacrificeBehavior : RitualCustomBehavior
{
    /// <summary>
    ///     Minimal amount of corpses.
    /// </summary>
    [DataField] public float Min = 1;

    /// <summary>
    ///     Maximum amount of corpses.
    /// </summary>
    [DataField] public float Max = 1;

    /// <summary>
    ///     Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float SacrificePoints = 2f;

    /// <summary>
    ///     Points gained on sacrificing a normal crewmember.
    /// </summary>
    [DataField] public float CommandSacrificePoints = 3f;

    // this is awful but it works so i'm not complaining
    // i'm complaining -kandiyaki
    // IM ALSO COMPLAINING -mq
    private BloodstreamSystem _bloodstream = default!;
    private DamageableSystem _damage = default!;
    private EntityLookupSystem _lookup = default!;
    private HellWorldSystem _hellworld = default!;
    private HereticSystem _heretic = default!;
    private HumanoidAppearanceSystem _humanoid = default!;
    private IEntityManager _entmanager = default!;
    private IPrototypeManager _proto = default!;
    private SharedMindSystem _mind = default!;
    private SharedSolutionContainerSystem _solutionContainerSystem = default!;
    private SharedTransformSystem _xform;
    private TransformSystem _transformSystem = default!;
    protected List<EntityUid> Uids = new();

    public override bool Execute(RitualData args, out string? outstr)
    {
        // this fucking sucks -mq
        _bloodstream = args.EntityManager.System<BloodstreamSystem>();
        _damage = args.EntityManager.System<DamageableSystem>();
        _entmanager = IoCManager.Resolve<IEntityManager>();
        _hellworld = args.EntityManager.System<HellWorldSystem>();
        _heretic = args.EntityManager.System<HereticSystem>();
        _humanoid = args.EntityManager.System<HumanoidAppearanceSystem>();
        _lookup = args.EntityManager.System<EntityLookupSystem>();
        _mind = args.EntityManager.System<SharedMindSystem>();
        _proto = IoCManager.Resolve<IPrototypeManager>();
        _solutionContainerSystem = args.EntityManager.System<SharedSolutionContainerSystem>();
        _transformSystem = args.EntityManager.System<TransformSystem>();
        _xform = args.EntityManager.System<SharedTransformSystem>();
        _xform = args.EntityManager.System<SharedTransformSystem>();

        //if the performer isn't a heretic, stop
        if (!args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
        {
            outstr = string.Empty;
            return false;
        }

        //get all entities in range of the circle
        var lookup = _lookup.GetEntitiesInRange(args.Platform, .75f);
        if (lookup.Count == 0 || lookup == null)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice");
            return false;
        }

        // get all the dead ones
        foreach (var look in lookup)
        {
            if (!args.EntityManager.TryGetComponent<MobStateComponent>(look, out var mobstate) // only mobs
            || !args.EntityManager.HasComponent<HumanoidAppearanceComponent>(look) //player races only
            || args.EntityManager.HasComponent<HellVictimComponent>(look) //no reusing corpses
            || args.EntityManager.HasComponent<GhoulComponent>(look)) //shouldn't happen because they gib on death but. sanity check
                continue;

            if (mobstate.CurrentState == Shared.Mobs.MobState.Dead)
                Uids.Add(look);
        }

        //if none are dead, say so
        if (Uids.Count < Min)
        {
            outstr = Loc.GetString("heretic-ritual-fail-sacrifice-ineligible");
            return false;
        }

        outstr = null;
        return true;
    }

    //this does way too much
    public override void Finalize(RitualData args)
    {

        for (int i = 0; i < Max; i++)
        {
            var isCommand = args.EntityManager.HasComponent<CommandStaffComponent>(Uids[i]);
            var knowledgeGain = isCommand ? CommandSacrificePoints : SacrificePoints;

            //get the humanoid appearance component
            if (!args.EntityManager.TryGetComponent<HumanoidAppearanceComponent>(Uids[i], out var humanoid))
                return;

            //get the species prototype from that
            if (!_proto.TryIndex(humanoid.Species, out var speciesPrototype))
                return;

            //spawn a clone of the victim
            //this should really use the cloningsystem but i coded this before that existed
            //and it works so i'm not changing it unless it causes issues
            var sacrificialWhiteBoy = args.EntityManager.Spawn(speciesPrototype.Prototype, _transformSystem.GetMapCoordinates(Uids[i]));
            _humanoid.CloneAppearance(Uids[i], sacrificialWhiteBoy);
            //make sure it has the right DNA
            if (args.EntityManager.TryGetComponent<DnaComponent>(Uids[i], out var victimDna))
            {
                if (args.EntityManager.TryGetComponent<BloodstreamComponent>(sacrificialWhiteBoy, out var dummyBlood))
                {
                    //this is copied from BloodstreamSystem's OnDnaGenerated
                    //i hate it
                    if (_solutionContainerSystem.ResolveSolution(sacrificialWhiteBoy, dummyBlood.BloodSolutionName, ref dummyBlood.BloodSolution, out var bloodSolution))
                    {
                        foreach (var reagent in bloodSolution.Contents)
                        {
                            List<ReagentData> reagentData = reagent.Reagent.EnsureReagentData();
                            reagentData.RemoveAll(x => x is DnaData);
                            reagentData.AddRange(_bloodstream.GetEntityBloodData(Uids[i]));
                        }
                    }
                }
            }
            //beat the clone to death. this is just to get matching organs
            if (args.EntityManager.TryGetComponent<DamageableComponent>(Uids[i], out var dmg))
            {
                var prot = (ProtoId<DamageGroupPrototype>)"Brute";
                var dmgtype = _proto.Index(prot);
                _damage.TryChangeDamage(sacrificialWhiteBoy, new DamageSpecifier(dmgtype, 1984f), true);
            }

            //send the target to hell world
            _hellworld.AddVictimComponent(Uids[i]);

            //teleport the body to a midround antag spawn spot so it's not just tossed into space
            _hellworld.TeleportRandomly(args, Uids[i]);

            //make sure that my shitty AddVictimComponent thing actually worked before trying to use a mind that isn't there
            if (args.EntityManager.TryGetComponent<HellVictimComponent>(Uids[i], out var hellVictim))
            {
                //i'm so sorry to all of my computer science professors. i've failed you
                if (hellVictim.HasMind)
                {
                    _hellworld.SendToHell(Uids[i], args, speciesPrototype);
                }

            }

            //update the heretic's knowledge
            if (args.EntityManager.TryGetComponent<HereticComponent>(args.Performer, out var hereticComp))
                _heretic.UpdateKnowledge(args.Performer, hereticComp, knowledgeGain);

            // update objectives
            if (_mind.TryGetMind(args.Performer, out var mindId, out var mind))
            {
                // this is godawful dogshit. but it works :)
                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeObjective", out var crewObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewObj, out var crewObjComp))
                    crewObjComp.Sacrificed += 1;

                if (_mind.TryFindObjective((mindId, mind), "HereticSacrificeHeadObjective", out var crewHeadObj)
                && args.EntityManager.TryGetComponent<HereticSacrificeConditionComponent>(crewHeadObj, out var crewHeadObjComp)
                && isCommand)
                    crewHeadObjComp.Sacrificed += 1;
            }
        }

        // reset it because it refuses to work otherwise.
        Uids = new();
        args.EntityManager.EventBus.RaiseLocalEvent(args.Performer, new EventHereticUpdateTargets());
    }
}
