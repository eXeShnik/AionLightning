namespace AionLightning.Game.Model.Quest;

// Numeric values are load-bearing: persisted to player_quests.status AND written to the
// wire in SM_QUEST_ACTION — the 4.6 client validates them (Java QuestStatus parity).
public enum QuestStatus : byte
{
    NONE     = 0,
    START    = 3,
    REWARD   = 4,
    COMPLETE = 5,
    LOCKED   = 6,
}

public sealed class QuestEntry
{
    public int         QuestId       { get; init;  }
    public QuestStatus Status        { get; set;   } = QuestStatus.START;
    public int         CompleteCount { get; set;   } = 0;

    private readonly int[] _vars = new int[6]; // kill-progress per seq slot, 0-63 each

    public int GetVar(int idx) => idx is >= 0 and < 6 ? _vars[idx] : 0;
    public void SetVar(int idx, int val)
    {
        if (idx is >= 0 and < 6)
            _vars[idx] = Math.Clamp(val, 0, 63);
    }

    // Packed vars stored in `step` DB column — matches Java QuestVars encoding.
    // vars[i] contributes vars[i] * 64^i to the packed integer.
    public int Step
    {
        get
        {
            int packed = 0, mult = 1;
            for (int i = 0; i < 6; i++) { packed += _vars[i] * mult; mult *= 64; }
            return packed;
        }
        set
        {
            int rem = value;
            for (int i = 0; i < 6; i++) { _vars[i] = rem % 64; rem /= 64; }
        }
    }
}
