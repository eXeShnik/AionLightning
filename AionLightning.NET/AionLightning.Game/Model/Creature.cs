namespace AionLightning.Game.Model;

public abstract class Creature : VisibleObject
{
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int MaxMp { get; set; }
    public int CurrentMp { get; set; }

    public CreatureState State { get; set; } = CreatureState.Active;
    public VisibleObject? Target { get; set; }

    public int StateValue => (int)State;

    public int HpPercentage => MaxHp > 0 ? (CurrentHp * 100) / MaxHp : 0;
    public bool IsAlreadyDead => CurrentHp <= 0;

    // Tracks when this creature last entered combat — used by RegenService to suppress out-of-combat regen
    public DateTime LastCombatTime { get; set; } = DateTime.MinValue;

    // Tracks the last auto-attack time for cooldown enforcement
    public DateTime LastAttackTime { get; set; } = DateTime.MinValue;

    public float MovementSpeed    { get; protected set; } = 6.0f;
    public int CurrentAttackSpeed { get; set; } = 1500;

    // Active buff/debuff effects — thread-safe via _effectsLock
    private readonly object              _effectsLock   = new();
    private readonly List<AbnormalState> _activeEffects = new();

    public void AddEffect(AbnormalState state)
    {
        lock (_effectsLock)
        {
            _activeEffects.RemoveAll(e => e.SkillId == state.SkillId);
            _activeEffects.Add(state);
        }
    }

    public void RemoveEffect(int skillId, DateTime expiry)
    {
        lock (_effectsLock)
            _activeEffects.RemoveAll(e => e.SkillId == skillId && e.Expiry == expiry);
    }

    public List<AbnormalState> GetActiveEffects()
    {
        lock (_effectsLock)
            return _activeEffects.Where(e => !e.IsExpired).ToList();
    }
}
