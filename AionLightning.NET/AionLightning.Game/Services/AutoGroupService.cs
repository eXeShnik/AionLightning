using AionLightning.Game.Configs.Options;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.AutoGroup;
using AionLightning.Game.Model.Group;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WorldMapInstanceType = AionLightning.Game.World.WorldMapInstance;

namespace AionLightning.Game.Services;

/// <summary>
/// PvP/co-op instance matchmaking (Java <c>services.AutoGroupService</c> + <c>model.autogroup.*</c>).
/// Players register solo or as an existing group for a queue (<see cref="AutoGroupTemplate"/>, keyed by
/// mask id); once a queue has enough eligible waiting members it reserves a fresh instance channel
/// (<see cref="InstanceService.GetNextAvailableInstance"/>) and prompts every matched member to confirm
/// entry (SM_AUTO_GROUP windowId 4). Confirming (<see cref="PressEnter"/>) auto-forms a brand-new team
/// for the channel — first two arrivals become a <see cref="GroupService"/> group (or, for 12-seat
/// battlefield/dredgion queues, an <see cref="AllianceService"/> alliance from the first arrival on —
/// see <see cref="FormTeamOnEnter"/>) — and teleports the player in via <see cref="TeleportService"/>.
/// Channel teardown once seated is left entirely to the existing generic
/// <see cref="EmptyInstanceCheckerService"/> (registering the auto-formed team marks the channel
/// team-registered, so it's destroyed as soon as everyone registered has left/logged out) — this
/// service only has to destroy a channel itself while it's still in the pre-entry "waiting to be
/// confirmed" phase (see <see cref="CancelEnter"/>/<see cref="OnPlayerLogout"/>).
///
/// Simplifications versus Java (all safe: gated behind <see cref="AutoGroupOptions.Enable"/>, default
/// false):
/// <list type="bullet">
/// <item>One active queue registration per player at a time (Java's <c>LookingForParty</c> tracks a
/// list of simultaneous queue registrations per player) — registering while already queued/pending is
/// rejected the same way Java rejects a duplicate/penalized registration.</item>
/// <item>NEW_GROUP_ENTRY and QUICK_GROUP_ENTRY share one FIFO queue per mask id — Java's quick-entry
/// preferentially joins an already-forming instance within its time window, kept separate from the
/// new-instance FIFO. This port always pops FIFO regardless of entry type.</item>
/// <item>Team composition inside an instance is generic (first-come grouping/alliancing) rather than
/// Java's per-category race-balanced slotting (Dredgion/Kamar/Ophidan/IronWall split factions;
/// PvP FFA/solo/Harmony/Glory arenas balance opposing sides) — see <see cref="AutoGroupCategory"/>'s
/// doc. Anti-abuse gates (harmony arena ticket-item requirement, per-instance re-entry cooldowns via
/// Java's now-unported <c>InstanceCooltime</c>/<c>PortalCooldownList</c>) are not enforced.</item>
/// <item>Entry position resolves via the queue's own anchor NPC (<see cref="AutoGroupTemplate.NpcIds"/>)
/// through the existing <see cref="PortalData"/> lookup, rather than Java's dedicated world+dialogId=10000
/// "generic instance entrance" portal index (not modelled in this port's <see cref="PortalData"/>).
/// Anchor-less queues (Kamar/IronWall, whose auto_group.xml row carries no npc_ids) fall back to world
/// origin (0,0,0) until that data is ported.</item>
/// </list>
/// </summary>
public sealed class AutoGroupService
{
    private const long PenaltyMs = 10_000;

    private sealed record ActiveRegistration(int InstanceMaskId, EntryRequestType EntryType, List<int> MemberObjectIds);

    private sealed class AutoGroupSession
    {
        public required AutoGroupTemplate Template;
        public required AutoGroupMetadata.Entry Metadata;
        public required WorldMapInstanceType Instance;
        public readonly HashSet<int> PendingObjectIds = new();
        public readonly List<Player> EnteredPlayers = new();
    }

    private readonly IDataManager _dataManager;
    private readonly GroupService _groupService;
    private readonly AllianceService _allianceService;
    private readonly InstanceService _instanceService;
    private readonly TeleportService _teleportService;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IOptions<AutoGroupOptions> _options;
    private readonly ILogger<AutoGroupService> _log;

