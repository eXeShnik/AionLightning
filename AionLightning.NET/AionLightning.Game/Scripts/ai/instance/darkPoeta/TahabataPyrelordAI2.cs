// TahabataPyrelordAI2 — Java ai/instance/darkPoeta/TahabataPyrelordAI2.java. Dragon boss with a
// chained HP-gated skill-tree rotation and a "petrify" finisher sequence below 30% HP.
using System.Collections.Generic;
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("tahabatapyrelord")]
public sealed class TahabataPyrelordAI2 : AggressiveNpcAI2
{
    private readonly List<int> _percents = new();
    private bool _lock1;
    private bool _lock2;

    public override void OnSpawned()
    {
        AddPercent();
        _lock2 = false;
        base.OnSpawned();
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        foreach (var percent in _percents.ToArray())
        {
            if (hpPercentage <= percent)
            {
                switch (percent)
                {
                    case 100:
                        UseFirstSkillTree();
                        break;
                    case 80:
                    case 60:
                        CancelTasks();
                        FirstSkill();
                        break;
                    case 30:
                        CancelTasks();
                        _lock1 = false;
                        UseLastSkillTree();
                        break;
                }
                _percents.Remove(percent);
                break;
            }
        }
    }

    private void UseFirstSkillTree() => ScheduleTask(FirstSkill, 20000);

    // note: every UseSkill call below dropped Java's EmoteManager.emoteStopAttacking(getOwner())
    // companion call; emote broadcasting isn't wired at the script layer yet.
    private void FirstSkill()
    {
        if (_lock2) return;
        int hpPercent = Owner.HpPercentage;
        if (hpPercent > 80)
        {
            UseSkill(18217); // Strike Down with Anger
        }
        else if (hpPercent > 60)
        {
            UseSkill(18232); // Explosion of Wrath
        }
        else if (hpPercent > 30)
        {
            UseSkill(18236); // Eruption of Power
            Sp(281258); // Faithful Subordinate (stone gargoyle)
        }
        SkillTwo();
    }

    private void SkillTwo()
    {
        if (_lock2) return;
        ScheduleTask(() =>
        {
            int hpPercent = Owner.HpPercentage;
            if (hpPercent > 80)
                UseSkill(18229); // Dragon's Fireball
            else if (hpPercent > 60)
                UseSkill(18225); // Dragon Flame
            else if (hpPercent > 30)
                UseSkill(18232); // Explosion of Wrath
            SkillThree();
        }, 5000);
    }

    private void SkillThree()
    {
        if (_lock2) return;
        ScheduleTask(() =>
        {
            int hpPercent = Owner.HpPercentage;
            if (hpPercent > 80 || (hpPercent <= 60 && hpPercent > 30))
                UseSkill(18225); // Dragon Flame
            else if (hpPercent <= 80 && hpPercent > 60)
                UseSkill(18231); // Mighty Thrust
            ScheduleTask(FirstSkill, 31000);
        }, 4000);
    }

    private void UseLastSkillTree()
    {
        if (_lock2) return;
        // note: Java also called getOwner().clearAttackedCount() here; not exposed to scripts.
        UseSkill(18239); // Soul Petrify
        ScheduleTask(() =>
        {
            UseSkill(18243); // Destroy Frozen Soul
            ScheduleTask(() =>
            {
                UseSkill(18243); // Destroy Frozen Soul
                LastSkillTree2();
            }, 3000);
        }, 4000);
    }

    private void LastSkillTree2()
    {
        if (_lock2) return;
        ScheduleTask(() =>
        {
            UseSkill(18232); // Explosion of Wrath
            if (!_lock1)
            {
                _lock1 = true;
                LastSkillTree2();
            }
        }, 10000);
        if (_lock1)
        {
            ScheduleTask(() =>
            {
                UseSkill(18241); // Powerful Flame
                Sp(281259); // Faithful Subordinate (Dragon)
                ScheduleTask(() =>
                {
                    _lock1 = false;
                    UseLastSkillTree(); // new skilltree repeat
                }, 27000);
            }, 13000);
        }
    }

    private void Sp(int npcId)
    {
        if (_lock2) return;
        if (npcId == 281258)
        {
            Spawn(npcId, 1191.2714f, 1220.5795f, 144.2901f, 36);
            Spawn(npcId, 1188.3695f, 1257.1322f, 139.66028f, 80);
            Spawn(npcId, 1177.1423f, 1253.9136f, 140.58705f, 97);
            Spawn(npcId, 1163.5889f, 1231.9149f, 145.40042f, 118);
        }
        else
        {
            Spawn(npcId, 1182.0021f, 1244.0125f, 142.67587f, 88);
            Spawn(npcId, 1192.3885f, 1236.5231f, 142.50638f, 68);
            Spawn(npcId, 1185.647f, 1227.2747f, 144.2261f, 32);
            Spawn(npcId, 1172.3302f, 1232.5709f, 144.70761f, 12);
        }
    }

    private void AddPercent()
    {
        _percents.Clear();
        _percents.AddRange(new[] { 100, 80, 60, 30 });
    }

    private void Despawn(int npcId)
    {
        // note: Java despawned every instance npc matching npcId (WorldMapInstance.getNpcs + controller
        // onDelete); enumerating/despawning other npcs by id isn't wired at the script layer yet.
        _ = npcId;
    }

    public override void OnBackHome()
    {
        AddPercent();
        CancelTasks();
        _lock2 = false;
        Despawn(281258);
        Despawn(281259);
        base.OnBackHome();
    }

    public override void OnDespawned()
    {
        _percents.Clear();
        CancelTasks();
        Despawn(281258);
        Despawn(281259);
        _lock2 = true;
        base.OnDespawned();
    }

    public override void OnDied()
    {
        _percents.Clear();
        Despawn(281258);
        Despawn(281259);
        _lock2 = true;
        base.OnDied(); // cancels any scheduled skill-tree tasks
    }
}
