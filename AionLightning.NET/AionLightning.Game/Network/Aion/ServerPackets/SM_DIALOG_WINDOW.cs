using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Opens an NPC dialog window on the client. Opcode 0x3C.
/// dialogId 10 = standard NPC greeting. questId 0 = no active quest context.
/// </summary>
public sealed class SM_DIALOG_WINDOW : AionServerPacket
{
    private readonly int _targetObjectId;
    private readonly int _dialogId;
    private readonly int _questId;

    public SM_DIALOG_WINDOW(int targetObjectId, int dialogId, int questId = 0)
        : base(0x3C)
    {
        _targetObjectId = targetObjectId;
        _dialogId       = dialogId;
        _questId        = questId;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_targetObjectId);
        w.WriteH(_dialogId);
        w.WriteD(_questId);
        w.WriteH(0);
    }
}
