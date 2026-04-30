using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_NPC_INFO : AionServerPacket
{
    private readonly Npc _npc;

    public SM_NPC_INFO(Npc npc) : base(0x0E) => _npc = npc;

    public override void Write(ref PacketWriter w)
    {
        var pos = _npc.Position;
        var tpl = _npc.Template;

        w.WriteF(pos.X);
        w.WriteF(pos.Y);
        w.WriteF(pos.Z);
        w.WriteD(_npc.ObjectId);
        w.WriteD(tpl.NpcId);
        w.WriteD(tpl.NpcId); // repeated per protocol

        // npcTypeId: 0=ATTACKABLE, 2=PEACE (dummy AI = peaceful)
        byte npcType = tpl.Ai.Equals("dummy", StringComparison.OrdinalIgnoreCase) ? (byte)2 : (byte)0;
        w.WriteC(npcType);

        w.WriteH((short)_npc.State); // creature state bitmask (65=normal, 33=fight, 7=dead)
        w.WriteC((byte)pos.Heading);

        w.WriteD(tpl.NameId);
        w.WriteD(tpl.TitleId);

        w.WriteH(0); // unk
        w.WriteC(0); // unk
        w.WriteD(0); // unk

        w.WriteD(0);   // creatorId (no master)
        w.WriteS("");  // masterName (empty)

        int maxHp  = _npc.MaxHp > 0 ? _npc.MaxHp : 1;
        int currHp = _npc.CurrentHp > 0 ? _npc.CurrentHp : maxHp;
        w.WriteC((byte)(100 * currHp / maxHp));
        w.WriteD(maxHp);
        w.WriteC(tpl.Level);

        w.WriteD(0); // no gear mask → no equipment entries
        float boundFront = tpl.BoundRadius?.Front ?? tpl.Height;
        w.WriteF(boundFront > 0 ? boundFront : 1.0f);

        w.WriteF(tpl.Height > 0 ? tpl.Height : 1.0f);
        w.WriteF(_npc.MovementSpeed);

        short delay = tpl.AttackDelay > 0 ? (short)tpl.AttackDelay : (short)1500;
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
        w.WriteD(_npc.Target?.ObjectId ?? 0); // current target objectId
        w.WriteD(0);  // townId
    }
}
