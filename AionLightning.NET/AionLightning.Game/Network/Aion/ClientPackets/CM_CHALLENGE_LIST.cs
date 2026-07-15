using AionLightning.Commons.Network;
using AionLightning.Game.Model.Templates.Challenge;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client requests the legion/town challenge task list. Opcode 0x18A.</summary>
public sealed class CM_CHALLENGE_LIST : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly ChallengeTaskService _challengeTaskService;

    private int _taskOwner;
    private int _ownerTypeId;

    public CM_CHALLENGE_LIST(GsClientConnection conn, ChallengeTaskService challengeTaskService)
    {
        _conn = conn;
        _challengeTaskService = challengeTaskService;
    }

    public override void Read(ref PacketReader r)
    {
        r.ReadC();               // action
        _taskOwner = r.ReadD();
        _ownerTypeId = r.ReadC();
        r.ReadD();                // playerId
        r.ReadD();                // dateSince
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        var type = _ownerTypeId == (int)ChallengeType.LEGION ? ChallengeType.LEGION : ChallengeType.TOWN;
        if (type == ChallengeType.LEGION && player.Legion is null) return;

        await _challengeTaskService.ShowTaskListAsync(_conn, player, type, _taskOwner, ct);
    }
}
