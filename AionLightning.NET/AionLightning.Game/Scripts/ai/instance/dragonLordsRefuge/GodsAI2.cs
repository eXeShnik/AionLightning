// GodsAI2 — Java ai/instance/dragonLordsRefuge/GodsAI2.java. Empyrean-lord adds (Marchutan/
// Kaisinel forms) that debuff players and build aggro on Tiamat before/after their exhaustion phase.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("gods")]
// marchutan : 219491, 219492 // kaisinel : 219488, 219489
public sealed class GodsAI2 : AggressiveNpcAI2
{
    private Npc? _tiamat;

    // note: Java also overrode handleDeactivate as a no-op, modifyDamage to always return 6000, and
    // handleActivate (re-resolve _tiamat, target it, and cast an aggro skill immediately) — none of
    // activate/deactivate switching, damage-modification hooks, or targeting have a C# equivalent.

    public override void OnSpawned()
    {
        base.OnSpawned();
        _tiamat = GetNpc(219361);
        int npcId = getOwner().Template.NpcId;
        if (npcId == 219488 || npcId == 219491)
        {
            // note: Java scheduled a debuff-all-players cast at 8s (skill 20932/20936) and, at 12s, targeted
            // Tiamat, added 100000 aggro hate, shouted (1401550), and cast an aggro-building skill
            // (20931/20935). Aggro-list manipulation, cross-NPC targeting, and NPC shouts aren't exposed to
            // scripts yet.
            ScheduleTask(() => UseSkill(npcId == 219488 ? 20932 : 20936), 8000);
            ScheduleTask(() => UseSkill(npcId == 219488 ? 20931 : 20935), 12000);
        }
        else if (npcId == 219492 || npcId == 219489)
        {
            // note: Java shouted (1401538/1401539) immediately, then cast a finishing skill on Tiamat at 2s;
            // NPC shouts and cross-NPC skill targeting aren't exposed to scripts yet.
            ScheduleTask(() => UseSkill(npcId == 219489 ? 20929 : 20933), 2000);
        }
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(getOwner().HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        int npcId = getOwner().Template.NpcId;
        if (npcId != 219488 && npcId != 219491) return;
        // note: Java shouted at 50%/15%/<5% HP thresholds (1401548/1401549) via NpcShoutsService; NPC
        // shouts aren't exposed to scripts yet.
    }
}
