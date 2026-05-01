using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;

namespace AionLightning.Game.Network.Aion.ClientPackets;

/// <summary>
/// LFG board operations. Opcode 0x2EF.
/// 0x00/0x0A = browse recruiter board; 0x04 = browse applicant board;
/// 0x02 = post group as recruiter; 0x06 = post self as applicant;
/// 0x01 = remove recruiter post; 0x03 = update recruiter message; 0x05 = remove applicant post.
/// </summary>
public sealed class CM_FIND_GROUP : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly FindGroupService   _lfgService;

    private byte   _action;
    private int    _playerObjId;
    private string _message = string.Empty;
    private byte   _groupType;
    private int    _unk;

    public CM_FIND_GROUP(GsClientConnection conn, FindGroupService lfgService)
    {
        _conn       = conn;
        _lfgService = lfgService;
    }

    public override void Read(ref PacketReader r)
    {
        _action = (byte)r.ReadC();
        switch (_action)
        {
            case 0x01: // delete recruiter
                _playerObjId = r.ReadD();
                _unk         = r.ReadD();
                break;
            case 0x02: // post as recruiter
                _playerObjId = r.ReadD();
                _message     = r.ReadS();
                _groupType   = (byte)r.ReadC();
                break;
            case 0x03: // update recruiter message
                _playerObjId = r.ReadD();
                _unk         = r.ReadD();
                _message     = r.ReadS();
                _groupType   = (byte)r.ReadC();
                break;
            case 0x05: // delete applicant
                _playerObjId = r.ReadD();
                break;
            case 0x06: // post as applicant
                _playerObjId = r.ReadD();
                _message     = r.ReadS();
                _groupType   = (byte)r.ReadC();
                r.ReadC(); // classId (read but resolved server-side)
                r.ReadC(); // level   (read but resolved server-side)
                break;
            // 0x00, 0x04, 0x07, 0x0A: no extra data to read
        }
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null) return;

        switch (_action)
        {
            case 0x00:
            case 0x0A:
            {
                var list = _lfgService.GetRecruiters(player.Race);
                await _conn.SendAsync(new SM_FIND_GROUP(0x00, list), ct);
                break;
            }
            case 0x04:
            {
                var list = _lfgService.GetApplicants(player.Race);
                await _conn.SendAsync(new SM_FIND_GROUP(0x04, list), ct);
                break;
            }
            case 0x02:
            {
                var group = player.Group;
                var entry = new FindGroupEntry(
                    objectId:   group?.GroupId ?? player.ObjectId,
                    name:       player.Name,
                    message:    _message,
                    groupType:  _groupType,
                    memberSize: (byte)(group?.Members.Count ?? 1),
                    minLevel:   player.Level,
                    maxLevel:   player.Level,
                    classId:    (byte)player.PlayerClass,
                    isPlayer:   group is null);
                _lfgService.AddRecruiter(player.Race, entry);
                await _conn.SendAsync(new SM_FIND_GROUP(0x02, new[] { entry }), ct);
                break;
            }
            case 0x06:
            {
                var entry = new FindGroupEntry(
                    objectId:   player.ObjectId,
                    name:       player.Name,
                    message:    _message,
                    groupType:  _groupType,
                    memberSize: 1,
                    minLevel:   player.Level,
                    maxLevel:   player.Level,
                    classId:    (byte)player.PlayerClass,
                    isPlayer:   true);
                _lfgService.AddApplicant(player.Race, entry);
                await _conn.SendAsync(new SM_FIND_GROUP(0x06, new[] { entry }), ct);
                break;
            }
            case 0x01:
            {
                _lfgService.RemoveRecruiter(player.Race, _playerObjId);
                await _conn.SendAsync(new SM_FIND_GROUP(0x01, _playerObjId, _unk), ct);
                break;
            }
            case 0x05:
            {
                _lfgService.RemoveApplicant(player.Race, _playerObjId);
                await _conn.SendAsync(new SM_FIND_GROUP(0x05, _playerObjId), ct);
                break;
            }
            case 0x03:
            {
                var existing = _lfgService.GetRecruiter(player.Race, _playerObjId);
                existing?.UpdateMessage(_message);
                await _conn.SendAsync(new SM_FIND_GROUP(0x03, _playerObjId, _unk), ct);
                break;
            }
        }
    }
}
