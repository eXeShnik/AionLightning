using AionLightning.Commons.Network;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Stats;

namespace AionLightning.Game.Network.Aion.ServerPackets;

public sealed class SM_STATS_INFO : AionServerPacket
{
    private readonly Player _player;
    private readonly PlayerStatsTemplate? _template;
    private readonly PlayerExperienceTable? _expTable;

    public SM_STATS_INFO(Player player, PlayerStatsTemplate? template = null,
        PlayerExperienceTable? expTable = null) : base(0x01)
    {
        _player   = player;
        _template = template;
        _expTable = expTable;
    }

    public override void Write(ref PacketWriter w)
    {
        var p = _player;
        var t = _template;
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int gameTime = (int)(DateTime.UtcNow - epoch).TotalMinutes;

        w.WriteD(p.ObjectId);
        w.WriteD(gameTime);

        // Current attributes (power, health, agility, accuracy, knowledge, will)
        w.WriteH((short)(t?.Power    ?? 100));
        w.WriteH((short)(t?.Health   ?? 100));
        w.WriteH((short)(t?.Agility  ?? 100));
        w.WriteH((short)(t?.Accuracy ?? 100));
        w.WriteH((short)(t?.Knowledge ?? 100));
        w.WriteH((short)(t?.Will     ?? 100));

        // Elemental resistances (water, wind, earth, fire, light, dark)
        w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0);

        w.WriteH(p.Level);
        w.WriteH(0); w.WriteH(0); w.WriteH(0); // unk

        long expNeeded = _expTable?.GetStartExpForLevel(_player.Level + 1) ?? 0;
        w.WriteQ(expNeeded);                // exp needed for next level
        w.WriteQ(p.ExpRecoverable);         // exp recoverable (grey portion of XP bar)
        w.WriteQ(_player.Exp);              // exp current

        w.WriteD(0); // unk

        int maxHp = Math.Max(1, (p.MaxHp > 0 ? p.MaxHp : 1000) + p.MaxHpBonusDelta);
        int curHp = Math.Min(p.CurrentHp > 0 ? p.CurrentHp : maxHp, maxHp);
        int maxMp = Math.Max(1, (p.MaxMp > 0 ? p.MaxMp : 500) + p.MaxMpBonusDelta);
        int curMp = Math.Min(p.CurrentMp > 0 ? p.CurrentMp : maxMp, maxMp);

        w.WriteD(maxHp); w.WriteD(curHp);
        w.WriteD(maxMp); w.WriteD(curMp);
        w.WriteH(6000); w.WriteH((short)p.Dp); // max DP, current DP
        w.WriteD(p.MaxFp); w.WriteD(p.CurrentFp); // max fly time, current fly time
        w.WriteH(0);                   // fly state

        int weaponAtkBonus = (p.MainHandMinDmg + p.MainHandMaxDmg) / 2;
        int totalAtk = Math.Max(1, (t?.MainHandAttack ?? 0) + weaponAtkBonus + p.BonusPhysicalAtk + p.PatkDebuffDelta + p.PatkStatUpDelta);
        w.WriteH((short)totalAtk); w.WriteH(0); // main/off-hand P-attack
        w.WriteH(0);                   // unk 3.0
        int pdef = Math.Max(0, p.PhysicalDefense + p.PdefDebuffDelta + p.PdefStatUpDelta);
        w.WriteD(pdef);                // P-def
        w.WriteH((short)(100 + p.MainHandMagicalAtk + p.BonusMagicAtk + p.MagicAtkDebuffDelta + p.MagicAtkStatUpDelta)); w.WriteH(0); // main/off-hand M-attack
        int mdef = Math.Max(100, p.MagicDefense + p.MagicDefDelta);
        w.WriteD(mdef);                // M-def
        w.WriteH((short)Math.Max(0, p.BonusMagicResist + p.MResistDebuffDelta + p.MResistStatUpDelta)); w.WriteH(0); // M-resist, unk 3.0
        w.WriteF(5.0f);                // attack range
        w.WriteH((short)Math.Max(500, p.CurrentAttackSpeed + p.AtkSpeedDebuffDelta)); // attack speed
        w.WriteH((short)Math.Max(0, (t?.Evasion ?? 100) + p.BonusEvasion + p.EvasionDebuffDelta + p.EvasionStatUpDelta)); // evasion
        w.WriteH((short)(p.BaseParry + p.BonusParry + p.ParryDelta)); w.WriteH((short)(p.BaseBlock + p.BonusBlock + p.BlockDelta)); // parry, block
        w.WriteH((short)((t?.MainHandCritRate ?? 0) + p.BonusPhysicalCritical + p.PhysCritDelta)); w.WriteH(0); // main/off-hand P-crit
        w.WriteH((short)((t?.MainHandAccuracy ?? 0) + p.BonusPhysicalAccuracy + p.PhysAccDelta)); w.WriteH(0); // main/off-hand P-accuracy
        w.WriteH(1);                   // unk
        w.WriteH((short)((t?.MagicAccuracy ?? 0) + p.BonusMagicalAccuracy + p.MagicAccDelta));
        w.WriteH((short)(p.BaseMagicCritRating + p.BonusMagicalCritical + p.MagicCritDelta)); // M-accuracy, M-crit
        w.WriteH(0);                   // unk
        w.WriteF(Math.Max(0f, (1000 - p.WeaponCastTimeBonus - p.CastTimeDelta) / 1000f)); // cast speed (ReverseStat: lower = faster)
        w.WriteH(0);                   // unk 3.5
        w.WriteH((short)(p.BonusConcentration + p.ConcentrationDelta)); // concentration
        w.WriteH((short)(p.BonusMagicBoost + p.MagicBoostDelta)); w.WriteH((short)(p.BonusMagicSuppression + p.MagicSuppressionDelta)); // M-boost, M-suppress
        w.WriteH((short)(p.BonusHealBoost + p.HealBoostDelta)); // heal boost
        w.WriteH((short)(p.BonusPhysicalCriticalResist + p.PhysCritResistDelta)); w.WriteH((short)(p.BonusMagicalCriticalResist + p.MagicCritResistDelta)); // P-crit resist, M-crit resist
        w.WriteH((short)(p.BonusStrikeFortitude + p.StrikeFortitudeDelta)); w.WriteH((short)(p.BonusSpellFortitude + p.SpellFortitudeDelta)); // P-crit fortitude, M-crit fortitude
        w.WriteH(0);                   // unk 3.5
        w.WriteD(p.Inventory.Capacity); w.WriteD(p.Inventory.BagSlotUsed); // inventory limit, current size
        w.WriteD(0); w.WriteD(0);      // unk
        w.WriteD((int)p.PlayerClass);

