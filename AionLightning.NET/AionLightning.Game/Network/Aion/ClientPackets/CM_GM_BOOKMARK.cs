using AionLightning.Commons.Network;
using AionLightning.Game.Network.Aion.ServerPackets;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>GM panel right-click actions (goto/summon player). Opcode 0x11E.</summary>
public sealed class CM_GM_BOOKMARK : AionClientPacket
{
    private readonly GsClientConnection       _conn;
    private readonly PlayerConnectionRegistry _connRegistry;

    private string _raw = string.Empty;

    public CM_GM_BOOKMARK(GsClientConnection conn, PlayerConnectionRegistry connRegistry)
    {
        _conn         = conn;
        _connRegistry = connRegistry;
    }

    public override void Read(ref PacketReader r) => _raw = r.ReadS();

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var gm = _conn.ActivePlayer;
        if (gm is null) return;

        var parts = _raw.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return;

        string command    = parts[0].ToUpperInvariant();
        string targetName = parts[1].Trim();

        switch (command)
        {
            case "TELEPORTTO":
                await HandleGotoAsync(gm, targetName, ct);
                break;

            case "RECALL":
                await HandleRecallAsync(gm, targetName, ct);
                break;
        }
    }

    private async ValueTask HandleGotoAsync(Model.Player gm, string targetName, CancellationToken ct)
    {
        var target = _connRegistry.GetByName(targetName)?.ActivePlayer;
        if (target is null) return;

        int oldWorldId = gm.Position.WorldId;
        int newWorldId = target.Position.WorldId;

        if (oldWorldId != newWorldId)
        {
            var del = new SM_DELETE(gm.ObjectId);
            foreach (var other in _connRegistry.GetAllExcept(gm.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(del, ct); } catch { }
        }

        gm.Position = target.Position;
        await _conn.SendAsync(new SM_TELEPORT_LOC(newWorldId, target.Position.X, target.Position.Y, target.Position.Z), ct);

        if (oldWorldId != newWorldId)
        {
            var equipment  = gm.Inventory.All.Where(i => i.IsEquipped).ToList();
            var playerInfo = new SM_PLAYER_INFO(gm, gm.Appearance, enemy: false, equipment);
            foreach (var other in _connRegistry.GetAllExcept(gm.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == newWorldId)
                    try { await other.SendAsync(playerInfo, ct); } catch { }
        }
    }

    private async ValueTask HandleRecallAsync(Model.Player gm, string targetName, CancellationToken ct)
    {
        var targetConn = _connRegistry.GetByName(targetName);
        var target     = targetConn?.ActivePlayer;
        if (target is null) return;

        int oldWorldId = target.Position.WorldId;
        int newWorldId = gm.Position.WorldId;

        if (oldWorldId != newWorldId)
        {
            var del = new SM_DELETE(target.ObjectId);
            foreach (var other in _connRegistry.GetAllExcept(target.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == oldWorldId)
                    try { await other.SendAsync(del, ct); } catch { }
        }

        target.Position = gm.Position;
        await targetConn!.SendAsync(
            new SM_TELEPORT_LOC(newWorldId, gm.Position.X, gm.Position.Y, gm.Position.Z), ct);

        if (oldWorldId != newWorldId)
        {
            var equipment  = target.Inventory.All.Where(i => i.IsEquipped).ToList();
            var playerInfo = new SM_PLAYER_INFO(target, target.Appearance, enemy: false, equipment);
            foreach (var other in _connRegistry.GetAllExcept(target.ObjectId))
                if (other.ActivePlayer?.Position.WorldId == newWorldId)
                    try { await other.SendAsync(playerInfo, ct); } catch { }
        }
    }
}
