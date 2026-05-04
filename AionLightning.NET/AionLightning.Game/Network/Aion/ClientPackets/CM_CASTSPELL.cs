using AionLightning.Commons.Network;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Templates.Skill;
using AionLightning.Game.Network.Aion.ServerPackets;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Network.Aion.ClientPackets;

public sealed class CM_CASTSPELL : AionClientPacket
{
    private readonly GsClientConnection _conn;
    private readonly GameWorld _world;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IDataManager _dataManager;
    private readonly ExperienceService _expService;
    private readonly SpawnService _spawnService;
    private readonly LootService _lootService;
    private readonly QuestService _questService;
    private readonly DuelService _duelService;
    private readonly NpcAiService _npcAi;
    private readonly IPlayerDao _playerDao;
    private readonly ILegionDao _legionDao;
    private readonly RateOptions _rates;

    private int _spellId;
    private int _level;
    private int _targetType;
    private int _targetObjectId;
    private float _x, _y, _z;
    private int _hitTime;

    public CM_CASTSPELL(GsClientConnection conn, GameWorld world,
        PlayerConnectionRegistry connRegistry, IDataManager dataManager,
        ExperienceService expService, SpawnService spawnService, LootService lootService,
        QuestService questService, DuelService duelService, NpcAiService npcAi,
        IPlayerDao playerDao, ILegionDao legionDao, RateOptions rates)
    {
        _conn         = conn;
        _world        = world;
        _connRegistry = connRegistry;
        _dataManager  = dataManager;
        _expService   = expService;
        _spawnService = spawnService;
        _lootService  = lootService;
        _questService = questService;
        _duelService  = duelService;
        _npcAi        = npcAi;
        _playerDao    = playerDao;
        _legionDao    = legionDao;
        _rates        = rates;
    }

    public override void Read(ref PacketReader r)
    {
        _spellId    = r.ReadH();
        _level      = r.ReadC();
        _targetType = r.ReadC();

        switch (_targetType)
        {
            case 0:
            case 3:
            case 4:
                _targetObjectId = r.ReadD();
                break;
            case 1:
                _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF();
                break;
            case 2:
                _x = r.ReadF(); _y = r.ReadF(); _z = r.ReadF();
                for (int i = 0; i < 8; i++) r.ReadF();
                break;
        }

        _hitTime = r.ReadH();
    }

