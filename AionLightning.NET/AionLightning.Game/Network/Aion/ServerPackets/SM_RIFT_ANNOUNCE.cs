using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_RIFT_ANNOUNCE. Java has 6 action-id variants (rift map, Gelkmaros/
/// Inggison outpost state, RVController vortex-rift open/close, single-rift despawn, Tiamaranta's Eye
/// state) — only the two this port's siege phase actually broadcasts (Silentera Canyon outpost state
/// and Tiamaranta's Eye infiltration-route state) are ported; the RVController-vortex-rift variant
/// belongs to a separate rifting subsystem that isn't ported.
/// Opcode 0xEC (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_RIFT_ANNOUNCE : AionServerPacket
{
    private readonly int _actionId;
    private readonly int _gelkmaros, _inggison;
    private readonly int _cl, _cr, _tl, _tr;

    /// <summary>actionId 1 — Gelkmaros/Inggison Silentera Canyon infiltration-route (outpost) state.</summary>
    public SM_RIFT_ANNOUNCE(bool gelkmaros, bool inggison) : base(0xEC)
    {
        _actionId = 1;
        _gelkmaros = gelkmaros ? 1 : 0;
        _inggison = inggison ? 1 : 0;
    }

    /// <summary>
    /// actionId 5 — Tiamaranta's Eye infiltration-route state. cl/cr = Western/Eastern Tiamaranta's Eye
    /// entrance (Center left/right); tl/tr = Elyos/Asmodian Eye Abyss Gate (Top left/right).
    /// </summary>
    public SM_RIFT_ANNOUNCE(bool cl, bool cr, bool tl, bool tr) : base(0xEC)
    {
        _actionId = 5;
        _cl = cl ? 1 : 0;
        _cr = cr ? 1 : 0;
        _tl = tl ? 1 : 0;
        _tr = tr ? 1 : 0;
    }

    public override void Write(ref PacketWriter w)
    {
        switch (_actionId)
        {
            case 1:
                w.WriteH(0x09);
                w.WriteC((byte)_actionId);
                w.WriteD(_gelkmaros);
                w.WriteD(_inggison);
                break;
            case 5:
                w.WriteH(0x05);
                w.WriteC((byte)_actionId);
                w.WriteC((byte)_cl);
                w.WriteC((byte)_cr);
                w.WriteC((byte)_tl);
                w.WriteC((byte)_tr);
                break;
        }
    }
}
