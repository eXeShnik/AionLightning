using AionLightning.Commons.Network;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Java network.aion.serverpackets.SM_RIFT_ANNOUNCE. Java has 6 action-id variants (rift map, Gelkmaros/
/// Inggison outpost state, RVController vortex-rift open/close, single-rift despawn, Tiamaranta's Eye
/// state). SiegeService ports actionId 1/5 (Silentera Canyon outpost state and Tiamaranta's Eye
/// infiltration-route state); RiftService (the general abyss-invasion rift system) adds actionId
/// 0/2/3/4 here (map overview, master-rift-open, entries-used, despawn) — the RVController-vortex-rift
/// fields these carry (isVortex) are always written as 0/false since the Dimensional Vortex subsystem
/// itself is out of scope (see RiftEnum's doc comment).
/// Opcode 0xEC (4.5-era packet table). TODO: verify opcode against a live 4.6 client capture before enabling.
/// </summary>
public sealed class SM_RIFT_ANNOUNCE : AionServerPacket
{
    private readonly int _actionId;
    private readonly int _gelkmaros, _inggison;
    private readonly int _cl, _cr, _tl, _tr;
    private readonly int[]? _overview;
    private readonly int _objectId, _maxEntries, _usedEntries, _remainTime, _minLevel, _maxLevel;
    private readonly float _x, _y, _z;
    private readonly bool _isMaster;

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

    /// <summary>actionId 0 — rift-count overview for a world (Java's getAnnounceData/calcRiftsData
    /// 8-slot FastMap: [0]=open master count, [1]=vortex-master count (always 0, no vortex here),
    /// [2]/[3]/[4]=duplicated master count (Java writes the same value three times), [5]=open slave
    /// count, [6]=duplicated slave count, [7]=vortex-slave count (always 0)). <paramref name="counts"/>
    /// must have exactly 8 elements.</summary>
    public SM_RIFT_ANNOUNCE(int[] counts) : base(0xEC)
    {
        _actionId = 0;
        _overview = counts;
    }

    /// <summary>actionId 2 — a master rift portal opened (Java's RVController-driven
    /// SM_RIFT_ANNOUNCE(RVController, isMaster=true)). Reports the master NPC's object id, this
    /// rift's max entry count, remaining open time in seconds, entry level range, and world position.</summary>
    public SM_RIFT_ANNOUNCE(int objectId, int maxEntries, int remainTimeSeconds, int minLevel, int maxLevel,
        float x, float y, float z) : base(0xEC)
    {
        _actionId = 2;
        _objectId = objectId;
        _maxEntries = maxEntries;
        _remainTime = remainTimeSeconds;
        _minLevel = minLevel;
        _maxLevel = maxLevel;
        _x = x; _y = y; _z = z;
        _isMaster = true;
    }

    /// <summary>actionId 3 — a rift's used-entry count changed (Java's RVController-driven
    /// SM_RIFT_ANNOUNCE(RVController, isMaster=false)).</summary>
    public SM_RIFT_ANNOUNCE(int objectId, int usedEntries, int remainTimeSeconds) : base(0xEC)
    {
        _actionId = 3;
        _objectId = objectId;
        _usedEntries = usedEntries;
        _remainTime = remainTimeSeconds;
    }

    /// <summary>actionId 4 — a rift portal NPC (master or slave) despawned.</summary>
    public SM_RIFT_ANNOUNCE(int objectId) : base(0xEC)
    {
        _actionId = 4;
        _objectId = objectId;
    }

    public override void Write(ref PacketWriter w)
    {
        switch (_actionId)
        {
            case 0:
                w.WriteH(0x19);
                w.WriteC((byte)_actionId);
                foreach (int value in _overview!)
                    w.WriteD(value);
                break;
            case 1:
                w.WriteH(0x09);
                w.WriteC((byte)_actionId);
                w.WriteD(_gelkmaros);
                w.WriteD(_inggison);
                break;
            case 2:
                w.WriteH(0x23);
                w.WriteC((byte)_actionId);
                w.WriteD(_objectId);
                w.WriteD(_maxEntries);
                w.WriteD(_remainTime);
                w.WriteD(_minLevel);
                w.WriteD(_maxLevel);
                w.WriteF(_x);
                w.WriteF(_y);
                w.WriteF(_z);
                w.WriteC(0); // isVortex — always false, no vortex rifts in this subsystem
                w.WriteC(_isMaster ? (byte)1 : (byte)0);
                break;
            case 3:
                w.WriteH(0x0f);
                w.WriteC((byte)_actionId);
                w.WriteD(_objectId);
                w.WriteD(_usedEntries);
                w.WriteD(_remainTime);
                w.WriteC(0); // isVortex — always false
                w.WriteC(0); // unk
                break;
            case 4:
                w.WriteH(0x05);
                w.WriteC((byte)_actionId);
                w.WriteD(_objectId);
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
