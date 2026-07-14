using AionLightning.Commons.Network;
using AionLightning.Game.Model.House;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_HOUSE_SCRIPTS — acknowledges/broadcasts a contiguous, inclusive
/// range of a house's decoration-script slots. CM_HOUSE_SCRIPT's single-slot ack always passes
/// from == to == the updated slot. Slot bytes are echoed back exactly as stored (see PlayerScript's class
/// doc — they are never recompressed server-side).
/// Opcode 0x83. // TODO: verify opcode vs live 4.6 client.
/// </summary>
public sealed class SM_HOUSE_SCRIPTS : AionServerPacket
{
    private readonly int _address;
    private readonly PlayerScripts _scripts;
    private readonly int _from;
    private readonly int _to;

    public SM_HOUSE_SCRIPTS(int address, PlayerScripts scripts, int from, int to) : base(0x83)
    {
        _address = address;
        _scripts = scripts;
        _from = from;
        _to = to;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteD(_address);
        w.WriteH(_to - _from + 1);

        for (int position = _from; position <= _to; position++)
        {
            w.WriteC((byte)position);
            var script = _scripts.Get(position);
            byte[]? bytes = script?.CompressedBytes;

            if (bytes is null)
            {
                w.WriteH(-1);
                continue;
            }
            if (bytes.Length == 0)
            {
                w.WriteH(0);
                continue;
            }

            w.WriteH(bytes.Length + 8);
            w.WriteD(bytes.Length);
            w.WriteD(script!.UncompressedSize);
            w.WriteB(bytes);
        }
    }
}
