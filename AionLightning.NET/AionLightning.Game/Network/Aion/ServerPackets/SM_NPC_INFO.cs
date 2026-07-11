using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Npc;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_NPC_INFO : AionServerPacket
{
    private readonly int _objectId;
    private readonly Position _pos;
    private readonly NpcTemplate _tpl;
    private readonly byte _npcType;
    private readonly short _state;
    private readonly int _creatorId;
    private readonly string _masterName;
    private readonly int _maxHp;
    private readonly int _currentHp;
    private readonly byte _level;
    private readonly float _moveSpeed;
    private readonly int _targetObjectId;

    public SM_NPC_INFO(Npc npc) : base(0x0E)
    {
        _objectId = npc.ObjectId;
        _pos      = npc.Position;
        _tpl      = npc.Template;
        // npcTypeId: 0=ATTACKABLE, 2=PEACE (dummy AI = peaceful)
        _npcType    = npc.Template.Ai.Equals("dummy", StringComparison.OrdinalIgnoreCase) ? (byte)2 : (byte)0;
        _state      = (short)npc.State;
        _creatorId  = 0;      // wild NPCs have no owner
        _masterName = string.Empty;
        _maxHp      = npc.MaxHp > 0 ? npc.MaxHp : 1;
        _currentHp  = npc.CurrentHp > 0 ? npc.CurrentHp : _maxHp;
        _level      = npc.Template.Level;
        _moveSpeed  = npc.MovementSpeed;
        _targetObjectId = npc.Target?.ObjectId ?? 0;
    }

    /// <summary>M381: summon variant — wires creatorId/masterName to the summon's master so the client
    /// links the spirit to its owner (Java SM_NPC_INFO(Summon, Player) does exactly this).</summary>
    public SM_NPC_INFO(Summon summon) : base(0x0E)
    {
        _objectId = summon.ObjectId;
        _pos      = summon.Position;
        _tpl      = summon.Template;
        // Phase 1 simplification: Java varies npcTypeId per receiving viewer (SUPPORT vs ATTACKABLE
        // based on enemy relation to the master); we always report PEACE-like since summons never
        // attack their own master's faction.
        _npcType    = 2;
        _state      = (short)summon.State;
        _creatorId  = summon.Master?.ObjectId ?? 0;
        _masterName = summon.Master?.Name ?? "LOST";
        _maxHp      = summon.MaxHp > 0 ? summon.MaxHp : 1;
        _currentHp  = summon.CurrentHp > 0 ? summon.CurrentHp : _maxHp;
        _level      = summon.Level;
        _moveSpeed  = summon.MovementSpeed;
        _targetObjectId = summon.Target?.ObjectId ?? 0;
    }

    public override void Write(ref PacketWriter w)
    {
        w.WriteF(_pos.X);
        w.WriteF(_pos.Y);
        w.WriteF(_pos.Z);
        w.WriteD(_objectId);
        w.WriteD(_tpl.NpcId);
        w.WriteD(_tpl.NpcId); // repeated per protocol

        w.WriteC(_npcType);

        w.WriteH(_state); // creature state bitmask (65=normal, 33=fight, 7=dead)
        w.WriteC((byte)_pos.Heading);

        w.WriteD(_tpl.NameId);
        w.WriteD(_tpl.TitleId);

        w.WriteH(0); // unk
        w.WriteC(0); // unk
        w.WriteD(0); // unk

        w.WriteD(_creatorId);
        w.WriteS(_masterName);

        w.WriteC((byte)(100 * _currentHp / _maxHp));
        w.WriteD(_maxHp);
        w.WriteC(_level);

        w.WriteD(0); // no gear mask → no equipment entries
        float boundFront = _tpl.BoundRadius?.Front ?? _tpl.Height;
        w.WriteF(boundFront > 0 ? boundFront : 1.0f);

        w.WriteF(_tpl.Height > 0 ? _tpl.Height : 1.0f);
        w.WriteF(_moveSpeed);

        short delay = _tpl.AttackDelay > 0 ? (short)_tpl.AttackDelay : (short)1500;
        w.WriteH(delay);
        w.WriteH(delay);

        w.WriteC(0); // not a flag, not a new spawn

        // movement target (idle — AI handles movement externally)
        w.WriteF(0f);
        w.WriteF(0f);
        w.WriteF(0f);
        w.WriteC(0); // movement mask

        w.WriteH(0); // static spawn id

        // 8 unknown bytes
        w.WriteC(0); w.WriteC(0); w.WriteC(0); w.WriteC(0);
        w.WriteC(0); w.WriteC(0); w.WriteC(0); w.WriteC(0);

        w.WriteC(0);  // visualState
        w.WriteH(1);  // NpcObjectType: 1 = NORMAL
        w.WriteC(0);  // unk
        w.WriteD(_targetObjectId); // current target objectId
        w.WriteD(0);  // townId
    }
}
