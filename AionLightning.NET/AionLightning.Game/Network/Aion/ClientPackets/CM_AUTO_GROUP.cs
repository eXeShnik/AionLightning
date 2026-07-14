using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Model.AutoGroup;
using AionLightning.Game.Services;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>Client registers/unregisters/confirms/cancels an AutoGroupService matchmaking queue. Opcode 0x16A.</summary>
public sealed class CM_AUTO_GROUP : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly AutoGroupService _autoGroupService;
    private readonly IOptions<AutoGroupOptions> _options;

    private int _instanceMaskId;
    private byte _windowId;
    private byte _entryRequestId;

    public CM_AUTO_GROUP(GsClientConnection conn, AutoGroupService autoGroupService, IOptions<AutoGroupOptions> options)
    {
        _conn             = conn;
        _autoGroupService = autoGroupService;
        _options          = options;
    }

    public override void Read(ref PacketReader r)
    {
        _instanceMaskId = r.ReadD();
        _windowId       = r.ReadC();
        _entryRequestId = r.ReadC();
    }

    public override ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return ValueTask.CompletedTask;

        // Java: CM_AUTO_GROUP itself checks AutoGroupConfig.AUTO_GROUP_ENABLE and bails before touching
        // the service. When disabled we silently drop the request (no queue accumulates, no packet is
        // sent) rather than echoing Java's dev-only chat message, which has no client string id.
        if (!_options.Value.Enable) return ValueTask.CompletedTask;

        switch (_windowId)
        {
            case 100:
                EntryRequestType? entryType = _entryRequestId switch
                {
                    0 => EntryRequestType.NewGroupEntry,
                    1 => EntryRequestType.QuickGroupEntry,
                    2 => EntryRequestType.GroupEntry,
                    _ => null,
                };
                if (entryType is null) break;
                _autoGroupService.RegisterPlayer(player, _instanceMaskId, entryType.Value);
                break;
            case 101:
                _autoGroupService.UnregisterPlayer(player, _instanceMaskId);
                break;
            case 102:
                _autoGroupService.PressEnter(player, _instanceMaskId);
                break;
            case 103:
                _autoGroupService.CancelEnter(player, _instanceMaskId);
                break;
            case 104:
                // Java: DredgionService/KamarBattlefieldService/OphidanBridgeService/IronWallWarFrontService
                // .showWindow — none of those per-battlefield "window" services exist in this port (only
                // the DredgionInstance2 script models the instance itself), so this is a documented no-op.
                break;
            case 105:
                // No-op in Java too (commented-out DredgionRegService call).
                break;
        }

        return ValueTask.CompletedTask;
    }
}
