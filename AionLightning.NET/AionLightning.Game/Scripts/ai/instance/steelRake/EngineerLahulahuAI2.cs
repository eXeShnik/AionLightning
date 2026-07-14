// EngineerLahulahuAI2 — Java ai/instance/steelRake/EngineerLahulahuAI2.java. Steel Rake boss that
// registers a set of helper turret NPCs and randomly triggers one pair's skill on a cadence once
// engaged, with two HP-percentage-gated self buffs.
using AionLightning.Game.Ai;
using AionLightning.Game.Model;

namespace Ai;

[AiName("engineerlahulahu")]
public sealed class EngineerLahulahuAI2 : AggressiveNpcAI2
{
    private bool _isStart;
    private bool _isUsedSkill;
    private Npc? _npc, _npc1, _npc2, _npc3, _npc4, _npc5, _npc6, _npc7, _npc8, _npc9, _npc10, _npc11;

    private void RegisterNpcs()
    {
        _npc = GetNpc(281111);
        _npc1 = GetNpc(281325);
        _npc2 = GetNpc(281323);
        _npc3 = GetNpc(281322);
        _npc4 = GetNpc(281326);
        _npc5 = GetNpc(281113);
        _npc6 = GetNpc(281324);
        _npc7 = GetNpc(281109);
        _npc8 = GetNpc(281112);
        _npc9 = GetNpc(281114);
        _npc10 = GetNpc(281108);
        _npc11 = GetNpc(281110);
    }

    public override void OnAttack(Creature creature)
    {
        base.OnAttack(creature);
        CheckPercentage(Owner.HpPercentage);
    }

    private void CheckPercentage(int hpPercentage)
    {
        if (hpPercentage <= 95 && !_isStart)
        {
            RegisterNpcs();
            _isStart = true;
            UseSkill(18131);
            UseSkills();
        }
        if (hpPercentage <= 25 && !_isUsedSkill)
        {
            _isUsedSkill = true;
            Owner.RemoveEffectBySkillId(18131);
            UseSkill(18132);
        }
    }

    public override void OnBackHome()
    {
        _isStart = false;
        _isUsedSkill = false;
        base.OnBackHome();
    }

    private void UseSkills()
    {
        // note: Java gated this on getPosition().isSpawned() && !isAlreadyDead(); spawned-state isn't
        // exposed to scripts, so only the death/start guard is checked here.
        if (Owner.IsAlreadyDead || !_isStart) return;

        int rnd = System.Random.Shared.Next(1, 9);
        switch (rnd)
        {
            case 1:
                UseHelperSkill(_npc);
                UseHelperSkill(_npc1);
                break;
            case 2:
                UseHelperSkill(_npc2);
                UseHelperSkill(_npc3);
                break;
            case 3:
                UseHelperSkill(_npc4);
                UseHelperSkill(_npc5);
                break;
            case 4:
                UseHelperSkill(_npc6);
                UseHelperSkill(_npc7);
                break;
            case 5:
                UseHelperSkill(_npc8);
                break;
            case 6:
                UseHelperSkill(_npc9);
                break;
            case 7:
                UseHelperSkill(_npc10);
                break;
            case 8:
                UseHelperSkill(_npc11);
                break;
        }
        ScheduleTask(UseSkills, 10000);
    }

    private static void UseHelperSkill(Npc? npc)
    {
        // note: Java targeted the helper npc at itself and cast skill 18153 through its own AI2
        // instance (npc.getController().useSkill); commanding another NPC's skill cast isn't wired at
        // the script layer yet.
        _ = npc;
    }
}