    private readonly object _gate = new();
    private readonly Dictionary<int, ActiveRegistration> _searchers = new();       // objectId -> queue wait entry
    private readonly Dictionary<int, LinkedList<int>> _queues = new();             // maskId -> FIFO of leader objectIds
    private readonly Dictionary<int, long> _penalties = new();                    // objectId -> unix-ms penalty expiry
    private readonly Dictionary<int, AutoGroupSession> _sessions = new();         // instanceId -> forming/live session
    private readonly Dictionary<int, int> _pendingSessionByPlayer = new();        // objectId -> instanceId (awaiting press-enter)

    public AutoGroupService(IDataManager dataManager, GroupService groupService, AllianceService allianceService,
        InstanceService instanceService, TeleportService teleportService, PlayerConnectionRegistry connRegistry,
        IOptions<AutoGroupOptions> options, ILogger<AutoGroupService> log)
    {
        _dataManager      = dataManager;
        _groupService     = groupService;
        _allianceService  = allianceService;
        _instanceService  = instanceService;
        _teleportService  = teleportService;
        _connRegistry     = connRegistry;
        _options          = options;
        _log              = log;
    }

    /// <summary>Java <c>startLooking</c>. No-op (silently) when the queue is unknown/ineligible, matching Java.</summary>
    public void RegisterPlayer(Player player, int instanceMaskId, EntryRequestType entryType)
    {
        if (!_options.Value.Enable) return;

        var template = _dataManager.AutoGroups.GetByMaskId(instanceMaskId);
        if (template is null || !AutoGroupMetadata.TryGet(instanceMaskId, out var metadata)) return;

        lock (_gate)
        {
            if (!CanEnter(player, entryType, template, out var rejection))
            {
                if (rejection is not null) Send(player.ObjectId, rejection);
                return;
            }

            if (IsPenalized(player.ObjectId) || _searchers.ContainsKey(player.ObjectId))
            {
                Send(player.ObjectId, SM_SYSTEM_MESSAGE.AutoGroupAlreadyRegistered(template.InstanceMapId));
                return;
            }

            var members = entryType == EntryRequestType.GroupEntry && player.Group is { } group
                ? group.Members.Select(m => m.ObjectId).ToList()
                : new List<int> { player.ObjectId };

            _searchers[player.ObjectId] = new ActiveRegistration(instanceMaskId, entryType, members);
            if (!_queues.TryGetValue(instanceMaskId, out var queue))
                _queues[instanceMaskId] = queue = new LinkedList<int>();
            queue.AddLast(player.ObjectId);

            bool showEntryIcon = metadata.Category is AutoGroupCategory.Dredgion or AutoGroupCategory.Kamar
                or AutoGroupCategory.Ophidan or AutoGroupCategory.IronWall;

            foreach (var memberId in members)
            {
                if (showEntryIcon) Send(memberId, new SM_AUTO_GROUP(template, 6, close: true));
                Send(memberId, SM_SYSTEM_MESSAGE.AutoGroupRegistrationSuccess());
                // Java overloads the windowId=1 "waitTime" wire slot to actually carry the entry-request-type id here.
                Send(memberId, new SM_AUTO_GROUP(template, 1, waitTime: (int)entryType, name: player.Name));
            }

            RunMatcher(instanceMaskId, template, metadata);
        }
    }

    /// <summary>Java <c>unregisterLooking</c> — leaves the queue before a match was found.</summary>
    public void UnregisterPlayer(Player player, int instanceMaskId)
    {
        lock (_gate)
        {
            if (!_searchers.TryGetValue(player.ObjectId, out var reg) || reg.InstanceMaskId != instanceMaskId) return;

            var template = _dataManager.AutoGroups.GetByMaskId(instanceMaskId);
            if (template is null) return;

            _searchers.Remove(player.ObjectId);
            if (_queues.TryGetValue(instanceMaskId, out var queue)) queue.Remove(player.ObjectId);
            StartPenalty(player.ObjectId);

            foreach (var memberId in reg.MemberObjectIds)
                Send(memberId, new SM_AUTO_GROUP(template, 2));
        }
    }