    public override async ValueTask RunAsync(CancellationToken ct)
    {
        var player = _conn.ActivePlayer;
        if (player is null || player.IsAlreadyDead) return;

        // Java PlayerRestrictions.canUseSkill: CANT_ATTACK_STATE blocks all offensive skills;
        // SILENCE blocks MAGICAL skills (physical skills still usable while silenced)
        if ((player.ActiveCcFlags & AbnormalCcFlags.CantAttack) != 0) return;

        if (!player.Skills.IsPresent(_spellId)) return;

        var template = _dataManager.Skills.GetTemplate(_spellId);

        if (template?.SkillType == SkillType.MAGICAL
            && (player.ActiveCcFlags & AbnormalCcFlags.Silence) != 0) return;

        // Server-side cooldown enforcement keyed by CooldownId group (mirrors Java isSkillDisabled)
        if (template is not null && template.Cooldown > 0)
        {
            int cdId = template.EffectiveCooldownId;
            if (player.IsSkillOnCooldown(cdId)) return;
            player.SetSkillCooldown(cdId, template.Cooldown);
            await _conn.SendAsync(new SM_SKILL_COOLDOWN(_dataManager.Skills, player.SkillCooldowns), ct);
        }

        // Broadcast cast animation
        var castPacket = _targetType is 1 or 2
            ? new SM_CASTSPELL(player.ObjectId, _spellId, _level, _targetType, _x, _y, _z, _hitTime)
            : new SM_CASTSPELL(player.ObjectId, _spellId, _level, _targetType, _targetObjectId, _hitTime);

        await BroadcastAsync(castPacket, ct);
        bool isHealSkill = template?.SubType is SkillSubType.HEAL;
        bool isDamageSkill = !isHealSkill
            && template?.SkillType is SkillType.MAGICAL or SkillType.PHYSICAL
            && template.SubType is not (SkillSubType.BUFF or SkillSubType.CHANT);
        int castDelay = template?.Duration ?? 0;
        if (castDelay > 0)
        {
            float castMult = Math.Max(0f, (1000 - player.WeaponCastTimeBonus - player.CastTimeDelta) / 1000f);
            castDelay = Math.Max(0, (int)(castDelay * castMult));
        }
        float castRange = template?.CastRange ?? 0f;

        // M245: hostileup — taunt skill forces NPC target to engage caster
        if (template?.Effects?.HasHostileUp == true && _targetType is 0)
        {
            var tauntNpc = _world.GetNpcByObjectId(_targetObjectId);
            if (tauntNpc is not null && !tauntNpc.IsAlreadyDead)
                _npcAi.ForceEngage(tauntNpc, player);
        }

        // Resurrection skill — targets a dead ally player, sets pending revive, sends dialog
        if (template?.Effects?.HasResurrectEffect == true && _targetType is 0 or 3 or 4)
        {
            var rezTarget = _world.GetPlayerByObjectId(_targetObjectId);
            if (rezTarget is not null && rezTarget.IsAlreadyDead && rezTarget.ObjectId != player.ObjectId)
            {
                rezTarget.HasPendingRevive    = true;
                rezTarget.ResurrectionSkillId = template.Effects.ResurrectSkillId;
                var rezConn = _connRegistry.Get(rezTarget.ObjectId);
                if (rezConn is not null)
                    try { await rezConn.SendAsync(new SM_RESURRECT(player.Name, _spellId), ct); } catch { }
                await BroadcastAsync(new SM_SKILL_ACTIVATION(_spellId), ct);
            }
            return;
        }

        if (isHealSkill && _targetType is 0 or 3 or 4)
        {
            // Heal target — self if targetObjectId == 0 or is the caster
            var healTarget = (_targetObjectId == 0 || _targetObjectId == player.ObjectId)
                ? (Creature)player
                : (Creature?)_world.GetPlayerByObjectId(_targetObjectId);
            if (healTarget is not null && !healTarget.IsAlreadyDead)
            {
                int skillLv = _level;
                float healBoostMult = 1.0f + (player.BonusHealBoost + player.HealBoostDelta) / 1000f;
                int healWorldId = player.Position.WorldId;
                var healEffects = template?.Effects?.HealEffects;

                if (healEffects is { Count: > 0 })
                {
                    // Java AbstractHealEffect.calculate: value + delta*level; percent applies to max stat
                    foreach (var he in healEffects)
                    {
                        int valueWithDelta = he.BaseValue + he.Delta * skillLv;
                        int healMaxStat = he.HealType switch
                        {
                            "hp" => healTarget.MaxHp,
                            "mp" => healTarget.MaxMp,
                            "fp" => healTarget is Player fpHtMax ? fpHtMax.MaxFp : 0,
                            "dp" => 6000, // DP cap (no MaxDp field, mirrors CM_ATTACK / CM_CASTSPELL DP gain caps)
                            _    => 0,
                        };
                        int heal = he.IsPercent ? healMaxStat * valueWithDelta / 100 : valueWithDelta;
                        heal = (int)(heal * healBoostMult);

                        if (he.HealType == "hp")
                        {
                            heal = Math.Min(heal, healTarget.MaxHp - healTarget.CurrentHp);
                            if (heal <= 0) continue;
                            healTarget.CurrentHp += heal;
                            var hpStatus = new SM_ATTACK_STATUS(healTarget, SM_ATTACK_STATUS.AttackType.NaturalHp,
                                _spellId, heal, SM_ATTACK_STATUS.LogId.Heal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == healWorldId)
                                    try { await c.SendAsync(hpStatus, ct); } catch { }
                        }
                        else if (he.HealType == "fp" && healTarget is Player fpHt)
                        {
                            // M248: fphealinstant — flight points heal (Player only)
                            heal = Math.Min(heal, fpHt.MaxFp - fpHt.CurrentFp);
                            if (heal <= 0) continue;
                            fpHt.CurrentFp += heal;
                            var fpStatus = new SM_ATTACK_STATUS(fpHt, SM_ATTACK_STATUS.AttackType.NaturalFp,
                                _spellId, heal, SM_ATTACK_STATUS.LogId.FpHeal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == healWorldId)
                                    try { await c.SendAsync(fpStatus, ct); } catch { }
                        }
                        else if (he.HealType == "dp" && healTarget is Player dpHt)
                        {
                            // M252: dphealinstant — divine power heal (Player only, capped at 6000)
                            heal = Math.Min(heal, 6000 - dpHt.Dp);
                            if (heal <= 0) continue;
                            dpHt.Dp += heal;
                            var dpInfo = new SM_DP_INFO(dpHt.ObjectId, dpHt.Dp);
                            var dpConn = _connRegistry.Get(dpHt.ObjectId);
                            if (dpConn is not null)
                                try { await dpConn.SendAsync(dpInfo, ct); } catch { }
                        }
                        else
                        {
                            heal = Math.Min(heal, healTarget.MaxMp - healTarget.CurrentMp);
                            if (heal <= 0) continue;
                            healTarget.CurrentMp += heal;
                            var mpStatus = new SM_ATTACK_STATUS(healTarget, SM_ATTACK_STATUS.AttackType.NaturalMp,
                                _spellId, heal, SM_ATTACK_STATUS.LogId.MpHeal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == healWorldId)
                                    try { await c.SendAsync(mpStatus, ct); } catch { }
                        }
                    }
                }
                else
                {
                    var hotEffects = template?.Effects?.HotEffects;
                    if (hotEffects is { Count: > 0 })
                    {
                        // HoT — add buff icon and spawn periodic heal ticks (mirrors DoT tick loop)
                        int hotDurationMs = hotEffects.Max(h => h.Duration2Ms);
                        var hotEffect = new AbnormalState
                        {
                            SkillId    = _spellId,
                            SkillLevel = skillLv,
                            EffectorId = player.ObjectId,
                            Expiry     = DateTime.UtcNow.AddMilliseconds(hotDurationMs),
                        };
                        healTarget.AddEffect(hotEffect);
                        bool hotTargetIsPlayer = healTarget is Player;
                        var hotAbnormal = new SM_ABNORMAL_EFFECT(healTarget.ObjectId, hotTargetIsPlayer,
                                              healTarget.GetActiveEffects());
                        foreach (var c in _connRegistry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == healWorldId)
                                try { await c.SendAsync(hotAbnormal, ct); } catch { }

                        foreach (var hot in hotEffects)
                        {
                            int healPerTick = Math.Max(1, hot.BaseValue + hot.Delta * skillLv);
                            var tickTarget  = healTarget;
                            var tickEffect  = hotEffect;
                            var tickSpellId = _spellId;
                            var registry    = _connRegistry;
                            _ = Task.Run(async () =>
                            {
                                while (!tickTarget.IsAlreadyDead && DateTime.UtcNow < tickEffect.Expiry)
                                {
                                    await Task.Delay(hot.CheckTimeMs);
                                    if (tickTarget.IsAlreadyDead || DateTime.UtcNow >= tickEffect.Expiry) break;

                                    int actual = hot.HealType switch
                                    {
                                        "hp"                                            => Math.Min(healPerTick, tickTarget.MaxHp - tickTarget.CurrentHp),
                                        "fp" when tickTarget is Player fpHotTickT       => Math.Min(healPerTick, fpHotTickT.MaxFp - fpHotTickT.CurrentFp),
                                        "fp"                                            => 0,
                                        "dp" when tickTarget is Player dpHotTickT       => Math.Min(healPerTick, 6000 - dpHotTickT.Dp),
                                        "dp"                                            => 0,
                                        _                                               => Math.Min(healPerTick, tickTarget.MaxMp - tickTarget.CurrentMp),
                                    };
                                    if (actual > 0)
                                    {
                                        if (hot.HealType == "hp")
                                        {
                                            tickTarget.CurrentHp += actual;
                                            var pkt = new SM_ATTACK_STATUS(tickTarget, SM_ATTACK_STATUS.AttackType.NaturalHp, tickSpellId, actual, SM_ATTACK_STATUS.LogId.Heal);
                                            int w = tickTarget.Position.WorldId;
                                            foreach (var c in registry.GetAll())
                                                if (c.ActivePlayer?.Position.WorldId == w)
                                                    try { await c.SendAsync(pkt); } catch { }
                                        }
                                        else if (hot.HealType == "fp" && tickTarget is Player fpHotTickApp)
                                        {
                                            // M249: FP HoT tick
                                            fpHotTickApp.CurrentFp += actual;
                                            var pkt = new SM_ATTACK_STATUS(fpHotTickApp, SM_ATTACK_STATUS.AttackType.NaturalFp, tickSpellId, actual, SM_ATTACK_STATUS.LogId.FpHeal);
                                            int w = fpHotTickApp.Position.WorldId;
                                            foreach (var c in registry.GetAll())
                                                if (c.ActivePlayer?.Position.WorldId == w)
                                                    try { await c.SendAsync(pkt); } catch { }
                                        }
                                        else if (hot.HealType == "dp" && tickTarget is Player dpHotTickApp)
                                        {
                                            // M252: DP HoT tick — capped at 6000, broadcast SM_DP_INFO to caster's own connection
                                            dpHotTickApp.Dp += actual;
                                            var dpHotConn = registry.Get(dpHotTickApp.ObjectId);
                                            if (dpHotConn is not null)
                                                try { await dpHotConn.SendAsync(new SM_DP_INFO(dpHotTickApp.ObjectId, dpHotTickApp.Dp)); } catch { }
                                        }
                                        else
                                        {
                                            tickTarget.CurrentMp += actual;
                                            var pkt = new SM_ATTACK_STATUS(tickTarget, SM_ATTACK_STATUS.AttackType.NaturalMp, tickSpellId, actual, SM_ATTACK_STATUS.LogId.MpHeal);
                                            int w = tickTarget.Position.WorldId;
                                            foreach (var c in registry.GetAll())
                                                if (c.ActivePlayer?.Position.WorldId == w)
                                                    try { await c.SendAsync(pkt); } catch { }
                                        }
                                    }
                                }
                                tickTarget.RemoveEffect(tickEffect.SkillId, tickEffect.Expiry);
                                var expired = new SM_ABNORMAL_EFFECT(tickTarget.ObjectId, hotTargetIsPlayer,
                                                  tickTarget.GetActiveEffects());
                                int expW = tickTarget.Position.WorldId;
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == expW)
                                        try { await c.SendAsync(expired); } catch { }
                            });
                        }
                    }
                    else
                    {
                        // Fallback for HEAL-subtype skills with no parseable healinstant/heal element
                        int heal = (int)((player.Level * 6 + Random.Shared.Next(15, 40)) * healBoostMult);
                        heal = Math.Min(heal, healTarget.MaxHp - healTarget.CurrentHp);
                        if (heal > 0)
                        {
                            healTarget.CurrentHp += heal;
                            var healStatus = new SM_ATTACK_STATUS(healTarget, SM_ATTACK_STATUS.AttackType.NaturalHp,
                                _spellId, heal, SM_ATTACK_STATUS.LogId.Heal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == healWorldId)
                                    try { await c.SendAsync(healStatus, ct); } catch { }
                        }
                    }
                }

                // Update group HP display for healed player
                if (healTarget is Player healedPlayer)
                {
                    var grp = healedPlayer.Group;
                    if (grp is not null)
                    {
                        var update = new SM_GROUP_MEMBER_INFO(grp.GroupId, healedPlayer, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
                        foreach (var m in grp.Members)
                        {
                            if (m.ObjectId == healedPlayer.ObjectId) continue;
                            var mc = _connRegistry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(update, ct); } catch { }
                        }
                    }
                }
            }

            // AoE heal: when the skill has AREA target type, apply the same instant heal to all
            // nearby allies (Healing Wind, Prayer of Wind, etc.) up to target_maxcount
            if ((template?.IsCasterAoe == true || template?.IsTargetAoe == true)
                && template.EffectiveRange > 0
                && template?.Effects?.HealEffects is { Count: > 0 } aoeHealEffects)
            {
                int aoeSkillLv      = _level;
                float aoeBoostMult  = 1.0f + (player.BonusHealBoost + player.HealBoostDelta) / 1000f;
                int aoeWorldId      = player.Position.WorldId;
                float aoeR          = template.EffectiveRange;
                Position aoeCenter  = healTarget?.Position ?? player.Position;
                int aoeMaxTargets   = template.TargetMaxCount;
                int aoeHealed       = healTarget is not null ? 1 : 0;

                foreach (var ally in _world.GetAll())
                {
                    if (aoeHealed >= aoeMaxTargets) break;
                    if (ally.IsAlreadyDead || ally.ObjectId == healTarget?.ObjectId) continue;
                    if (ally.Race != player.Race) continue;
                    if (ally.Position.WorldId != aoeWorldId) continue;
                    float adx = ally.Position.X - aoeCenter.X, ady = ally.Position.Y - aoeCenter.Y;
                    if (adx * adx + ady * ady > aoeR * aoeR) continue;

                    foreach (var he in aoeHealEffects)
                    {
                        int vd = he.BaseValue + he.Delta * aoeSkillLv;
                        int aoeMaxStat = he.HealType switch
                        {
                            "hp" => ally.MaxHp,
                            "mp" => ally.MaxMp,
                            "fp" => ally is Player fpAllyMax ? fpAllyMax.MaxFp : 0,
                            "dp" => 6000,
                            _    => 0,
                        };
                        int h  = he.IsPercent ? aoeMaxStat * vd / 100 : vd;
                        h = (int)(h * aoeBoostMult);

                        if (he.HealType == "hp")
                        {
                            h = Math.Min(h, ally.MaxHp - ally.CurrentHp);
                            if (h <= 0) continue;
                            ally.CurrentHp += h;
                            var pkt = new SM_ATTACK_STATUS(ally, SM_ATTACK_STATUS.AttackType.NaturalHp, _spellId, h, SM_ATTACK_STATUS.LogId.Heal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == aoeWorldId)
                                    try { await c.SendAsync(pkt, ct); } catch { }
                        }
                        else if (he.HealType == "fp" && ally is Player fpAlly)
                        {
                            // M248: AoE FP heal — Player allies only
                            h = Math.Min(h, fpAlly.MaxFp - fpAlly.CurrentFp);
                            if (h <= 0) continue;
                            fpAlly.CurrentFp += h;
                            var pkt = new SM_ATTACK_STATUS(fpAlly, SM_ATTACK_STATUS.AttackType.NaturalFp, _spellId, h, SM_ATTACK_STATUS.LogId.FpHeal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == aoeWorldId)
                                    try { await c.SendAsync(pkt, ct); } catch { }
                        }
                        else if (he.HealType == "dp" && ally is Player dpAlly)
                        {
                            // M252: AoE DP heal — Player allies only
                            h = Math.Min(h, 6000 - dpAlly.Dp);
                            if (h <= 0) continue;
                            dpAlly.Dp += h;
                            var dpAllyConn = _connRegistry.Get(dpAlly.ObjectId);
                            if (dpAllyConn is not null)
                                try { await dpAllyConn.SendAsync(new SM_DP_INFO(dpAlly.ObjectId, dpAlly.Dp), ct); } catch { }
                        }
                        else
                        {
                            h = Math.Min(h, ally.MaxMp - ally.CurrentMp);
                            if (h <= 0) continue;
                            ally.CurrentMp += h;
                            var pkt = new SM_ATTACK_STATUS(ally, SM_ATTACK_STATUS.AttackType.NaturalMp, _spellId, h, SM_ATTACK_STATUS.LogId.MpHeal);
                            foreach (var c in _connRegistry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == aoeWorldId)
                                    try { await c.SendAsync(pkt, ct); } catch { }
                        }
                    }
                    aoeHealed++;
                }
            }

            var activation = new SM_SKILL_ACTIVATION(_spellId);
            await BroadcastAsync(activation, ct);
        }
        else if (template?.SubType is SkillSubType.BUFF or SkillSubType.CHANT
                 && _targetType is 0 or 3 or 4 && template.Duration > 0)
        {
            // Determine buff target: self when targetObjectId is 0 or caster's own id
            Creature? buffTarget = (_targetObjectId == 0 || _targetObjectId == player.ObjectId)
                ? player
                : _world.GetPlayerByObjectId(_targetObjectId);

            if (buffTarget is not null && !buffTarget.IsAlreadyDead)
            {
                // Dispel debuff: remove all active debuff states from the target and re-broadcast
                // Java DispelDebuffEffect.applyEffect — removes effects by DispelCategory; simplified to IsDebuff flag
                if (template.Effects?.HasDispelDebuff == true)
                {
                    buffTarget.ClearDebuffs();
                    bool isPlayerTarget = buffTarget is Player;
                    int dispelWorldId = player.Position.WorldId;
                    var cleansed = new SM_ABNORMAL_EFFECT(buffTarget.ObjectId, isPlayerTarget,
                                       buffTarget.GetActiveEffects());
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == dispelWorldId)
                            try { await c.SendAsync(cleansed, ct); } catch { }
                    await BroadcastAsync(new SM_SKILL_ACTIVATION(_spellId), ct);
                    return; // dispel completes here, no buff state added
                }

                int durationMs       = template.Duration;
                int maxHpStatUpDelta = template.Effects?.MaxHpStatUpDelta ?? 0;
                int maxMpStatUpDelta    = template.Effects?.MaxMpStatUpDelta       ?? 0;
                int mBoostStatUpDelta   = template.Effects?.MagicBoostStatUpDelta  ?? 0;
                int healBoostStatUpDelta = template.Effects?.HealBoostStatUpDelta ?? 0;
                int physAccStatUpDelta   = template.Effects?.PhysAccStatUpDelta   ?? 0;
                int magicAccStatUpDelta  = template.Effects?.MagicAccStatUpDelta  ?? 0;
                int parryStatUpDelta     = template.Effects?.ParryStatUpDelta     ?? 0;
                int blockStatUpDelta     = template.Effects?.BlockStatUpDelta     ?? 0;
                int physCritStatUpDelta         = template.Effects?.PhysCritStatUpDelta         ?? 0;
                int magicCritStatUpDelta        = template.Effects?.MagicCritStatUpDelta        ?? 0;
                int physCritResistStatUpDelta    = template.Effects?.PhysCritResistStatUpDelta    ?? 0;
                int magicCritResistStatUpDelta   = template.Effects?.MagicCritResistStatUpDelta   ?? 0;
                int strikeFortitudeStatUpDelta   = template.Effects?.StrikeFortitudeStatUpDelta   ?? 0;
                int spellFortitudeStatUpDelta    = template.Effects?.SpellFortitudeStatUpDelta    ?? 0;
                int castTimeStatUpDelta          = template.Effects?.CastTimeStatUpDelta          ?? 0;
                int concentrationStatUpDelta     = template.Effects?.ConcentrationStatUpDelta     ?? 0;
                int magicSuppressionStatUpDelta  = template.Effects?.MagicSuppressionStatUpDelta  ?? 0;
                int pdefStatUpDelta              = template.Effects?.PdefStatUpDelta              ?? 0;
                int magicDefStatUpDelta          = template.Effects?.MagicDefStatUpDelta          ?? 0;
                int patkStatUpDelta              = template.Effects?.PhysAtkStatUpDelta           ?? 0;
                int magicAtkStatUpDelta          = template.Effects?.MagicAtkStatUpDelta          ?? 0;
                int evasionStatUpDelta           = template.Effects?.EvasionStatUpDelta           ?? 0;
                int mresistStatUpDelta           = template.Effects?.MResistStatUpDelta           ?? 0;
                int atkSpeedStatUpDelta          = template.Effects?.AtkSpeedStatUpDelta          ?? 0;
                int speedStatUpPct               = template.Effects?.SpeedStatUpPct               ?? 0;
                var effect = new AbnormalState
                {
                    SkillId            = _spellId,
                    SkillLevel         = _level,
                    EffectorId         = player.ObjectId,
                    Expiry             = DateTime.UtcNow.AddMilliseconds(durationMs),
                    SpeedStatUpPct     = speedStatUpPct,
                    PreBuffMovSpeed    = buffTarget.MovementSpeed,
                    MaxHpDelta         = maxHpStatUpDelta,
                    MaxMpDelta         = maxMpStatUpDelta,
                    MagicBoostDeltaVal = mBoostStatUpDelta,
                    HealBoostDeltaVal  = healBoostStatUpDelta,
                    PhysAccDeltaVal    = physAccStatUpDelta,
                    MagicAccDeltaVal   = magicAccStatUpDelta,
                    ParryDeltaVal      = parryStatUpDelta,
                    BlockDeltaVal      = blockStatUpDelta,
                    PhysCritDeltaVal        = physCritStatUpDelta,
                    MagicCritDeltaVal       = magicCritStatUpDelta,
                    PhysCritResistDeltaVal   = physCritResistStatUpDelta,
                    MagicCritResistDeltaVal  = magicCritResistStatUpDelta,
                    StrikeFortitudeDeltaVal  = strikeFortitudeStatUpDelta,
                    SpellFortitudeDeltaVal   = spellFortitudeStatUpDelta,
                    CastTimeDeltaVal         = castTimeStatUpDelta,
                    ConcentrationDeltaVal    = concentrationStatUpDelta,
                    MagicSuppressionDeltaVal = magicSuppressionStatUpDelta,
                    PdefStatUpDeltaVal       = pdefStatUpDelta,
                    MagicDefDeltaVal         = magicDefStatUpDelta,
                    PatkStatUpDeltaVal       = patkStatUpDelta,
                    MagicAtkStatUpDeltaVal   = magicAtkStatUpDelta,
                    EvasionStatUpDeltaVal    = evasionStatUpDelta,
                    MResistStatUpDeltaVal    = mresistStatUpDelta,
                    AtkSpeedStatUpDeltaVal   = atkSpeedStatUpDelta,
                };
                buffTarget.AddEffect(effect);

                if (atkSpeedStatUpDelta != 0)
                {
                    var atkSpdEmo = new SM_EMOTION(buffTarget, EmotionType.START_EMOTE2);
                    int atkSpdWorld = player.Position.WorldId;
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == atkSpdWorld)
                            try { await c.SendAsync(atkSpdEmo, ct); } catch { }
                }
                if (speedStatUpPct != 0)
                {
                    buffTarget.MovementSpeed = Math.Min(12.0f, buffTarget.MovementSpeed * (100 + speedStatUpPct) / 100f);
                    var speedEmo = new SM_EMOTION(buffTarget, EmotionType.START_EMOTE2);
                    int speedWorld = player.Position.WorldId;
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == speedWorld)
                            try { await c.SendAsync(speedEmo, ct); } catch { }
                }
                if ((maxHpStatUpDelta != 0 || maxMpStatUpDelta != 0 || mBoostStatUpDelta != 0 || healBoostStatUpDelta != 0 || physAccStatUpDelta != 0 || magicAccStatUpDelta != 0 || parryStatUpDelta != 0 || blockStatUpDelta != 0 || physCritStatUpDelta != 0 || magicCritStatUpDelta != 0 || physCritResistStatUpDelta != 0 || magicCritResistStatUpDelta != 0 || strikeFortitudeStatUpDelta != 0 || spellFortitudeStatUpDelta != 0 || castTimeStatUpDelta != 0 || concentrationStatUpDelta != 0 || magicSuppressionStatUpDelta != 0 || pdefStatUpDelta != 0 || magicDefStatUpDelta != 0 || patkStatUpDelta != 0 || magicAtkStatUpDelta != 0 || evasionStatUpDelta != 0 || mresistStatUpDelta != 0 || atkSpeedStatUpDelta != 0) && buffTarget is Player statUpPlayer)
                {
                    var statsInfoBuff = new SM_STATS_INFO(statUpPlayer, _dataManager.PlayerStats.GetTemplate(statUpPlayer.PlayerClass, statUpPlayer.Level));
                    var statUpConn = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == statUpPlayer);
                    if (statUpConn is not null) try { await statUpConn.SendAsync(statsInfoBuff, ct); } catch { }
                }

                bool buffTargetIsPlayer = buffTarget is Player;
                int  buffWorldId        = player.Position.WorldId;
                var  abnormal = new SM_ABNORMAL_EFFECT(buffTarget.ObjectId, buffTargetIsPlayer,
                                    buffTarget.GetActiveEffects());
                foreach (var c in _connRegistry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == buffWorldId)
                        try { await c.SendAsync(abnormal, ct); } catch { }

                // Schedule expiry — remove effect and re-broadcast to target's current zone
                var expiryEffect = effect;
                var expiryTarget = buffTarget;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(durationMs);
                    bool buffStatChanged = expiryEffect.MaxHpDelta != 0 || expiryEffect.MaxMpDelta != 0 ||
                        expiryEffect.MagicBoostDeltaVal != 0 || expiryEffect.HealBoostDeltaVal != 0 ||
                        expiryEffect.PhysAccDeltaVal != 0 || expiryEffect.MagicAccDeltaVal != 0 ||
                        expiryEffect.ParryDeltaVal != 0 || expiryEffect.BlockDeltaVal != 0 ||
                        expiryEffect.PhysCritDeltaVal != 0 || expiryEffect.MagicCritDeltaVal != 0 ||
                        expiryEffect.PhysCritResistDeltaVal != 0 || expiryEffect.MagicCritResistDeltaVal != 0 ||
                        expiryEffect.StrikeFortitudeDeltaVal != 0 || expiryEffect.SpellFortitudeDeltaVal != 0 ||
                        expiryEffect.CastTimeDeltaVal != 0 || expiryEffect.ConcentrationDeltaVal != 0 ||
                        expiryEffect.MagicSuppressionDeltaVal != 0 || expiryEffect.PdefStatUpDeltaVal != 0 ||
                        expiryEffect.MagicDefDeltaVal != 0 || expiryEffect.PatkStatUpDeltaVal != 0 ||
                        expiryEffect.MagicAtkStatUpDeltaVal != 0 || expiryEffect.EvasionStatUpDeltaVal != 0 ||
                        expiryEffect.MResistStatUpDeltaVal != 0 || expiryEffect.AtkSpeedStatUpDeltaVal != 0;
                    bool atkSpeedChanged = expiryEffect.AtkSpeedStatUpDeltaVal != 0;
                    bool movSpeedChanged = expiryEffect.SpeedStatUpPct != 0;

                    expiryTarget.RemoveEffectBySkillId(expiryEffect.SkillId);

                    // Clamp HP/MP when a MaxHp/MaxMp buff expires (cap went down)
                    if (expiryEffect.MaxHpDelta != 0)
                    {
                        int newMaxHp = Math.Max(1, expiryTarget.MaxHp + expiryTarget.MaxHpBonusDelta);
                        if (expiryTarget.CurrentHp > newMaxHp) expiryTarget.CurrentHp = newMaxHp;
                    }
                    if (expiryEffect.MaxMpDelta != 0)
                    {
                        int newMaxMp = Math.Max(1, expiryTarget.MaxMp + expiryTarget.MaxMpBonusDelta);
                        if (expiryTarget.CurrentMp > newMaxMp) expiryTarget.CurrentMp = newMaxMp;
                    }

                    if (atkSpeedChanged)
                    {
                        var restoreAtkEmo = new SM_EMOTION(expiryTarget, EmotionType.START_EMOTE2);
                        int restoreAtkWorld = expiryTarget.Position.WorldId;
                        foreach (var c in _connRegistry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == restoreAtkWorld)
                                try { await c.SendAsync(restoreAtkEmo); } catch { }
                    }
                    if (movSpeedChanged)
                    {
                        var restoreSpeedEmo = new SM_EMOTION(expiryTarget, EmotionType.START_EMOTE2);
                        int restoreSpeedWorld = expiryTarget.Position.WorldId;
                        foreach (var c in _connRegistry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == restoreSpeedWorld)
                                try { await c.SendAsync(restoreSpeedEmo); } catch { }
                    }
                    if (buffStatChanged && expiryTarget is Player expiredBuffPlayer)
                    {
                        var statsInfoExp = new SM_STATS_INFO(expiredBuffPlayer, _dataManager.PlayerStats.GetTemplate(expiredBuffPlayer.PlayerClass, expiredBuffPlayer.Level));
                        var expBuffConn = _connRegistry.GetAll().FirstOrDefault(c => c.ActivePlayer == expiredBuffPlayer);
                        if (expBuffConn is not null) try { await expBuffConn.SendAsync(statsInfoExp); } catch { }
                    }
                    var expired = new SM_ABNORMAL_EFFECT(expiryTarget.ObjectId, buffTargetIsPlayer,
                                      expiryTarget.GetActiveEffects());
                    int expWorldId = expiryTarget.Position.WorldId;
                    foreach (var c in _connRegistry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == expWorldId)
                            try { await c.SendAsync(expired); } catch { }
                });
            }

            await BroadcastAsync(new SM_SKILL_ACTIVATION(_spellId), ct);
        }
        else if (isDamageSkill && _targetType is 1 or 2 && template?.IsGroundAoe == true)
        {
            // Ground-targeted AoE (first_target=POINT, target_type=AREA): damage all enemies near the cast point
            var spellId  = _spellId;
            var world    = _world;
            var registry = _connRegistry;
            var conn     = _conn;
            float px = _x, py = _y, pz = _z;
            float aoeR    = Math.Max(1f, template.EffectiveRange);
            float aoeAlt  = Math.Max(1f, template.EffectiveAltitude);
            int   maxHits = template.TargetMaxCount;
            bool  hitsEnemies = !string.Equals(template.TargetRelation, "FRIEND", StringComparison.OrdinalIgnoreCase);

            _ = Task.Run(async () =>
            {
                if (castDelay > 0)
                    await Task.Delay(castDelay);

                if (player.IsAlreadyDead) return;

                int castWorldId = player.Position.WorldId;
                var activation  = new SM_SKILL_ACTIVATION(spellId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(activation); } catch { }

                // Collect targets within the AoE cylinder (horizontal dist + altitude filter)
                var targets = new List<Creature>();
                if (hitsEnemies)
                {
                    foreach (var npc in world.GetAllNpcs())
                    {
                        if (npc.IsAlreadyDead) continue;
                        if (npc.Position.WorldId != castWorldId) continue;
                        float dx = npc.Position.X - px, dy = npc.Position.Y - py, dz = npc.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(npc);
                        if (targets.Count >= maxHits) break;
                    }
                    // Also hit players from opposing faction (PvP zones) within AoE
                    foreach (var other in world.GetAll())
                    {
                        if (other.IsAlreadyDead || other.ObjectId == player.ObjectId) continue;
                        if (other.Race == player.Race) continue;
                        if (other.Position.WorldId != castWorldId) continue;
                        float dx = other.Position.X - px, dy = other.Position.Y - py, dz = other.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(other);
                        if (targets.Count >= maxHits) break;
                    }
                }
                else
                {
                    // FRIEND relation: heal/buff allies in area (resolve as heal at group-heal formula)
                    foreach (var ally in world.GetAll())
                    {
                        if (ally.IsAlreadyDead) continue;
                        if (ally.Race != player.Race && ally.ObjectId != player.ObjectId) continue;
                        if (ally.Position.WorldId != castWorldId) continue;
                        float dx = ally.Position.X - px, dy = ally.Position.Y - py, dz = ally.Position.Z - pz;
                        if (dx * dx + dy * dy > aoeR * aoeR) continue;
                        if (Math.Abs(dz) > aoeAlt) continue;
                        targets.Add(ally);
                        if (targets.Count >= maxHits) break;
                    }
                }

                bool spellIsMagical = template?.SkillType == SkillType.MAGICAL;
                int mAtk = 100 + player.MainHandMagicalAtk + player.BonusMagicAtk + player.MagicAtkDebuffDelta + player.MagicAtkStatUpDelta;
                int pAtk = player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk + player.PatkStatUpDelta;
                var gAoeDmgFx = template?.Effects?.DamageEffects;
                int? gAoeSkillBase = gAoeDmgFx is { Count: > 0 }
                    ? gAoeDmgFx[0].BaseValue + gAoeDmgFx[0].Delta * (_level - 1)
                    : null;

                foreach (var target in targets)
                {
                    // Java: magicBoost -= getMBResist() (target suppression reduces caster boost, min 0)
                    int tMBSuppress = spellIsMagical
                        ? (target is Player pvpSupp ? pvpSupp.BonusMagicSuppression + pvpSupp.MagicSuppressionDelta
                         : target is Npc npcSupp   ? (npcSupp.Template.Stats?.MBResist ?? 0)
                         : 0)
                        : 0;
                    float magicBoostMult = 1.0f + Math.Max(0, player.BonusMagicBoost + player.MagicBoostDelta - tMBSuppress) / 1000f;

                    int gAoeBase = gAoeSkillBase ?? (spellIsMagical ? player.Level * 6 : player.Level * 4) + Random.Shared.Next(10, 40);
                    int rawSpellDmg = spellIsMagical
                        ? (int)((mAtk + gAoeBase) * magicBoostMult)
                        : pAtk + gAoeBase;

                    // M258: skip resist/dodge when damage effect carries noresist="true"
                    bool gAoeNoResist = gAoeDmgFx is { Count: > 0 } && gAoeDmgFx[0].IsNoResist;
                    // Magic resist check (Java calculateMagicalResistRate) / physical dodge check (calculatePhysicalDodgeRate)
                    if (spellIsMagical && !gAoeNoResist)
                    {
                        int totalMagicAcc = player.BaseMagicAccuracy + player.BonusMagicalAccuracy + player.MagicAccDelta + player.ConcentrationDelta;
                        int targetMR = (target is Player pvpResist ? pvpResist.BonusMagicResist
                                     : target is Npc npcResist   ? NpcMagicResist(npcResist)
                                     : 0) + target.MResistDebuffDelta + target.MResistStatUpDelta;
                        int resistRate = Math.Max(1, targetMR - totalMagicAcc);
                        int tLvlAoE = target is Player pvpLvl ? pvpLvl.Level : target is Npc npcLvl ? npcLvl.Level : 0;
                        int lvlDiffAoE = tLvlAoE - player.Level - 2;
                        if (lvlDiffAoE > 0) resistRate += lvlDiffAoE * 100;
                        if (Random.Shared.Next(1000) < resistRate)
                        {
                            var resistPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(resistPkt); } catch { }
                            continue;
                        }
                    }
                    else if (!gAoeNoResist) // physical skill — dodge check (Java calculatePhysicalDodgeRate)
                    {
                        int physAccAoE = player.BasePhysicalAccuracy + player.BonusPhysicalAccuracy + player.PhysAccDelta;
                        int evAoE = (target is Player pvpEvAoE ? pvpEvAoE.BaseEvasion + pvpEvAoE.BonusEvasion
                                   : target is Npc npcEvAoE   ? NpcPhysicalAccuracy(npcEvAoE) + (npcEvAoE.Template.Stats?.Evasion ?? 0)
                                   : 0) + target.EvasionDebuffDelta + target.EvasionStatUpDelta;
                        float rawDodgeAoE = evAoE - physAccAoE;
                        if (target is Npc npcDodgeAoE)
                            rawDodgeAoE *= 1f + NpcLevelDiffMod(npcDodgeAoE.Level - player.Level);
                        float dodgeRateAoE = Math.Clamp(rawDodgeAoE * 0.6f + 50f, 0f, 300f);
                        if (Random.Shared.Next(1000) < (int)dodgeRateAoE)
                        {
                            var dodgePkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(dodgePkt); } catch { }
                            continue;
                        }
                    }

                    // Magical crit check (same piecewise formula as physical crit)
                    if (spellIsMagical)
                    {
                        int mCritRating = player.BaseMagicCritRating + player.BonusMagicalCritical + player.MagicCritDelta;
                        int mCritResist = target is Player pvpMCrit ? pvpMCrit.BonusMagicalCriticalResist + pvpMCrit.MagicCritResistDelta : 0;
                        mCritRating = Math.Max(0, mCritRating - mCritResist);
                        double mCritRate = mCritRating <= 440 ? mCritRating * 0.1
                                         : mCritRating <= 600 ? 44.0 + (mCritRating - 440) * 0.05
                                         : 52.0 + (mCritRating - 600) * 0.02;
                        if (Random.Shared.Next(100) < (int)mCritRate)
                        {
                            int spF = target is Player pvpSpF ? pvpSpF.BonusSpellFortitude + pvpSpF.SpellFortitudeDelta : 0;
                            float mCritCoeff = Math.Max(1.0f, 1.5f - (float)Math.Round(spF / 1000.0));
                            rawSpellDmg = (int)(rawSpellDmg * mCritCoeff);
                        }
                    }
                    else // physical skill crit (Java calculatePhysicalCriticalRate, coefficient 1.5 for skills)
                    {
                        int pCritRating = player.BaseCritRating + player.BonusPhysicalCritical + player.PhysCritDelta;
                        int pCritResist = target is Player pvpPCG ? pvpPCG.BonusPhysicalCriticalResist + pvpPCG.PhysCritResistDelta : 0;
                        pCritRating = Math.Max(0, pCritRating - pCritResist);
                        double pCritRate = pCritRating <= 440 ? pCritRating * 0.1
                                         : pCritRating <= 600 ? 44.0 + (pCritRating - 440) * 0.05
                                         : 52.0 + (pCritRating - 600) * 0.02;
                        if (Random.Shared.Next(100) < (int)pCritRate)
                        {
                            int sFortG = target is Player pvpSFG ? pvpSFG.BonusStrikeFortitude + pvpSFG.StrikeFortitudeDelta : 0;
                            float pCritCoeffG = Math.Max(1.0f, 1.5f - (float)Math.Round(sFortG / 1000.0));
                            rawSpellDmg = (int)(rawSpellDmg * pCritCoeffG);
                        }
                    }

                    // NPC level-diff or PvP damage reduction (Java StatFunctions.adjustDamages)
                    if (target is Npc npcLvlAoEDmg)
                    {
                        float lvlMod = NpcLevelDiffMod(npcLvlAoEDmg.Level - player.Level);
                        if (lvlMod > 0f) rawSpellDmg = Math.Max(1, (int)(rawSpellDmg * (1f - lvlMod)));
                    }
                    else if (target is Player)
                        rawSpellDmg = Math.Max(1, rawSpellDmg / 2); // PvP 50%

                    // MResist = resist-chance only; NPC MAGICAL_DEFEND base = 0; use MBResist (magic fortitude) for mitigation
                    int spellDef = target is Player pvpSpellTarget
                                 ? (spellIsMagical ? pvpSpellTarget.MagicDefense + pvpSpellTarget.MagicDefDelta
                                                   : pvpSpellTarget.PhysicalDefense + pvpSpellTarget.PdefDebuffDelta + pvpSpellTarget.PdefStatUpDelta)
                                 : target is Npc npcSpellTarget
                                 ? (spellIsMagical ? (npcSpellTarget.Template.Stats?.MBResist ?? 0)
                                                   : (npcSpellTarget.Template.Stats?.PDef    ?? 0))
                                 : 0;
                    int damage = spellDef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + spellDef)) : rawSpellDmg;
                    // M247: noreducespellatk — defense-bypass damage replaces regular formula (per AoE target)
                    var gAoeNoReduce = template?.Effects?.NoReduceEffects;
                    if (gAoeNoReduce is { Count: > 0 })
                    {
                        int noReduceVal = gAoeNoReduce[0].BaseValue + gAoeNoReduce[0].Delta * (_level - 1);
                        damage = gAoeNoReduce[0].IsPercent ? Math.Max(1, target.MaxHp * noReduceVal / 100) : Math.Max(1, noReduceVal);
                    }
                    target.CurrentHp      = Math.Max(0, target.CurrentHp - damage);
                    target.LastCombatTime = DateTime.UtcNow;
                    player.LastCombatTime = DateTime.UtcNow;

                    // M239: drain damage variants — caster restores HP/MP from each AoE-target damage
                    if (gAoeDmgFx is { Count: > 0 } && (gAoeDmgFx[0].HpPercent != 0 || gAoeDmgFx[0].MpPercent != 0))
                    {
                        // M254: physical drain → SkillAtkDrainInstant (23), magical → SpellAtkDrainInstant (24)
                        var gAoeDrainLog = gAoeDmgFx[0].DamageType == "physical"
                            ? SM_ATTACK_STATUS.LogId.SkillAtkDrainInstant
                            : SM_ATTACK_STATUS.LogId.SpellAtkDrainInstant;
                        if (gAoeDmgFx[0].HpPercent != 0)
                        {
                            int hpGain = Math.Min(damage * gAoeDmgFx[0].HpPercent / 100, player.MaxHp - player.CurrentHp);
                            if (hpGain > 0)
                            {
                                player.CurrentHp += hpGain;
                                var drainHpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, spellId, hpGain, gAoeDrainLog);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(drainHpPkt); } catch { }
                            }
                        }
                        if (gAoeDmgFx[0].MpPercent != 0)
                        {
                            int mpGain = Math.Min(damage * gAoeDmgFx[0].MpPercent / 100, player.MaxMp - player.CurrentMp);
                            if (mpGain > 0)
                            {
                                player.CurrentMp += mpGain;
                                var drainMpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, spellId, mpGain, gAoeDrainLog);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(drainMpPkt); } catch { }
                            }
                        }
                    }

                    // M240: mpattackinstant — burn target MP (per AoE target)
                    var gAoeMpFx = template?.Effects?.MpAttackEffects;
                    if (gAoeMpFx is { Count: > 0 })
                    {
                        int mpBurnVal = gAoeMpFx[0].BaseValue + gAoeMpFx[0].Delta * (_level - 1);
                        int mpBurn = gAoeMpFx[0].IsPercent ? target.MaxMp * mpBurnVal / 100 : mpBurnVal;
                        if (mpBurn > 0)
                            target.CurrentMp = Math.Max(0, target.CurrentMp - mpBurn);
                    }

                    if (target is Npc hitNpc && target.CurrentHp > 0)
                        _npcAi.ForceEngage(hitNpc, player);

                    // M255: procatk_instant uses LogId.ProcAtkInstant (92) per Java; other variants use SpellAtk
                    var gAoeStatusLog = (gAoeDmgFx is { Count: > 0 } && gAoeDmgFx[0].Variant == "procatk_instant")
                        ? SM_ATTACK_STATUS.LogId.ProcAtkInstant
                        : SM_ATTACK_STATUS.LogId.SpellAtk;
                    var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, damage, gAoeStatusLog);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(statusPkt); } catch { }

                    if (template?.Effects?.HasDispelBuff == true && target.CurrentHp > 0)
                    {
                        target.ClearBuffs();
                        bool dtip = target is Player;
                        var dp = new SM_ABNORMAL_EFFECT(target.ObjectId, dtip, target.GetActiveEffects());
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(dp); } catch { }
                    }

                    // Apply DoT (bleed/poison/disease) to each AoE target
                    if (target.CurrentHp > 0 && template?.Effects?.DotEffects is { Count: > 0 } aoeDotsG)
                    {
                        foreach (var dot in aoeDotsG)
                        {
                            int dotLvG     = _level;
                            int dotTickDmg = Math.Max(1, dot.BaseValue + dot.Delta * dotLvG);
                            var dotExpiry  = DateTime.UtcNow.AddMilliseconds(dot.Duration2Ms);
                            var dotEffect  = new AbnormalState
                            {
                                SkillId    = spellId,
                                SkillLevel = dotLvG,
                                EffectorId = player.ObjectId,
                                Expiry     = dotExpiry,
                                DotInfo    = dot,
                                IsDebuff   = true,
                            };
                            target.AddEffect(dotEffect);
                            bool dotTargetIsPlayer = target is Player;
                            var dotAbnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, dotTargetIsPlayer, target.GetActiveEffects());
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(dotAbnormal); } catch { }

                            var dotTickTarget = target;
                            var dotTickEffect = dotEffect;
                            var dotTickCaster = player;
                            var dotTickInfo   = dot;
                            var dotTickLogId  = dot.DotType switch
                            {
                                "bleed"         => SM_ATTACK_STATUS.LogId.Bleed,
                                "spellatk"      => SM_ATTACK_STATUS.LogId.SpellAtk,
                                "spellatkdrain" => SM_ATTACK_STATUS.LogId.SpellAtkDrain,
                                _               => SM_ATTACK_STATUS.LogId.Poison,
                            };
                            _ = Task.Run(async () =>
                            {
                                while (!dotTickTarget.IsAlreadyDead && DateTime.UtcNow < dotTickEffect.Expiry)
                                {
                                    await Task.Delay(dotTickInfo.CheckTimeMs);
                                    if (dotTickTarget.IsAlreadyDead || DateTime.UtcNow >= dotTickEffect.Expiry) break;
                                    dotTickTarget.CurrentHp = Math.Max(0, dotTickTarget.CurrentHp - dotTickDmg);
                                    if (dotTickInfo.HpPercent != 0)
                                        dotTickCaster.CurrentHp = Math.Min(dotTickCaster.MaxHp, dotTickCaster.CurrentHp + dotTickDmg * dotTickInfo.HpPercent / 100);
                                    if (dotTickInfo.MpPercent != 0)
                                        dotTickCaster.CurrentMp = Math.Min(dotTickCaster.MaxMp, dotTickCaster.CurrentMp + dotTickDmg * dotTickInfo.MpPercent / 100);
                                    var tickPkt = new SM_ATTACK_STATUS(dotTickTarget, SM_ATTACK_STATUS.AttackType.Damage, spellId, dotTickDmg, dotTickLogId);
                                    int tw = dotTickTarget.Position.WorldId;
                                    foreach (var c in registry.GetAll())
                                        if (c.ActivePlayer?.Position.WorldId == tw)
                                            try { await c.SendAsync(tickPkt); } catch { }
                                }
                                dotTickTarget.RemoveEffect(dotTickEffect.SkillId, dotTickEffect.Expiry);
                                var expiredDot = new SM_ABNORMAL_EFFECT(dotTickTarget.ObjectId, dotTargetIsPlayer, dotTickTarget.GetActiveEffects());
                                int dw = dotTickTarget.Position.WorldId;
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == dw)
                                        try { await c.SendAsync(expiredDot); } catch { }
                            });
                        }
                    }
                }

                // DP gain for successful AoE cast
                if (targets.Count > 0 && player.Dp < 6000)
                {
                    player.Dp = Math.Min(6000, player.Dp + 150);
                    try { await conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp)); } catch { }
                }

                // Handle killed NPCs: broadcast death, generate drops, award XP/AP
                foreach (var killed in targets.OfType<Npc>().Where(t => t.CurrentHp <= 0))
                {
                    killed.State |= CreatureState.Dead;
                    int npcWorldId = killed.Position.WorldId;
                    var die = new SM_EMOTION(killed, EmotionType.DIE);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(die); } catch { }

                    var diedShout = _dataManager.NpcShouts.GetRandomShout(
                        killed.Template.NpcId, NpcShoutData.ShoutEventType.DIED, npcWorldId);
                    if (diedShout.HasValue)
                    {
                        var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(killed.ObjectId, diedShout.Value.StringId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                                try { await c.SendAsync(shoutPkt); } catch { }
                    }

                    world.Remove(killed);
                    _lootService.GenerateDrops(killed, player);
                    await _questService.HandleNpcKillAsync(player, killed, conn, CancellationToken.None);

                    long xpBase = killed.Template.Stats?.MaxXp > 0 ? killed.Template.Stats.MaxXp : killed.Level * 50L;
                    await _expService.AddGroupExpAsync(player, xpBase, killed.Level, CancellationToken.None);

                    if (killed.Position.WorldId == AbyssRankService.AbyssWorldId
                        || killed.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
                    {
                        int ap = AbyssRankService.CalculateNpcApReward(killed.Level);
                        bool aoeNpcRankUp = AbyssRankService.AddAp(player, ap);
                        if (player.AbyssRank > player.AbyssMaxRank) player.AbyssMaxRank = player.AbyssRank;
                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (aoeNpcRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        await _playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await AwardLegionContributionAsync(player, ap, registry, _legionDao, CancellationToken.None);
                    }

                    var killedNpc    = killed;
                    var killedDrops  = _lootService.GetLoot(killedNpc.ObjectId);
                    int killedDecayMs = killedDrops is null ? 5_000 : killedDrops.Count == 0 ? 90_000 : 300_000;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(killedDecayMs);
                        var del = new SM_DELETE(killedNpc.ObjectId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == killedNpc.Position.WorldId)
                                try { await c.SendAsync(del); } catch { }
                        _lootService.ClearLoot(killedNpc.ObjectId);
                        _spawnService.ScheduleRespawn(killedNpc);
                    });
                }
            });
        }
        else if (isDamageSkill && _targetType is 0 or 3 or 4 && _targetObjectId != 0)
        {
            var spellId    = _spellId;
            var world      = _world;
            var registry   = _connRegistry;
            var expSvc     = _expService;
            var spawnSvc   = _spawnService;
            var lootSvc    = _lootService;
            var questSvc   = _questService;
            var duelSvc    = _duelService;
            var conn       = _conn;
            var npcAi      = _npcAi;
            var playerDao  = _playerDao;
            var legionDao  = _legionDao;
            var rates      = _rates;

            _ = Task.Run(async () =>
            {
                if (castDelay > 0)
                    await Task.Delay(castDelay);

                int castWorldId = player.Position.WorldId;
                var activation = new SM_SKILL_ACTIVATION(spellId);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(activation); } catch { }

                if (player.IsAlreadyDead) return;

                Creature? target = world.GetPlayerByObjectId(_targetObjectId)
                                ?? (Creature?)world.GetNpcByObjectId(_targetObjectId);
                if (target is null || target.IsAlreadyDead) return;
                if (target.Position.WorldId != castWorldId) return;
                if (castRange > 0f && player.Position.DistanceTo(target.Position) > castRange) return;

                bool spellIsMagical = template?.SkillType == SkillType.MAGICAL;

                // M258: skip resist/dodge when damage effect carries noresist="true"
                var stPreNoResistFx = template?.Effects?.DamageEffects;
                bool stNoResist = stPreNoResistFx is { Count: > 0 } && stPreNoResistFx[0].IsNoResist;
                // Magic resist check (Java calculateMagicalResistRate) / physical dodge check (calculatePhysicalDodgeRate)
                if (spellIsMagical && !stNoResist)
                {
                    int totalMagicAcc = player.BaseMagicAccuracy + player.BonusMagicalAccuracy + player.MagicAccDelta;
                    int targetMagicResist = (target is Player pvpResistTarget ? pvpResistTarget.BonusMagicResist
                                         : target is Npc npcResistTarget    ? NpcMagicResist(npcResistTarget)
                                         : 0) + target.MResistDebuffDelta + target.MResistStatUpDelta;
                    int resistRate = Math.Max(1, targetMagicResist - totalMagicAcc);
                    int tLvlST = target is Player pvpSTLvl ? pvpSTLvl.Level : target is Npc npcSTLvl ? npcSTLvl.Level : 0;
                    int lvlDiffST = tLvlST - player.Level - 2;
                    if (lvlDiffST > 0) resistRate += lvlDiffST * 100;
                    if (Random.Shared.Next(1000) < resistRate)
                    {
                        var resistPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(resistPkt); } catch { }
                        return;
                    }
                }
                else if (!spellIsMagical && !stNoResist) // physical skill — dodge check (Java calculatePhysicalDodgeRate)
                {
                    int physAccST = player.BasePhysicalAccuracy + player.BonusPhysicalAccuracy + player.PhysAccDelta;
                    int evST = (target is Player pvpEvST ? pvpEvST.BaseEvasion + pvpEvST.BonusEvasion
                              : target is Npc npcEvST   ? NpcPhysicalAccuracy(npcEvST) + (npcEvST.Template.Stats?.Evasion ?? 0)
                              : 0) + target.EvasionDebuffDelta + target.EvasionStatUpDelta;
                    float rawDodgeST = evST - physAccST;
                    if (target is Npc npcDodgeST)
                        rawDodgeST *= 1f + NpcLevelDiffMod(npcDodgeST.Level - player.Level);
                    float dodgeRateST = Math.Clamp(rawDodgeST * 0.6f + 50f, 0f, 300f);
                    if (Random.Shared.Next(1000) < (int)dodgeRateST)
                    {
                        var dodgePkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(dodgePkt); } catch { }
                        return;
                    }
                }

                var stDmgFx = template?.Effects?.DamageEffects;
                int? stSkillBase = stDmgFx is { Count: > 0 }
                    ? stDmgFx[0].BaseValue + stDmgFx[0].Delta * (_level - 1)
                    : null;

                int rawSpellDmg;
                if (spellIsMagical)
                {
                    int mAtkG = 100 + player.MainHandMagicalAtk + player.BonusMagicAtk + player.MagicAtkDebuffDelta + player.MagicAtkStatUpDelta;
                    int tMBSuppressG = target is Player pvpSuppG ? pvpSuppG.BonusMagicSuppression + pvpSuppG.MagicSuppressionDelta
                                     : target is Npc npcSuppG ? (npcSuppG.Template.Stats?.MBResist ?? 0) : 0;
                    float mbMultG = 1.0f + Math.Max(0, player.BonusMagicBoost + player.MagicBoostDelta - tMBSuppressG) / 1000f;
                    int stMagicBase = stSkillBase ?? player.Level * 6 + Random.Shared.Next(10, 40);
                    rawSpellDmg = (int)((mAtkG + stMagicBase) * mbMultG);
                }
                else
                {
                    int pAtkG = player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk + player.PatkStatUpDelta;
                    rawSpellDmg = pAtkG + (stSkillBase ?? player.Level * 4 + Random.Shared.Next(10, 40));
                }

                // Magical crit check (Java calculateMagicalCriticalRate, same piecewise formula as physical)
                if (spellIsMagical)
                {
                    int mCritRating = player.BaseMagicCritRating + player.BonusMagicalCritical + player.MagicCritDelta;
                    int mCritResist = target is Player pvpMCritTarget ? pvpMCritTarget.BonusMagicalCriticalResist + pvpMCritTarget.MagicCritResistDelta : 0;
                    mCritRating = Math.Max(0, mCritRating - mCritResist);
                    double mCritRate = mCritRating <= 440 ? mCritRating * 0.1
                                     : mCritRating <= 600 ? 44.0 + (mCritRating - 440) * 0.05
                                     : 52.0 + (mCritRating - 600) * 0.02;
                    if (Random.Shared.Next(100) < (int)mCritRate)
                    {
                        int spFt = target is Player pvpSpFt ? pvpSpFt.BonusSpellFortitude + pvpSpFt.SpellFortitudeDelta : 0;
                        float mCritCoeffG = Math.Max(1.0f, 1.5f - (float)Math.Round(spFt / 1000.0));
                        rawSpellDmg = (int)(rawSpellDmg * mCritCoeffG);
                    }
                }
                else // physical skill crit (Java calculatePhysicalCriticalRate, coefficient 1.5 for skills)
                {
                    int pCritRating = player.BaseCritRating + player.BonusPhysicalCritical + player.PhysCritDelta;
                    int pCritResist = target is Player pvpPCST ? pvpPCST.BonusPhysicalCriticalResist + pvpPCST.PhysCritResistDelta : 0;
                    pCritRating = Math.Max(0, pCritRating - pCritResist);
                    double pCritRate = pCritRating <= 440 ? pCritRating * 0.1
                                     : pCritRating <= 600 ? 44.0 + (pCritRating - 440) * 0.05
                                     : 52.0 + (pCritRating - 600) * 0.02;
                    if (Random.Shared.Next(100) < (int)pCritRate)
                    {
                        int sFortST = target is Player pvpSFST ? pvpSFST.BonusStrikeFortitude + pvpSFST.StrikeFortitudeDelta : 0;
                        float pCritCoeffST = Math.Max(1.0f, 1.5f - (float)Math.Round(sFortST / 1000.0));
                        rawSpellDmg = (int)(rawSpellDmg * pCritCoeffST);
                    }
                }

                // NPC level-diff or PvP damage reduction (Java StatFunctions.adjustDamages)
                if (target is Npc npcLvlSTDmg)
                {
                    float lvlModST = NpcLevelDiffMod(npcLvlSTDmg.Level - player.Level);
                    if (lvlModST > 0f) rawSpellDmg = Math.Max(1, (int)(rawSpellDmg * (1f - lvlModST)));
                }
                else if (target is Player)
                    rawSpellDmg = Math.Max(1, rawSpellDmg / 2); // PvP 50%

                // Java: MResist is resist-chance only; MAGICAL_DEFEND (=0 for NPCs) is separate damage mitigation
                int spellDef = target is Player pvpSpellTarget
                             ? (spellIsMagical ? pvpSpellTarget.MagicDefense + pvpSpellTarget.MagicDefDelta
                                               : pvpSpellTarget.PhysicalDefense + pvpSpellTarget.PdefDebuffDelta + pvpSpellTarget.PdefStatUpDelta)
                             : target is Npc npcSpellTarget
                             ? (spellIsMagical ? (npcSpellTarget.Template.Stats?.MBResist ?? 0)
                                               : (npcSpellTarget.Template.Stats?.PDef    ?? 0))
                             : 0;
                int damage = spellDef > 0 ? Math.Max(1, rawSpellDmg * 1000 / (1000 + spellDef)) : rawSpellDmg;
                // M247: noreducespellatk — defense-bypass damage replaces regular formula
                var stNoReduce = template?.Effects?.NoReduceEffects;
                if (stNoReduce is { Count: > 0 })
                {
                    int noReduceVal = stNoReduce[0].BaseValue + stNoReduce[0].Delta * (_level - 1);
                    damage = stNoReduce[0].IsPercent ? Math.Max(1, target.MaxHp * noReduceVal / 100) : Math.Max(1, noReduceVal);
                }
                target.CurrentHp = Math.Max(0, target.CurrentHp - damage);

                // M239: drain damage variants — caster restores HP/MP from dealt damage
                if (stDmgFx is { Count: > 0 } && (stDmgFx[0].HpPercent != 0 || stDmgFx[0].MpPercent != 0))
                {
                    // M254: physical drain → SkillAtkDrainInstant (23), magical → SpellAtkDrainInstant (24)
                    var stDrainLog = stDmgFx[0].DamageType == "physical"
                        ? SM_ATTACK_STATUS.LogId.SkillAtkDrainInstant
                        : SM_ATTACK_STATUS.LogId.SpellAtkDrainInstant;
                    if (stDmgFx[0].HpPercent != 0)
                    {
                        int hpGain = Math.Min(damage * stDmgFx[0].HpPercent / 100, player.MaxHp - player.CurrentHp);
                        if (hpGain > 0)
                        {
                            player.CurrentHp += hpGain;
                            var drainHpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, spellId, hpGain, stDrainLog);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(drainHpPkt); } catch { }
                        }
                    }
                    if (stDmgFx[0].MpPercent != 0)
                    {
                        int mpGain = Math.Min(damage * stDmgFx[0].MpPercent / 100, player.MaxMp - player.CurrentMp);
                        if (mpGain > 0)
                        {
                            player.CurrentMp += mpGain;
                            var drainMpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, spellId, mpGain, stDrainLog);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(drainMpPkt); } catch { }
                        }
                    }
                }

                // M240: mpattackinstant — burn target MP (Java MpAttackInstantEffect.reduceMp)
                var stMpFx = template?.Effects?.MpAttackEffects;
                if (stMpFx is { Count: > 0 })
                {
                    int mpBurnVal = stMpFx[0].BaseValue + stMpFx[0].Delta * (_level - 1);
                    int mpBurn = stMpFx[0].IsPercent ? target.MaxMp * mpBurnVal / 100 : mpBurnVal;
                    if (mpBurn > 0)
                        target.CurrentMp = Math.Max(0, target.CurrentMp - mpBurn);
                }

                // Both caster and target enter combat
                var combatNow = DateTime.UtcNow;
                player.LastCombatTime = combatNow;
                target.LastCombatTime = combatNow;

                // NPC retaliation: force NPC to engage the caster when hit by a spell
                if (target is Npc spellHitNpc && target.CurrentHp > 0)
                    npcAi.ForceEngage(spellHitNpc, player);

                if (target is Player damagedPlayer)
                {
                    var grp = damagedPlayer.Group;
                    if (grp is not null)
                    {
                        var update = new SM_GROUP_MEMBER_INFO(grp.GroupId, damagedPlayer, SM_GROUP_MEMBER_INFO.GroupEvent.Update);
                        foreach (var m in grp.Members)
                        {
                            if (m.ObjectId == damagedPlayer.ObjectId) continue;
                            var mc = registry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(update); } catch { }
                        }
                    }
                }

                // M255: procatk_instant uses LogId.ProcAtkInstant (92) per Java; other variants use SpellAtk
                var stStatusLog = (stDmgFx is { Count: > 0 } && stDmgFx[0].Variant == "procatk_instant")
                    ? SM_ATTACK_STATUS.LogId.ProcAtkInstant
                    : SM_ATTACK_STATUS.LogId.SpellAtk;
                var statusPkt = new SM_ATTACK_STATUS(target, SM_ATTACK_STATUS.AttackType.Damage, spellId, damage, stStatusLog);
                foreach (var c in registry.GetAll())
                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        try { await c.SendAsync(statusPkt); } catch { }

                // Dispel buff: strip all non-debuff effects from the target after impact
                if (template?.Effects?.HasDispelBuff == true && target.CurrentHp > 0)
                {
                    target.ClearBuffs();
                    bool dispelTargetIsPlayer = target is Player;
                    var dispelPkt = new SM_ABNORMAL_EFFECT(target.ObjectId, dispelTargetIsPlayer, target.GetActiveEffects());
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(dispelPkt); } catch { }
                }

                // DP gain on successful spell hit (150 DP per skill, capped at 6000)
                if (player.Dp < 6000)
                {
                    player.Dp = Math.Min(6000, player.Dp + 150);
                    try { await conn.SendAsync(new SM_DP_INFO(player.ObjectId, player.Dp)); } catch { }
                }

                // Caster/target-centered AoE splash: damage all additional enemies within effective_range
                if ((template?.IsCasterAoe == true || template?.IsTargetAoe == true) && template.EffectiveRange > 0)
                {
                    float aoeR   = template.EffectiveRange;
                    float aoeAlt = Math.Max(1f, template.EffectiveAltitude);
                    int   maxHits = template.TargetMaxCount;
                    Position center = template.IsCasterAoe ? player.Position : target.Position;
                    bool hitsEnemies = !string.Equals(template.TargetRelation, "FRIEND",
                                           StringComparison.OrdinalIgnoreCase);
                    int splashCount = 1; // primary target already counted

                    var splashNpcs = world.GetAllNpcs()
                        .Where(n => !n.IsAlreadyDead
                                 && n.ObjectId != target.ObjectId
                                 && n.Position.WorldId == castWorldId
                                 && hitsEnemies)
                        .Where(n => {
                            float dx = n.Position.X - center.X, dy = n.Position.Y - center.Y, dz = n.Position.Z - center.Z;
                            return dx * dx + dy * dy <= aoeR * aoeR && Math.Abs(dz) <= aoeAlt;
                        });

                    foreach (var splash in splashNpcs)
                    {
                        if (splashCount >= maxHits) break;
                        splashCount++;

                        int splashMBSuppress = spellIsMagical ? (splash.Template.Stats?.MBResist ?? 0) : 0;
                        float splashMBMult = 1.0f + Math.Max(0, player.BonusMagicBoost + player.MagicBoostDelta - splashMBSuppress) / 1000f;
                        int splashBase = stSkillBase ?? (spellIsMagical ? player.Level * 6 : player.Level * 4) + Random.Shared.Next(10, 40);
                        int splashRaw = spellIsMagical
                            ? (int)(((100 + player.MainHandMagicalAtk + player.BonusMagicAtk + player.MagicAtkDebuffDelta + player.MagicAtkStatUpDelta) + splashBase) * splashMBMult)
                            : (player.BasePhysicalAttack + (player.MainHandMinDmg + player.MainHandMaxDmg) / 2 + player.BonusPhysicalAtk + player.PatkStatUpDelta) + splashBase;

                        // Magic resist check for AoE splash / physical dodge check (splash targets are NPC-only)
                        if (spellIsMagical)
                        {
                            int totalMagicAccS = player.BaseMagicAccuracy + player.BonusMagicalAccuracy + player.MagicAccDelta;
                            int splashMR = NpcMagicResist(splash);
                            int splashResistRate = Math.Max(1, splashMR - totalMagicAccS);
                            int splashLvlDiff = splash.Level - player.Level - 2;
                            if (splashLvlDiff > 0) splashResistRate += splashLvlDiff * 100;
                            if (Random.Shared.Next(1000) < splashResistRate)
                            {
                                var resistPktS = new SM_ATTACK_STATUS(splash, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(resistPktS); } catch { }
                                continue;
                            }
                        }
                        else // physical splash — dodge check (NPC-only; no PvP evasion resist)
                        {
                            int physAccSpl = player.BasePhysicalAccuracy + player.BonusPhysicalAccuracy + player.PhysAccDelta;
                            int evSpl = NpcPhysicalAccuracy(splash) + (splash.Template.Stats?.Evasion ?? 0);
                            float rawDodgeSpl = (evSpl - physAccSpl) * (1f + NpcLevelDiffMod(splash.Level - player.Level));
                            float dodgeRateSpl = Math.Clamp(rawDodgeSpl * 0.6f + 50f, 0f, 300f);
                            if (Random.Shared.Next(1000) < (int)dodgeRateSpl)
                            {
                                var dodgePktS = new SM_ATTACK_STATUS(splash, SM_ATTACK_STATUS.AttackType.Damage, spellId, 0, SM_ATTACK_STATUS.LogId.SpellAtk);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(dodgePktS); } catch { }
                                continue;
                            }
                        }

                        // Magical crit check for AoE splash
                        if (spellIsMagical)
                        {
                            int mCritRatingS = player.BaseMagicCritRating + player.BonusMagicalCritical + player.MagicCritDelta;
                            double mCritRateS = mCritRatingS <= 440 ? mCritRatingS * 0.1
                                              : mCritRatingS <= 600 ? 44.0 + (mCritRatingS - 440) * 0.05
                                              : 52.0 + (mCritRatingS - 600) * 0.02;
                            if (Random.Shared.Next(100) < (int)mCritRateS)
                                splashRaw = (int)(splashRaw * 1.5f);
                        }
                        else // physical skill crit for AoE splash (splash targets are NPCs, no PvP crit resist)
                        {
                            int pCritSpl = player.BaseCritRating + player.BonusPhysicalCritical + player.PhysCritDelta;
                            double pCritRateSpl = pCritSpl <= 440 ? pCritSpl * 0.1
                                                : pCritSpl <= 600 ? 44.0 + (pCritSpl - 440) * 0.05
                                                : 52.0 + (pCritSpl - 600) * 0.02;
                            if (Random.Shared.Next(100) < (int)pCritRateSpl)
                                splashRaw = (int)(splashRaw * 1.5f);
                        }

                        int splashDef = spellIsMagical ? (splash.Template.Stats?.MBResist ?? 0)
                                                       : (splash.Template.Stats?.PDef    ?? 0);
                        int splashDmg = splashDef > 0 ? Math.Max(1, splashRaw * 1000 / (1000 + splashDef)) : splashRaw;
                        // M253: noreducespellatk — defense-bypass damage applies to splash targets too
                        if (stNoReduce is { Count: > 0 })
                        {
                            int splashNoReduceVal = stNoReduce[0].BaseValue + stNoReduce[0].Delta * (_level - 1);
                            splashDmg = stNoReduce[0].IsPercent ? Math.Max(1, splash.MaxHp * splashNoReduceVal / 100) : Math.Max(1, splashNoReduceVal);
                        }
                        splash.CurrentHp      = Math.Max(0, splash.CurrentHp - splashDmg);
                        splash.LastCombatTime = DateTime.UtcNow;

                        // M239: drain damage variants — splash hit also restores HP/MP to caster
                        if (stDmgFx is { Count: > 0 } && (stDmgFx[0].HpPercent != 0 || stDmgFx[0].MpPercent != 0))
                        {
                            // M254: physical drain → SkillAtkDrainInstant (23), magical → SpellAtkDrainInstant (24)
                            var splashDrainLog = stDmgFx[0].DamageType == "physical"
                                ? SM_ATTACK_STATUS.LogId.SkillAtkDrainInstant
                                : SM_ATTACK_STATUS.LogId.SpellAtkDrainInstant;
                            if (stDmgFx[0].HpPercent != 0)
                            {
                                int hpGain = Math.Min(splashDmg * stDmgFx[0].HpPercent / 100, player.MaxHp - player.CurrentHp);
                                if (hpGain > 0)
                                {
                                    player.CurrentHp += hpGain;
                                    var drainHpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalHp, spellId, hpGain, splashDrainLog);
                                    foreach (var c in registry.GetAll())
                                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                            try { await c.SendAsync(drainHpPkt); } catch { }
                                }
                            }
                            if (stDmgFx[0].MpPercent != 0)
                            {
                                int mpGain = Math.Min(splashDmg * stDmgFx[0].MpPercent / 100, player.MaxMp - player.CurrentMp);
                                if (mpGain > 0)
                                {
                                    player.CurrentMp += mpGain;
                                    var drainMpPkt = new SM_ATTACK_STATUS(player, SM_ATTACK_STATUS.AttackType.NaturalMp, spellId, mpGain, splashDrainLog);
                                    foreach (var c in registry.GetAll())
                                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                            try { await c.SendAsync(drainMpPkt); } catch { }
                                }
                            }
                        }

                        // M240: mpattackinstant — burn splash target MP
                        if (stMpFx is { Count: > 0 })
                        {
                            int mpBurnVal = stMpFx[0].BaseValue + stMpFx[0].Delta * (_level - 1);
                            int mpBurn = stMpFx[0].IsPercent ? splash.MaxMp * mpBurnVal / 100 : mpBurnVal;
                            if (mpBurn > 0)
                                splash.CurrentMp = Math.Max(0, splash.CurrentMp - mpBurn);
                        }

                        if (splash.CurrentHp > 0)
                            npcAi.ForceEngage(splash, player);

                        // M255: procatk_instant uses LogId.ProcAtkInstant (92) per Java; other variants use SpellAtk
                        var splashStatusLog = (stDmgFx is { Count: > 0 } && stDmgFx[0].Variant == "procatk_instant")
                            ? SM_ATTACK_STATUS.LogId.ProcAtkInstant
                            : SM_ATTACK_STATUS.LogId.SpellAtk;
                        var splashPkt = new SM_ATTACK_STATUS(splash, SM_ATTACK_STATUS.AttackType.Damage, spellId, splashDmg, splashStatusLog);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(splashPkt); } catch { }

                        if (splash.CurrentHp <= 0)
                        {
                            splash.State |= CreatureState.Dead;
                            int splashWorld = splash.Position.WorldId;
                            var splashDie = new SM_EMOTION(splash, EmotionType.DIE);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == splashWorld)
                                    try { await c.SendAsync(splashDie); } catch { }

                            var shout = _dataManager.NpcShouts.GetRandomShout(
                                splash.Template.NpcId, NpcShoutData.ShoutEventType.DIED, splashWorld);
                            if (shout.HasValue)
                            {
                                var shoutPkt2 = SM_SYSTEM_MESSAGE.NpcShout(splash.ObjectId, shout.Value.StringId);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == splashWorld)
                                        try { await c.SendAsync(shoutPkt2); } catch { }
                            }

                            world.Remove(splash);
                            _lootService.GenerateDrops(splash, player);
                            await _questService.HandleNpcKillAsync(player, splash, conn, CancellationToken.None);
                            long splashXp = splash.Template.Stats?.MaxXp > 0 ? splash.Template.Stats.MaxXp : splash.Level * 50L;
                            await _expService.AddGroupExpAsync(player, splashXp, splash.Level, CancellationToken.None);

                            var deadSplash    = splash;
                            var splashDrops   = _lootService.GetLoot(deadSplash.ObjectId);
                            int splashDecayMs = splashDrops is null ? 5_000 : splashDrops.Count == 0 ? 90_000 : 300_000;
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(splashDecayMs);
                                var del = new SM_DELETE(deadSplash.ObjectId);
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == deadSplash.Position.WorldId)
                                        try { await c.SendAsync(del); } catch { }
                                _lootService.ClearLoot(deadSplash.ObjectId);
                                _spawnService.ScheduleRespawn(deadSplash);
                            });
                        }

                        // Apply DoT (bleed/poison/disease) to surviving AoE splash targets
                        if (splash.CurrentHp > 0 && template?.Effects?.DotEffects is { Count: > 0 } splashDots)
                        {
                            foreach (var dot in splashDots)
                            {
                                int dotLvSpl    = _level;
                                int dotTickDmg  = Math.Max(1, dot.BaseValue + dot.Delta * dotLvSpl);
                                var dotExpiry   = DateTime.UtcNow.AddMilliseconds(dot.Duration2Ms);
                                var dotEffect   = new AbnormalState
                                {
                                    SkillId    = spellId,
                                    SkillLevel = dotLvSpl,
                                    EffectorId = player.ObjectId,
                                    Expiry     = dotExpiry,
                                    DotInfo    = dot,
                                    IsDebuff   = true,
                                };
                                splash.AddEffect(dotEffect);
                                var dotAbnormal = new SM_ABNORMAL_EFFECT(splash.ObjectId, isPlayer: false, splash.GetActiveEffects());
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                        try { await c.SendAsync(dotAbnormal); } catch { }

                                var dotTickTarget = splash;
                                var dotTickEffect = dotEffect;
                                var dotTickCaster = player;
                                var dotTickInfo   = dot;
                                var dotTickLogId  = dot.DotType switch
                                {
                                    "bleed"         => SM_ATTACK_STATUS.LogId.Bleed,
                                    "spellatk"      => SM_ATTACK_STATUS.LogId.SpellAtk,
                                    "spellatkdrain" => SM_ATTACK_STATUS.LogId.SpellAtkDrain,
                                    _               => SM_ATTACK_STATUS.LogId.Poison,
                                };
                                _ = Task.Run(async () =>
                                {
                                    while (!dotTickTarget.IsAlreadyDead && DateTime.UtcNow < dotTickEffect.Expiry)
                                    {
                                        await Task.Delay(dotTickInfo.CheckTimeMs);
                                        if (dotTickTarget.IsAlreadyDead || DateTime.UtcNow >= dotTickEffect.Expiry) break;
                                        dotTickTarget.CurrentHp = Math.Max(0, dotTickTarget.CurrentHp - dotTickDmg);
                                        if (dotTickInfo.HpPercent != 0)
                                            dotTickCaster.CurrentHp = Math.Min(dotTickCaster.MaxHp, dotTickCaster.CurrentHp + dotTickDmg * dotTickInfo.HpPercent / 100);
                                        if (dotTickInfo.MpPercent != 0)
                                            dotTickCaster.CurrentMp = Math.Min(dotTickCaster.MaxMp, dotTickCaster.CurrentMp + dotTickDmg * dotTickInfo.MpPercent / 100);
                                        var tickPkt = new SM_ATTACK_STATUS(dotTickTarget, SM_ATTACK_STATUS.AttackType.Damage, spellId, dotTickDmg, dotTickLogId);
                                        int tw = dotTickTarget.Position.WorldId;
                                        foreach (var c in registry.GetAll())
                                            if (c.ActivePlayer?.Position.WorldId == tw)
                                                try { await c.SendAsync(tickPkt); } catch { }
                                    }
                                    dotTickTarget.RemoveEffect(dotTickEffect.SkillId, dotTickEffect.Expiry);
                                    var expiredDot = new SM_ABNORMAL_EFFECT(dotTickTarget.ObjectId, isPlayer: false, dotTickTarget.GetActiveEffects());
                                    int dw = dotTickTarget.Position.WorldId;
                                    foreach (var c in registry.GetAll())
                                        if (c.ActivePlayer?.Position.WorldId == dw)
                                            try { await c.SendAsync(expiredDot); } catch { }
                                });
                            }
                        }
                    }
                }

                // Apply DEBUFF visual effect when:
                // (a) DEBUFF subtype with template.Duration > 0, OR
                // (b) ATTACK/other subtype with tslot=DEBUFF and effect elements have a duration2
                // Java: AttackSkill effects (slow/statdown etc.) apply via EffectController alongside damage
                int debuffDurationMs = template?.Duration > 0 ? template.Duration
                                     : (template?.Effects?.EffectDuration ?? 0);
                bool isDebuffSkill = template?.SubType == SkillSubType.DEBUFF
                                  || (string.Equals(template?.TSlot, "DEBUFF",
                                          StringComparison.OrdinalIgnoreCase) && debuffDurationMs > 0);
                if (target.CurrentHp > 0 && isDebuffSkill && debuffDurationMs > 0)
                {
                    bool debuffTargetIsPlayer = target is Player;
                    int  snareSpeedPct        = template?.Effects?.SnareSpeedPct     ?? 0;
                    int  slowAtkPct           = template?.Effects?.SlowAttackSpeedPct ?? 0;
                    int  pdefDelta            = template?.Effects?.PdefAddDelta       ?? 0;
                    int  mresistDelta         = template?.Effects?.MResistAddDelta    ?? 0;
                    int  patkDelta            = template?.Effects?.PhysAtkAddDelta    ?? 0;
                    int  evasionDelta         = template?.Effects?.EvasionAddDelta    ?? 0;
                    int  maxHpDelta           = template?.Effects?.MaxHpAddDelta      ?? 0;
                    int  magicAtkDelta        = template?.Effects?.MagicAtkAddDelta   ?? 0;
                    int  atkSpdDelta          = template?.Effects?.AtkSpeedAddDelta   ?? 0;
                    int  maxMpDelta           = template?.Effects?.MaxMpAddDelta        ?? 0;
                    int  mBoostDebuffDelta     = template?.Effects?.MagicBoostAddDelta   ?? 0;
                    int  physAccDelta          = template?.Effects?.PhysAccAddDelta       ?? 0;
                    int  magicAccDelta         = template?.Effects?.MagicAccAddDelta      ?? 0;
                    int  parryDelta            = template?.Effects?.ParryAddDelta         ?? 0;
                    int  blockDelta            = template?.Effects?.BlockAddDelta         ?? 0;
                    int  physCritDelta         = template?.Effects?.PhysCritAddDelta         ?? 0;
                    int  magicCritDelta        = template?.Effects?.MagicCritAddDelta        ?? 0;
                    int  physCritResistDelta   = template?.Effects?.PhysCritResistAddDelta    ?? 0;
                    int  magicCritResistDelta  = template?.Effects?.MagicCritResistAddDelta   ?? 0;
                    int  strikeFortitudeDelta  = template?.Effects?.StrikeFortitudeAddDelta   ?? 0;
                    int  spellFortitudeDelta   = template?.Effects?.SpellFortitudeAddDelta    ?? 0;
                    int  castTimeDelta         = template?.Effects?.CastTimeAddDelta           ?? 0;
                    int  concentrationDelta    = template?.Effects?.ConcentrationAddDelta      ?? 0;
                    int  magicSuppressionDelta = template?.Effects?.MagicSuppressionAddDelta   ?? 0;
                    int  magicDefDelta         = template?.Effects?.MagicDefAddDelta            ?? 0;
                    var  debuffEffect = new AbnormalState
                    {
                        SkillId             = spellId,
                        SkillLevel          = _level,
                        EffectorId          = player.ObjectId,
                        Expiry              = DateTime.UtcNow.AddMilliseconds(debuffDurationMs),
                        CcFlags             = template!.CcFlags,
                        IsDebuff            = true,
                        MovSpeedPct         = snareSpeedPct,
                        PreDebuffSpeed      = target.MovementSpeed,
                        AttackSpeedPct      = slowAtkPct,
                        PreDebuffAtkSpeed   = target.CurrentAttackSpeed,
                        PdefDelta           = pdefDelta,
                        MResistDelta        = mresistDelta,
                        PatkDelta           = patkDelta,
                        EvasionDelta        = evasionDelta,
                        MaxHpDelta          = maxHpDelta,
                        MagicAtkDelta       = magicAtkDelta,
                        AtkSpeedDelta       = atkSpdDelta,
                        MaxMpDelta          = maxMpDelta,
                        MagicBoostDeltaVal  = mBoostDebuffDelta,
                        PhysAccDeltaVal     = physAccDelta,
                        MagicAccDeltaVal    = magicAccDelta,
                        ParryDeltaVal       = parryDelta,
                        BlockDeltaVal       = blockDelta,
                        PhysCritDeltaVal        = physCritDelta,
                        MagicCritDeltaVal       = magicCritDelta,
                        PhysCritResistDeltaVal  = physCritResistDelta,
                        MagicCritResistDeltaVal = magicCritResistDelta,
                        StrikeFortitudeDeltaVal = strikeFortitudeDelta,
                        SpellFortitudeDeltaVal  = spellFortitudeDelta,
                        CastTimeDeltaVal         = castTimeDelta,
                        ConcentrationDeltaVal    = concentrationDelta,
                        MagicSuppressionDeltaVal = magicSuppressionDelta,
                        MagicDefDeltaVal         = magicDefDelta,
                    };
                    target.AddEffect(debuffEffect);

                    // Snare: reduce movement speed; Slow: increase attack speed (higher = slower)
                    bool speedChanged = snareSpeedPct != 0 || slowAtkPct != 0;
                    if (snareSpeedPct != 0)
                        target.MovementSpeed = Math.Max(1.0f, target.MovementSpeed * (100 + snareSpeedPct) / 100f);
                    if (slowAtkPct != 0)
                        target.CurrentAttackSpeed = Math.Max(500, (int)(target.CurrentAttackSpeed * (100 + slowAtkPct) / 100f));
                    if (speedChanged)
                    {
                        var speedEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(speedEmo); } catch { }
                    }

                    if (atkSpdDelta != 0)
                    {
                        var atkSpdEmo = new SM_EMOTION(target, EmotionType.START_EMOTE2);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(atkSpdEmo); } catch { }
                    }
                    if ((pdefDelta != 0 || mresistDelta != 0 || patkDelta != 0 || evasionDelta != 0 || maxHpDelta != 0 || magicAtkDelta != 0 || atkSpdDelta != 0 || maxMpDelta != 0 || mBoostDebuffDelta != 0 || physAccDelta != 0 || magicAccDelta != 0 || parryDelta != 0 || blockDelta != 0 || physCritDelta != 0 || magicCritDelta != 0 || physCritResistDelta != 0 || magicCritResistDelta != 0 || strikeFortitudeDelta != 0 || spellFortitudeDelta != 0 || castTimeDelta != 0 || concentrationDelta != 0 || magicSuppressionDelta != 0 || magicDefDelta != 0) && target is Player debuffedPlayer)
                    {
                        var statsInfo = new SM_STATS_INFO(debuffedPlayer, _dataManager.PlayerStats.GetTemplate(debuffedPlayer.PlayerClass, debuffedPlayer.Level));
                        var dc = registry.GetAll().FirstOrDefault(c => c.ActivePlayer == debuffedPlayer);
                        if (dc is not null) try { await dc.SendAsync(statsInfo); } catch { }
                    }

                    var debuffAbnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, debuffTargetIsPlayer,
                                            target.GetActiveEffects());
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(debuffAbnormal); } catch { }

                    // Java RootEffect/StunEffect/ParalyzeEffect: broadcast SM_TARGET_IMMOBILIZE to freeze
                    // the target's position on all clients when any movement-blocking CC is applied
                    if ((debuffEffect.CcFlags & AbnormalCcFlags.CantMove) != 0)
                    {
                        var immobilize = new SM_TARGET_IMMOBILIZE(target);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(immobilize); } catch { }
                    }

                    // Schedule expiry broadcast at target's current zone
                    var expEffect      = debuffEffect;
                    var expTarget      = target;
                    int expDurationMs  = debuffDurationMs;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(expDurationMs);
                        bool statChanged = expEffect.PdefDelta != 0 || expEffect.MResistDelta != 0 ||
                            expEffect.PatkDelta != 0 || expEffect.EvasionDelta != 0 ||
                            expEffect.MaxHpDelta != 0 || expEffect.MagicAtkDelta != 0 ||
                            expEffect.AtkSpeedDelta != 0 || expEffect.MaxMpDelta != 0 ||
                            expEffect.MagicBoostDeltaVal != 0 || expEffect.PhysAccDeltaVal != 0 ||
                            expEffect.MagicAccDeltaVal != 0 || expEffect.ParryDeltaVal != 0 ||
                            expEffect.BlockDeltaVal != 0 || expEffect.PhysCritDeltaVal != 0 ||
                            expEffect.MagicCritDeltaVal != 0 || expEffect.PhysCritResistDeltaVal != 0 ||
                            expEffect.MagicCritResistDeltaVal != 0 || expEffect.StrikeFortitudeDeltaVal != 0 ||
                            expEffect.SpellFortitudeDeltaVal != 0 || expEffect.CastTimeDeltaVal != 0 ||
                            expEffect.ConcentrationDeltaVal != 0 || expEffect.MagicSuppressionDeltaVal != 0 ||
                            expEffect.MagicDefDeltaVal != 0;
                        bool speedRestored    = expEffect.MovSpeedPct != 0 || expEffect.AttackSpeedPct != 0;
                        bool atkSpeedRestored = expEffect.AtkSpeedDelta != 0;

                        expTarget.RemoveEffectBySkillId(expEffect.SkillId);

                        if (speedRestored)
                        {
                            var restoreEmo = new SM_EMOTION(expTarget, EmotionType.START_EMOTE2);
                            int restoreWorld = expTarget.Position.WorldId;
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == restoreWorld)
                                    try { await c.SendAsync(restoreEmo); } catch { }
                        }
                        if (atkSpeedRestored)
                        {
                            var restoreAtkEmo = new SM_EMOTION(expTarget, EmotionType.START_EMOTE2);
                            int restoreAtkWorld = expTarget.Position.WorldId;
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == restoreAtkWorld)
                                    try { await c.SendAsync(restoreAtkEmo); } catch { }
                        }
                        if (statChanged && expTarget is Player restoredPlayer)
                        {
                            var statsInfo = new SM_STATS_INFO(restoredPlayer, _dataManager.PlayerStats.GetTemplate(restoredPlayer.PlayerClass, restoredPlayer.Level));
                            var dc = registry.GetAll().FirstOrDefault(c => c.ActivePlayer == restoredPlayer);
                            if (dc is not null) try { await dc.SendAsync(statsInfo); } catch { }
                        }
                        var expired = new SM_ABNORMAL_EFFECT(expTarget.ObjectId, debuffTargetIsPlayer,
                                          expTarget.GetActiveEffects());
                        int expWorldId = expTarget.Position.WorldId;
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == expWorldId)
                                try { await c.SendAsync(expired); } catch { }
                    });
                }

                // Apply DoT (bleed/poison/disease) effects from skill <effects> block
                // Duration comes from the effect's duration2 attribute (template.Duration is 0 for DoT skills)
                if (target.CurrentHp > 0 && template?.Effects?.DotEffects is { Count: > 0 } dots)
                {
                    foreach (var dot in dots)
                    {
                        int skillLv    = _level;
                        int dmgPerTick = Math.Max(1, dot.BaseValue + dot.Delta * skillLv);
                        var dotExpiry  = DateTime.UtcNow.AddMilliseconds(dot.Duration2Ms);
                        var dotEffect  = new AbnormalState
                        {
                            SkillId    = spellId,
                            SkillLevel = skillLv,
                            EffectorId = player.ObjectId,
                            Expiry     = dotExpiry,
                            DotInfo    = dot,
                            IsDebuff   = true,
                        };
                        target.AddEffect(dotEffect);

                        bool dotTargetIsPlayer = target is Player;
                        var dotAbnormal = new SM_ABNORMAL_EFFECT(target.ObjectId, dotTargetIsPlayer,
                                              target.GetActiveEffects());
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                try { await c.SendAsync(dotAbnormal); } catch { }

                        // Schedule periodic ticks then expiry removal
                        var tickTarget  = target;
                        var tickEffect  = dotEffect;
                        var tickCaster  = player;
                        var tickInfo    = dot;
                        var tickLogId   = dot.DotType switch
                        {
                            "bleed"         => SM_ATTACK_STATUS.LogId.Bleed,
                            "spellatk"      => SM_ATTACK_STATUS.LogId.SpellAtk,
                            "spellatkdrain" => SM_ATTACK_STATUS.LogId.SpellAtkDrain,
                            _               => SM_ATTACK_STATUS.LogId.Poison,
                        };
                        _ = Task.Run(async () =>
                        {
                            while (!tickTarget.IsAlreadyDead && DateTime.UtcNow < tickEffect.Expiry)
                            {
                                await Task.Delay(tickInfo.CheckTimeMs);
                                if (tickTarget.IsAlreadyDead || DateTime.UtcNow >= tickEffect.Expiry) break;

                                tickTarget.CurrentHp = Math.Max(0, tickTarget.CurrentHp - dmgPerTick);
                                if (tickInfo.HpPercent != 0)
                                    tickCaster.CurrentHp = Math.Min(tickCaster.MaxHp, tickCaster.CurrentHp + dmgPerTick * tickInfo.HpPercent / 100);
                                if (tickInfo.MpPercent != 0)
                                    tickCaster.CurrentMp = Math.Min(tickCaster.MaxMp, tickCaster.CurrentMp + dmgPerTick * tickInfo.MpPercent / 100);
                                var tickPkt = new SM_ATTACK_STATUS(tickTarget, SM_ATTACK_STATUS.AttackType.Damage, spellId, dmgPerTick, tickLogId);
                                int tickWorld = tickTarget.Position.WorldId;
                                foreach (var c in registry.GetAll())
                                    if (c.ActivePlayer?.Position.WorldId == tickWorld)
                                        try { await c.SendAsync(tickPkt); } catch { }
                            }
                            tickTarget.RemoveEffect(tickEffect.SkillId, tickEffect.Expiry);
                            var expiredDot = new SM_ABNORMAL_EFFECT(tickTarget.ObjectId, dotTargetIsPlayer,
                                                tickTarget.GetActiveEffects());
                            int dotExpWorld = tickTarget.Position.WorldId;
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == dotExpWorld)
                                    try { await c.SendAsync(expiredDot); } catch { }
                        });
                    }
                }

                if (target.CurrentHp > 0) return;

                if (target is Player deadPlayer)
                {
                    // Duel: end without killing — restore 1 HP, send result, clear duel state
                    if (duelSvc.GetOpponent(player.ObjectId) == deadPlayer.ObjectId)
                    {
                        deadPlayer.CurrentHp = 1;
                        duelSvc.EndDuel(player.ObjectId, deadPlayer.ObjectId);
                        try { await conn.SendAsync(SM_DUEL.Won(deadPlayer.Name)); } catch { }
                        var loserConn = registry.Get(deadPlayer.ObjectId);
                        if (loserConn is not null)
                            try { await loserConn.SendAsync(SM_DUEL.Lost(player.Name)); } catch { }
                        return;
                    }

                    deadPlayer.State |= CreatureState.Dead;
                    deadPlayer.ClearAllEffects();
                    var die         = new SM_EMOTION(deadPlayer, EmotionType.DIE);
                    var clearEffect = new SM_ABNORMAL_EFFECT(deadPlayer.ObjectId, isPlayer: true);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                        {
                            try { await c.SendAsync(die); } catch { }
                            try { await c.SendAsync(clearEffect); } catch { }
                        }
                    var targetConn = registry.Get(deadPlayer.ObjectId);
                    if (targetConn is not null)
                    {
                        try { await targetConn.SendAsync(new SM_DIE()); } catch { }
                        try { await targetConn.SendAsync(SM_SYSTEM_MESSAGE.YouWereKilledBy(player.Name)); } catch { }
                    }

                    // Zone-wide kill announcement: "%0 was killed by %1's attack."
                    var killAnnounce = SM_SYSTEM_MESSAGE.PlayerKilledByPlayer(deadPlayer.Name, player.Name);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == castWorldId)
                            try { await c.SendAsync(killAnnounce); } catch { }

                    // Group members see "[player] has died."
                    var deadGroup = deadPlayer.Group;
                    if (deadGroup is not null)
                    {
                        var groupDied = SM_SYSTEM_MESSAGE.GroupMemberDied(deadPlayer.Name);
                        foreach (var m in deadGroup.Members)
                        {
                            if (m.ObjectId == deadPlayer.ObjectId) continue;
                            var mc = registry.Get(m.ObjectId);
                            if (mc is not null) try { await mc.SendAsync(groupDied); } catch { }
                        }
                    }

                    // PvP AP exchange — only in Abyss/Balaurea maps AND opposing factions
                    if (AbyssRankService.IsPvPMap(castWorldId) && player.Race != deadPlayer.Race)
                    {
                        int apGain = AbyssRankService.CalculatePvPApGained(player, deadPlayer);
                        if (rates.ApPlayerGainRate != 1.0f)
                            apGain = Math.Max(1, (int)(apGain * rates.ApPlayerGainRate));
                        int apLoss = AbyssRankService.CalculatePvPApLost(player, deadPlayer);
                        bool spellPvpRankUp = AbyssRankService.AddAp(player, apGain);
                        AbyssRankService.LoseAp(deadPlayer, apLoss);
                        AbyssRankService.TrackPvPKill(player, apGain);

                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (spellPvpRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        if (targetConn is not null)
                            try { await targetConn.SendAsync(SM_ABYSS_RANK.ForPlayer(deadPlayer), CancellationToken.None); } catch { }

                        await playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await playerDao.UpdateAbyssAsync(deadPlayer.ObjectId, deadPlayer.AbyssPoints, deadPlayer.AbyssRank, CancellationToken.None);
                        await playerDao.UpdateAbyssKillStatsAsync(player.ObjectId,
                            player.AbyssAllKill, player.AbyssMaxRank,
                            player.AbyssDailyKill, player.AbyssDailyAp,
                            player.AbyssWeeklyKill, player.AbyssWeeklyAp,
                            player.AbyssLastKill, player.AbyssLastAp, CancellationToken.None);
                        await AwardLegionContributionAsync(player, apGain, registry, legionDao, CancellationToken.None);
                    }
                }
                else if (target is Npc deadNpc)
                {
                    deadNpc.State |= CreatureState.Dead;
                    int npcWorldId = deadNpc.Position.WorldId;
                    var die = new SM_EMOTION(deadNpc, EmotionType.DIE);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(die); } catch { }

                    // DIED shout — NPC death cry
                    var diedShout = _dataManager.NpcShouts.GetRandomShout(
                        deadNpc.Template.NpcId, NpcShoutData.ShoutEventType.DIED, npcWorldId);
                    if (diedShout.HasValue)
                    {
                        var shoutPkt = SM_SYSTEM_MESSAGE.NpcShout(deadNpc.ObjectId, diedShout.Value.StringId);
                        foreach (var c in registry.GetAll())
                            if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                                try { await c.SendAsync(shoutPkt); } catch { }
                    }

                    world.Remove(deadNpc);

                    lootSvc.GenerateDrops(deadNpc, player);

                    // Update quest kill progress
                    await questSvc.HandleNpcKillAsync(player, deadNpc, conn, CancellationToken.None);

                    // Award XP — level-diff scaling and group distribution handled inside AddGroupExpAsync
                    long xpBase = deadNpc.Template.Stats?.MaxXp > 0
                        ? deadNpc.Template.Stats.MaxXp
                        : deadNpc.Level * 50L;
                    await expSvc.AddGroupExpAsync(player, xpBase, deadNpc.Level, CancellationToken.None);

                    // Award AP for kills in the Abyss world or against ABYSS_GUARD NPCs; persist immediately
                    if (deadNpc.Position.WorldId == AbyssRankService.AbyssWorldId
                        || deadNpc.Template.NpcType.Contains("ABYSS", StringComparison.OrdinalIgnoreCase))
                    {
                        int ap = AbyssRankService.CalculateNpcApReward(deadNpc.Level);
                        bool spellNpcRankUp = AbyssRankService.AddAp(player, ap);
                        if (player.AbyssRank > player.AbyssMaxRank) player.AbyssMaxRank = player.AbyssRank;
                        try { await conn.SendAsync(SM_ABYSS_RANK.ForPlayer(player), CancellationToken.None); } catch { }
                        if (spellNpcRankUp)
                        {
                            var rankPkt = new SM_ABYSS_RANK_UPDATE(player.ObjectId, player.AbyssRank);
                            foreach (var c in registry.GetAll())
                                if (c.ActivePlayer?.Position.WorldId == castWorldId)
                                    try { await c.SendAsync(rankPkt); } catch { }
                        }
                        await playerDao.UpdateAbyssAsync(player.ObjectId, player.AbyssPoints, player.AbyssRank, CancellationToken.None);
                        await AwardLegionContributionAsync(player, ap, registry, legionDao, CancellationToken.None);
                    }

                    var stDrops  = lootSvc.GetLoot(deadNpc.ObjectId);
                    int stDecayMs = stDrops is null ? 5_000 : stDrops.Count == 0 ? 90_000 : 300_000;
                    await Task.Delay(stDecayMs);
                    var del = new SM_DELETE(deadNpc.ObjectId);
                    foreach (var c in registry.GetAll())
                        if (c.ActivePlayer?.Position.WorldId == npcWorldId)
                            try { await c.SendAsync(del); } catch { }
                    lootSvc.ClearLoot(deadNpc.ObjectId);
                    spawnSvc.ScheduleRespawn(deadNpc);
                }
            });
        }
        else
        {
            // Buff, chant, passive, or unknown — broadcast activation immediately
            var activation = new SM_SKILL_ACTIVATION(_spellId);
            await BroadcastAsync(activation, ct);
        }
    }

    private async ValueTask BroadcastAsync(AionServerPacket packet, CancellationToken ct)
    {
        try { await _conn.SendAsync(packet, ct); } catch { }
        int worldId = _conn.ActivePlayer!.Position.WorldId;
        foreach (var other in _connRegistry.GetAllExcept(_conn.ActivePlayer!.ObjectId))
            if (other.ActivePlayer?.Position.WorldId == worldId)
                try { await other.SendAsync(packet, ct); } catch { }
    }

    // Java NpcGameStats.getMResist(): base = round(level*17.5+75) when mRes==0; template MResist adds on top
    private static int NpcMagicResist(Model.Npc npc)
    {
        int @base = (int)Math.Round(npc.Level * 17.5f + 75);
        int tpl   = npc.Template.Stats?.MResist ?? 0;
        return tpl > 0 ? tpl : @base;
    }

    // Java NpcGameStats.calcStats(): level*(33.6-0.16*level)+5; base evasion and physical accuracy for NPCs
    private static int NpcPhysicalAccuracy(Model.Npc npc) => (int)(npc.Level * (33.6f - 0.16f * npc.Level) + 5f);

    // Java StatFunctions.getNpcLevelDiffMod: multiplier for dodge and damage when NPC > player level
    private static float NpcLevelDiffMod(int levelDiff) => levelDiff switch
    {
        3 => 0.1f, 4 => 0.2f, 5 => 0.3f, 6 => 0.4f,
        7 => 0.5f, 8 => 0.6f, 9 => 0.7f,
        _ => levelDiff > 9 ? 0.8f : 0f
    };

    private static async Task AwardLegionContributionAsync(Model.Player player, long apAmount,
        PlayerConnectionRegistry registry, ILegionDao legionDao, CancellationToken ct)
    {
        var legion = player.Legion;
        if (legion is null || apAmount <= 0) return;

        legion.ContributionPoints += apAmount;
        await legionDao.UpdateContributionPointsAsync(legion.LegionId, legion.ContributionPoints, ct);

        var update = new SM_LEGION_EDIT(legion.ContributionPoints);
        foreach (var member in legion.Members.Values)
        {
            var mConn = registry.Get(member.ObjectId);
            if (mConn is not null)
                try { await mConn.SendAsync(update, ct); } catch { }
        }
    }
}
