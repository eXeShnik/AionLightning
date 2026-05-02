using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client deactivates a toggle skill (stance/chant off). Opcode 0xE0.</summary>
public sealed class CM_TOGGLE_SKILL_DEACTIVATE : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private short _skillId;

    public CM_TOGGLE_SKILL_DEACTIVATE(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _skillId = r.ReadH();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        // Clear the stance indicator
        await _conn.SendAsync(new SM_PLAYER_STANCE(player.ObjectId, 0), ct);

        // Remove the associated buff effect and broadcast the updated list
        player.RemoveEffectBySkillId(_skillId);

        int worldId  = player.Position.WorldId;
        var abnormal = new SM_ABNORMAL_EFFECT(player.ObjectId, isPlayer: true, player.GetActiveEffects());
        foreach (var conn in _connRegistry.GetAll())
            if (conn.ActivePlayer?.Position.WorldId == worldId)
                try { await conn.SendAsync(abnormal, ct); } catch { }
    }
}