    /// <summary>Java <c>pressEnter</c> — confirms entry into a matched, already-created channel.</summary>
    public void PressEnter(Player player, int instanceMaskId)
    {
        lock (_gate)
        {
            if (!_pendingSessionByPlayer.TryGetValue(player.ObjectId, out int instanceId)) return;
            if (!_sessions.TryGetValue(instanceId, out var session) || session.Template.MaskId != instanceMaskId) return;
            if (!session.PendingObjectIds.Remove(player.ObjectId)) return;

            _pendingSessionByPlayer.Remove(player.ObjectId);

            if (player.Group is not null) _groupService.LeaveGroup(player);
            if (player.Alliance is not null) _allianceService.RemoveMember(player);

            FormTeamOnEnter(session, player);

            var (x, y, z, heading) = ResolveEntryPosition(session.Template, player.Race);
            _ = _teleportService.TeleportToAsync(player, session.Template.InstanceMapId, session.Instance.InstanceId,
                x, y, z, heading, portAnimation: 0, CancellationToken.None);

            Send(player.ObjectId, new SM_AUTO_GROUP(session.Template, 5));
        }
    }

    /// <summary>Java <c>cancelEnter</c> — declines the match; destroys the reserved channel if nobody else is seated.</summary>
    public void CancelEnter(Player player, int instanceMaskId)
    {
        lock (_gate)
        {
            if (!_pendingSessionByPlayer.TryGetValue(player.ObjectId, out int instanceId)) return;
            if (!_sessions.TryGetValue(instanceId, out var session) || session.Template.MaskId != instanceMaskId) return;

            if (session.PendingObjectIds.Remove(player.ObjectId))
                StartPenalty(player.ObjectId);
            _pendingSessionByPlayer.Remove(player.ObjectId);

            Send(player.ObjectId, new SM_AUTO_GROUP(session.Template, 2));

            if (session.EnteredPlayers.Count == 0 && session.PendingObjectIds.Count == 0)
            {
                _sessions.Remove(instanceId);
                _instanceService.DestroyInstance(session.Instance);
            }
        }
    }

    /// <summary>Java <c>onPlayerLogOut</c>'s queue/pending-enter cleanup (post-seat logout is already
    /// handled generically by <see cref="EmptyInstanceCheckerService"/>).</summary>
    public void OnPlayerLogout(Player player)
    {
        lock (_gate)
        {
            if (_searchers.TryGetValue(player.ObjectId, out var reg))
            {
                _searchers.Remove(player.ObjectId);
                if (_queues.TryGetValue(reg.InstanceMaskId, out var queue)) queue.Remove(player.ObjectId);
            }

            if (_pendingSessionByPlayer.Remove(player.ObjectId, out int instanceId)
                && _sessions.TryGetValue(instanceId, out var session))
            {
                session.PendingObjectIds.Remove(player.ObjectId);
                if (session.EnteredPlayers.Count == 0 && session.PendingObjectIds.Count == 0)
                {
                    _sessions.Remove(instanceId);
                    _instanceService.DestroyInstance(session.Instance);
                }
            }
        }
    }

    /// <summary>Java <c>startSort</c>, collapsed to a single FIFO-per-mask-id fill loop (see class doc).</summary>
    private void RunMatcher(int instanceMaskId, AutoGroupTemplate template, AutoGroupMetadata.Entry metadata)
    {
        if (!_queues.TryGetValue(instanceMaskId, out var queue)) return;

        while (queue.Count > 0 && queue.Sum(id => _searchers[id].MemberObjectIds.Count) >= metadata.PlayerSize)
        {
            var matched = new List<ActiveRegistration>();
            int seats = metadata.PlayerSize;
            var node = queue.First;
            while (node is not null && seats > 0)
            {
                var next = node.Next;
                var reg = _searchers[node.Value];
                if (reg.MemberObjectIds.Count <= seats)
                {
                    matched.Add(reg);
                    seats -= reg.MemberObjectIds.Count;
                    queue.Remove(node);
                    _searchers.Remove(node.Value);
                }
                node = next;
            }

            if (matched.Count == 0) break; // remaining entries individually exceed capacity — never matches, same as Java

            CreateSessionAndPrompt(template, metadata, matched);
        }
    }

    private void CreateSessionAndPrompt(AutoGroupTemplate template, AutoGroupMetadata.Entry metadata, List<ActiveRegistration> matched)
    {
        var instance = _instanceService.GetNextAvailableInstance(template.InstanceMapId);
        var session = new AutoGroupSession { Template = template, Metadata = metadata, Instance = instance };
        _sessions[instance.InstanceId] = session;

        foreach (var reg in matched)
        foreach (var memberId in reg.MemberObjectIds)
        {
            session.PendingObjectIds.Add(memberId);
            _pendingSessionByPlayer[memberId] = instance.InstanceId;
            Send(memberId, new SM_AUTO_GROUP(template, 4));
        }

        _log.LogInformation("AutoGroupService: matched {Count} player(s) into new channel {World}:{Instance} for queue {MaskId}",
            session.PendingObjectIds.Count, instance.WorldId, instance.InstanceId, template.MaskId);
    }