        w.WriteH(0); w.WriteH(0);      // unk 3.0
        w.WriteH(0); w.WriteH(0);      // unk 3.5
        w.WriteQ(0); w.WriteQ(0);      // reposte energy current/max
        w.WriteQ(0);                   // salvation percent

        // 4.3 NA
        w.WriteH(0); w.WriteH(0); w.WriteH(1); w.WriteH(0);

        // Base attributes (same as current — no buff bonuses)
        w.WriteH((short)(t?.Power    ?? 100));
        w.WriteH((short)(t?.Health   ?? 100));
        w.WriteH((short)(t?.Agility  ?? 100));
        w.WriteH((short)(t?.Accuracy ?? 100));
        w.WriteH((short)(t?.Knowledge ?? 100));
        w.WriteH((short)(t?.Will     ?? 100));
        w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); w.WriteH(0); // resistances
        w.WriteD(maxHp); w.WriteD(maxMp);
        w.WriteD(6000); w.WriteD(p.MaxFp); // base DP cap, base fly time
        w.WriteH((short)totalAtk); w.WriteH(0); // base main/off-hand P-attack
        w.WriteD(100 + p.MainHandMagicalAtk + p.BonusMagicAtk + p.MagicAtkDebuffDelta + p.MagicAtkStatUpDelta); w.WriteD(pdef); // base M-attack, base P-def
        w.WriteD(mdef);                // base M-def
        w.WriteH((short)Math.Max(0, p.BonusMagicResist + p.MResistDebuffDelta + p.MResistStatUpDelta)); w.WriteF(5.0f); // base M-resist, attack range
        w.WriteH(0);                   // unk 3.5
        w.WriteH((short)Math.Max(0, (t?.Evasion ?? 100) + p.BonusEvasion + p.EvasionStatUpDelta)); // base evasion
        w.WriteH((short)(p.BaseParry + p.BonusParry + p.ParryDelta)); w.WriteH((short)(p.BaseBlock + p.BonusBlock + p.BlockDelta)); // base parry, block
        w.WriteH((short)((t?.MainHandCritRate ?? 0) + p.BonusPhysicalCritical + p.PhysCritDelta)); w.WriteH(0); // base main/off-hand P-crit
        w.WriteH(0);                   // base M-crit
        w.WriteH(0);                   // unk
        w.WriteH((short)((t?.MainHandAccuracy ?? 0) + p.BonusPhysicalAccuracy)); w.WriteH(0); // base main/off-hand P-accuracy
        w.WriteH(0); w.WriteH((short)((t?.MagicAccuracy ?? 0) + p.BonusMagicalAccuracy + p.MagicAccDelta)); // off-hand M-accuracy, base M-accuracy
        w.WriteH((short)p.BonusConcentration);           // base concentration
        w.WriteH((short)p.BonusMagicBoost); w.WriteH((short)p.BonusMagicSuppression); // base M-boost, suppress
        w.WriteH((short)p.BonusHealBoost);              // base heal boost
        w.WriteH((short)p.BonusPhysicalCriticalResist); w.WriteH(0); // base P/M-crit resist
        w.WriteH((short)p.BonusStrikeFortitude); w.WriteH((short)p.BonusSpellFortitude); // base P/M-crit fortitude
    }
}
