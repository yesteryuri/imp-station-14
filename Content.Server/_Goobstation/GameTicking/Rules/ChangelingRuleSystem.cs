using Content.Server.Antag;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Shared.Changeling;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;
using System.Text;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ChangelingRuleSystem : GameRuleSystem<ChangelingRuleComponent>
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ObjectivesSystem _objective = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;

    public readonly SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Goobstation/Ambience/Antag/changeling_start.ogg");

    public readonly ProtoId<AntagPrototype> ChangelingPrototypeId = "Changeling";

    public readonly ProtoId<NpcFactionPrototype> ChangelingFactionId = "Changeling";

    public readonly ProtoId<NpcFactionPrototype> NanotrasenFactionId = "NanoTrasen";

    public readonly ProtoId<CurrencyPrototype> Currency = "EvolutionPoint";

    [ValidatePrototypeId<EntityPrototype>] EntProtoId _mindRole = "MindRoleChangeling";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingRuleComponent, AfterAntagEntitySelectedEvent>(OnSelectAntag);
        SubscribeLocalEvent<ChangelingRuleComponent, ObjectivesTextPrependEvent>(OnTextPrepend);
    }

    private void OnSelectAntag(EntityUid uid, ChangelingRuleComponent comp, ref AfterAntagEntitySelectedEvent args)
    {
        MakeChangeling(args.EntityUid, comp);
    }
    public bool MakeChangeling(EntityUid target, ChangelingRuleComponent rule)
    {
        if (!_mind.TryGetMind(target, out var mindId, out var mind))
            return false;

        _role.MindAddRole(mindId, _mindRole.Id, mind, true);

        // briefing
        var metaData = MetaData(target);
        var briefing = Loc.GetString("changeling-role-greeting", ("name", metaData?.EntityName ?? "Unknown"));
        var briefingShort = Loc.GetString("changeling-role-greeting-short", ("name", metaData?.EntityName ?? "Unknown"));

        _antag.SendBriefing(target, briefing, Color.Yellow, BriefingSound);

        if (_role.MindHasRole<ChangelingRoleComponent>(mindId, out var mr))
            AddComp(mr.Value, new RoleBriefingComponent { Briefing = briefingShort }, overwrite: true);

        // hivemind stuff
        _npcFaction.RemoveFaction(target, NanotrasenFactionId, false);
        _npcFaction.AddFaction(target, ChangelingFactionId);

        // make sure it's initial chems are set to max
        var changelingComp = EnsureComp<ChangelingComponent>(target);

        // add store
        var store = EnsureComp<StoreComponent>(target);
        foreach (var category in rule.StoreCategories)
            store.Categories.Add(category);
        store.CurrencyWhitelist.Add(Currency);
        store.Balance.Add(Currency, changelingComp.MaxEvolutionPoints);

        //#IMP: Make sure they can use the store button
        var uiComp = EnsureComp<UserInterfaceComponent>(target);
        if (!_userInterfaceSystem.HasUi(target, StoreUiKey.Key, uiComp))
        {
            _userInterfaceSystem.SetUi(target, StoreUiKey.Key, new InterfaceData("StoreBoundUserInterface"));
        }

        rule.ChangelingMinds.Add(mindId);

        return true;
    }

    private void OnTextPrepend(EntityUid uid, ChangelingRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        var mostAbsorbedName = string.Empty;
        var mostStolenName = string.Empty;
        var mostAbsorbed = 0f;
        var mostStolen = 0f;

        var query = EntityQueryEnumerator<ChangelingComponent>();
        while (query.MoveNext(out var user, out var ling))
        {
            if (!_mind.TryGetMind(user, out var mindId, out var mind))
                continue;

            var metaData = MetaData(user);
            if (ling.TotalAbsorbedEntities > mostAbsorbed)
            {
                mostAbsorbed = ling.TotalAbsorbedEntities;
                mostAbsorbedName = _objective.GetTitle((mindId, mind), metaData.EntityName);
            }
            if (ling.TotalStolenDNA > mostStolen)
            {
                mostStolen = ling.TotalStolenDNA;
                mostStolenName = _objective.GetTitle((mindId, mind), metaData.EntityName);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine(Loc.GetString($"roundend-prepend-changeling-absorbed{(!string.IsNullOrWhiteSpace(mostAbsorbedName) ? "-named" : "")}", ("name", mostAbsorbedName), ("number", mostAbsorbed)));
        sb.AppendLine(Loc.GetString($"roundend-prepend-changeling-stolen{(!string.IsNullOrWhiteSpace(mostStolenName) ? "-named" : "")}", ("name", mostStolenName), ("number", mostStolen)));

        args.Text = sb.ToString();
    }
}