    /// <summary>
    /// Java <c>AutoGeneralInstance.onEnterInstance</c>'s "first two arrivals form a fresh
    /// <c>TeamType.AUTO_GROUP</c> group, later arrivals join it" rule, generalised to every queue.
    /// 12-seat queues (Dredgion/Kamar/Ophidan/IronWall) go straight to an alliance from the first
    /// arrival on so a 7th solo entrant never has to dissolve/escalate a full 6-seat group mid-session.
    /// </summary>
    private void FormTeamOnEnter(AutoGroupSession session, Player player)
    {
        if (session.EnteredPlayers.Count == 0)
        {
            session.EnteredPlayers.Add(player);
            return;
        }

        var anchor = session.EnteredPlayers[0];

        if (session.Metadata.PlayerSize > PlayerGroup.MaxMembers)
        {
            if (anchor.Alliance is not { } alliance)
            {
                alliance = _allianceService.CreateAlliance(anchor, Enumerable.Empty<Player>());
                _instanceService.RegisterAllianceWithInstance(session.Instance, alliance);
            }
            _allianceService.AddMember(alliance, player);
        }
        else if (anchor.Group is { } group)
        {
            _groupService.JoinGroup(group, player);
        }
        else
        {
            var newGroup = _groupService.CreateGroup(anchor, player);
            _instanceService.RegisterGroupWithInstance(session.Instance, newGroup);
        }

        session.EnteredPlayers.Add(player);
    }

    /// <summary>Java <c>canEnter</c>: level range + group-entry leader/member eligibility. Register-flag-off
    /// rejections are silent (no packet), matching Java; level/leader/member rejections carry a message.</summary>
    private bool CanEnter(Player player, EntryRequestType entryType, AutoGroupTemplate template, out SM_SYSTEM_MESSAGE? rejection)
    {
        rejection = null;

        if (player.Level < template.MinLevel || player.Level > template.MaxLevel)
        {
            rejection = SM_SYSTEM_MESSAGE.AutoGroupLevelRestricted();
            return false;
        }

        switch (entryType)
        {
            case EntryRequestType.NewGroupEntry:
                if (!template.RegisterNew) return false;
                break;
            case EntryRequestType.QuickGroupEntry:
                if (!template.RegisterQuick) return false;
                break;
            case EntryRequestType.GroupEntry:
                if (!template.RegisterGroup) return false;
                if (player.Group is not { } group || group.LeaderObjectId != player.ObjectId)
                {
                    rejection = SM_SYSTEM_MESSAGE.AutoGroupNotLeader();
                    return false;
                }
                foreach (var member in group.Members)
                {
                    if (member.ObjectId == player.ObjectId) continue;
                    if (member.Level < template.MinLevel || member.Level > template.MaxLevel)
                    {
                        rejection = SM_SYSTEM_MESSAGE.AutoGroupMemberCantEnter(member.Name);
                        return false;
                    }
                    if (_searchers.ContainsKey(member.ObjectId))
                    {
                        rejection = SM_SYSTEM_MESSAGE.AutoGroupMemberCantEnter(member.Name);
                        return false;
                    }
                }
                break;
        }

        return true;
    }

    private (float X, float Y, float Z, byte Heading) ResolveEntryPosition(AutoGroupTemplate template, Race race)
    {
        foreach (var npcId in template.NpcIds)
        {
            var loc = _dataManager.Portals.GetPortalLocation(npcId, race);
            if (loc is not null) return (loc.Value.X, loc.Value.Y, loc.Value.Z, loc.Value.Heading);
        }
        return (0f, 0f, 0f, 0);
    }

    private bool IsPenalized(int objectId)
    {
        if (!_penalties.TryGetValue(objectId, out long expiry)) return false;
        if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= expiry) { _penalties.Remove(objectId); return false; }
        return true;
    }

    private void StartPenalty(int objectId) => _penalties[objectId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + PenaltyMs;

    private void Send(int objectId, SM_AUTO_GROUP packet)
    {
        var conn = _connRegistry.Get(objectId);
        if (conn is not null) _ = conn.SendAsync(packet, CancellationToken.None);
    }

    private void Send(int objectId, SM_SYSTEM_MESSAGE packet)
    {
        var conn = _connRegistry.Get(objectId);
        if (conn is not null) _ = conn.SendAsync(packet, CancellationToken.None);
    }
}
