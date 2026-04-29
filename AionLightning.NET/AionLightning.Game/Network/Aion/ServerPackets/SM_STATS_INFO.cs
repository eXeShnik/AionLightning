using AionLightning.Commons.Network;
using AionLightning.Game.Model;

namespace AionLightning.Game.Network.Aion.ServerPackets;

/// <summary>
/// Sends player stat sheet. All values are stubs sufficient to make the client functional.
/// A full implementation requires the stat calculation system (out of M5 scope).
/// </summary>
public sealed class SM_STATS_INFO : AionServerPacket
{
    private readonly Player _player;

    public SM_STATS_INFO(Player player) : base(0x01) => _player = player;

    public override void Write(ref PacketWriter w)
    {
        var p = _player;
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int gameTime = (int)(DateTime.UtcNow - epoch).TotalMinutes;

        w.WriteD(p.ObjectId);
        w.WriteD(gameTime);

        // Current attributes (power, health, accuracy, agility, knowledge, will)
        w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100);

        // Elemental resistances (water, wind, earth, fire, light, dark)
        w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0);

        w.WriteH(p.Level);
        w.WriteH(0); w.WriteH(0); w.WriteH(0); // unk

        w.WriteQ(0); // exp needed
        w.WriteQ(0); // exp recoverable
        w.WriteQ(0); // exp current

        w.WriteD(0); // unk

        int maxHp = p.MaxHp > 0 ? p.MaxHp : 1000;
        int curHp = p.CurrentHp > 0 ? p.CurrentHp : maxHp;
        int maxMp = p.MaxMp > 0 ? p.MaxMp : 500;
        int curMp = p.CurrentMp > 0 ? p.CurrentMp : maxMp;

        w.WriteD(maxHp); w.WriteD(curHp);
        w.WriteD(maxMp); w.WriteD(curMp);
        w.WriteH(6000); w.WriteH(0);   // max DP, current DP
        w.WriteD(60);   w.WriteD(60);  // max fly time, current fly time
        w.WriteH(0);                   // fly state

        w.WriteH(100); w.WriteH(0);    // main/off-hand P-attack
        w.WriteH(0);                   // unk 3.0
        w.WriteD(100);                 // P-def
        w.WriteH(100); w.WriteH(0);    // main/off-hand M-attack
        w.WriteD(100);                 // M-def
        w.WriteH(0); w.WriteH(0);      // M-resist, unk 3.0
        w.WriteF(5.0f);                // attack range
        w.WriteH(1500);                // attack speed
        w.WriteH(100);                 // evasion
        w.WriteH(0); w.WriteH(0);      // parry, block
        w.WriteH(0); w.WriteH(0);      // main/off-hand P-crit
        w.WriteH(0); w.WriteH(0);      // main/off-hand P-accuracy
        w.WriteH(1);                   // unk
        w.WriteH(0); w.WriteH(0);      // M-accuracy, M-crit
        w.WriteH(0);                   // unk
        w.WriteF(1.0f);                // cast speed
        w.WriteH(0);                   // unk 3.5
        w.WriteH(0);                   // concentration
        w.WriteH(0); w.WriteH(0);      // M-boost, M-suppress
        w.WriteH(0);                   // heal boost
        w.WriteH(0); w.WriteH(0);      // P-crit resist, M-crit resist
        w.WriteH(0); w.WriteH(0);      // P-crit fortitude, M-crit fortitude
        w.WriteH(0);                   // unk 3.5
        w.WriteD(27); w.WriteD(0);     // inventory limit, current size
        w.WriteD(0); w.WriteD(0);      // unk
        w.WriteD((int)p.PlayerClass);

        w.WriteH(0); w.WriteH(0);      // unk 3.0
        w.WriteH(0); w.WriteH(0);      // unk 3.5
        w.WriteQ(0); w.WriteQ(0);      // reposte energy current/max
        w.WriteQ(0);                   // salvation percent

        // 4.3 NA
        w.WriteH(0); w.WriteH(0); w.WriteH(1); w.WriteH(0);

        // Base attributes (same as current — no buff bonuses in M5)
        w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100); w.WriteH(100);
        w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); // resistances
        w.WriteD(maxHp); w.WriteD(maxMp);
        w.WriteD(6000); w.WriteD(60);  // base DP, fly time
        w.WriteH(100); w.WriteH(0);    // base main/off-hand P-attack
        w.WriteD(100); w.WriteD(100);  // base M-attack, base P-def
        w.WriteD(100);                 // base M-def
        w.WriteH(0); w.WriteF(5.0f);   // base M-resist, attack range
        w.WriteH(0);                   // unk 3.5
        w.WriteH(100);                 // base evasion
        w.WriteH(0); w.WriteH(0);      // base parry, block
        w.WriteH(0); w.WriteH(0);      // base main/off-hand P-crit
        w.WriteH(0);                   // base M-crit
        w.WriteH(0);                   // unk
        w.WriteH(0); w.WriteH(0);      // base main/off-hand P-accuracy
        w.WriteH(0); w.WriteH(0);      // off-hand M-accuracy, base M-accuracy
        w.WriteH(0);                   // base concentration
        w.WriteH(0); w.WriteH(0);      // base M-boost, suppress
        w.WriteH(0);                   // base heal boost
        w.WriteH(0); w.WriteH(0);      // base P/M-crit resist
    }
}
