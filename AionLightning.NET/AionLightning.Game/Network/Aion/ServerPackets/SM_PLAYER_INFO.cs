using AionLightning.Commons.Network;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Item;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Makes a player visible to another client — sent on spawn and on entering the other player's range.
/// Port of Java SM_PLAYER_INFO.writeImpl().
/// </summary>
public sealed class SM_PLAYER_INFO : AionServerPacket
{
    private readonly Player _player;
    private readonly PlayerAppearance _appearance;
    private readonly bool _enemy;
    private readonly IReadOnlyList<Item> _equipment;

    public SM_PLAYER_INFO(Player player, PlayerAppearance appearance, bool enemy,
        IEnumerable<Item>? equipment = null)
        : base(0x20)
    {
        _player     = player;
        _appearance = appearance;
        _enemy      = enemy;
        _equipment  = equipment?.ToList() ?? [];
    }

    public override void Write(ref PacketWriter w)
    {
        var p = _player;
        var a = _appearance;

        int raceId   = (int)p.Race;
        int genderId = (int)p.Gender;
        int templateId = 100000 + raceId * 2 + genderId;

        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);
        w.WriteD(p.ObjectId);

        w.WriteD(templateId);   // model template
        w.WriteD(0);            // robotId (4.5)
        w.WriteD(templateId);   // base model

        w.WriteC(0x00);         // pet info
        // M302: write active transform model (0 = no transform; shapechange/polymorph/deform sets TransformModelId)
        w.WriteD(p.TransformModelId);
        w.WriteC(_enemy ? (byte)0x00 : (byte)0x26); // visible flags

        w.WriteC((byte)raceId);
        w.WriteC((byte)p.PlayerClass);
        w.WriteC((byte)genderId);
        w.WriteH((short)p.State);

        w.WriteB(new byte[8]);  // unk

        w.WriteC((byte)p.Position.Heading);
        w.WriteS(p.Name);
        w.WriteH(p.TitleId >= 0 ? (short)p.TitleId : (short)0);
        w.WriteH(0);            // mentor flag
        w.WriteH(0);            // casting skill id

        // No legion — 12 zero bytes + writeH(0) in legion path
        w.WriteB(new byte[12]);

        int maxHp   = p.MaxHp > 0 ? p.MaxHp : 1;
        int currHp  = p.CurrentHp > 0 ? p.CurrentHp : 1;
        w.WriteC((byte)(100 * currHp / maxHp));
        w.WriteH(0);            // current DP
        w.WriteC(0x00);         // unk

        // Equipment mask: OR of each equipped slot id
        int mask = 0;
        foreach (var item in _equipment)
            mask |= item.Slot;
        w.WriteD(mask);

        // Per-item entries: templateId + godstone + dye + enchant glow
        foreach (var item in _equipment)
        {
            w.WriteD(item.ItemId);
            w.WriteD(0); // no godstone
            w.WriteD(0); // no dye
            w.WriteD(item.EnchantLevel >= 15 ? 1 : 0);
        }

        // Appearance
        w.WriteD(a.SkinRgb);
        w.WriteD(a.HairRgb);
        w.WriteD(a.EyeRgb);
        w.WriteD(a.LipRgb);
        w.WriteC((byte)a.Face);
        w.WriteC((byte)a.Hair);
        w.WriteC((byte)a.Deco);
        w.WriteC((byte)a.Tattoo);
        w.WriteC((byte)a.FaceContour);
        w.WriteC((byte)a.Expression);
        w.WriteC((byte)(genderId == 1 ? 6 : 5)); // gender marker
        w.WriteC((byte)a.JawLine);
        w.WriteC((byte)a.Forehead);
        w.WriteC((byte)a.EyeHeight);
        w.WriteC((byte)a.EyeSpace);
        w.WriteC((byte)a.EyeWidth);
        w.WriteC((byte)a.EyeSize);
        w.WriteC((byte)a.EyeShape);
        w.WriteC((byte)a.EyeAngle);
        w.WriteC((byte)a.BrowHeight);
        w.WriteC((byte)a.BrowAngle);
        w.WriteC((byte)a.BrowShape);
        w.WriteC((byte)a.Nose);
        w.WriteC((byte)a.NoseBridge);
        w.WriteC((byte)a.NoseWidth);
        w.WriteC((byte)a.NoseTip);
        w.WriteC((byte)a.Cheek);
        w.WriteC((byte)a.LipHeight);
        w.WriteC((byte)a.MouthSize);
        w.WriteC((byte)a.LipSize);
        w.WriteC((byte)a.Smile);
        w.WriteC((byte)a.LipShape);
        w.WriteC((byte)a.JawHeight);
        w.WriteC((byte)a.ChinJut);
        w.WriteC((byte)a.EarShape);
        w.WriteC((byte)a.HeadSize);
        w.WriteC((byte)a.Neck);
        w.WriteC((byte)a.NeckLength);
        w.WriteC((byte)a.ShoulderSize);
        w.WriteC((byte)a.Torso);
        w.WriteC((byte)a.Chest);
        w.WriteC((byte)a.Waist);
        w.WriteC((byte)a.Hips);
        w.WriteC((byte)a.ArmThickness);
        w.WriteC((byte)a.HandSize);
        w.WriteC((byte)a.LegThickness);
        w.WriteC((byte)a.FootSize);
        w.WriteC((byte)a.FacialRate);
        w.WriteC(0x00);
        w.WriteC((byte)a.ArmLength);
        w.WriteC((byte)a.LegLength);
        w.WriteC((byte)a.Shoulders);
        w.WriteC((byte)a.FaceShape);
        w.WriteC(0x00);

        w.WriteC((byte)a.Voice);
        w.WriteF(a.Height);
        w.WriteF(0.25f);        // scale
        w.WriteF(2.0f);         // gravity
        w.WriteF(p.MovementSpeed);

        w.WriteH((short)p.CurrentAttackSpeed); // attack speed base
        w.WriteH((short)p.CurrentAttackSpeed); // attack speed current
        w.WriteC(0);            // port animation

        w.WriteS(string.Empty); // private store message

        // Movement — standing still
        w.WriteF(0); w.WriteF(0); w.WriteF(0);
        w.WriteF(p.Position.X);
        w.WriteF(p.Position.Y);
        w.WriteF(p.Position.Z);
        w.WriteC(0x00);         // move type stop

        if (p.IsUsingFlyTeleport)
        {
            w.WriteD(p.FlightTeleportId);
            w.WriteD(p.FlightDistance);
        }
        // note: Java's else-if windstream branch (writes windstreamPath.teleportId/distance) is not
        // ported — no windstream player-mode/state exists in this port yet.

        w.WriteC(0);            // visual state
        w.WriteS(string.Empty); // player note

        w.WriteH(p.Level);
        w.WriteH((short)p.DisplaySettings);
        w.WriteH((short)p.DenySettings);
        w.WriteH(1);            // abyss rank 1

        w.WriteH(0);            // unk
        w.WriteD(0);            // target object id
        w.WriteC(0);            // suspect id
        w.WriteD(0);
        w.WriteC(0);            // mentor
        w.WriteD(0);            // house owner id
        w.WriteD(1);            // item effect id
        w.WriteC((byte)(raceId == 0 ? 3 : 5)); // language: asmo=3, ely=5
    }
}
