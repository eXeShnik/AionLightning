using AionLightning.Game.DataHolders;
using AionLightning.Game.Model;
using AionLightning.Game.Services;
using GameWorld = AionLightning.Game.World.World;

namespace AionLightning.Game.Ai;

/// <summary>
/// No-op base for every per-NPC AI script (Java <c>AbstractAI</c> + <c>AITemplate</c> +
/// <c>NpcAI2</c> collapsed into one class, mirroring how <see cref="Instance.GeneralInstanceHandler"/>
/// collapses the Java instance-handler hierarchy). Scripts under <c>Scripts/ai/**</c> carrying an
/// <see cref="AiNameAttribute"/> subclass this and override only the <c>OnXxx</c> hooks they need.
/// Compiled script classes are constructed by reflection with a parameterless constructor and
/// cannot inject DI services themselves, so the handful of helpers that need game services reach
/// them through one-time static injection (<see cref="InitServices"/>) — the same pattern
/// <see cref="Instance.GeneralInstanceHandler"/> and <c>QuestHandlerBase</c> use.
/// </summary>
public abstract class NpcAi2
{
    private static SpawnService? _spawnService;
    private static GameWorld? _world;
    private static IDataManager? _dataManager;

    /// <summary>Wires the shared services once at boot (called from the AI-engine host).</summary>
    public static void InitServices(SpawnService spawnService, GameWorld world, IDataManager dataManager)
    {
        _spawnService = spawnService;
        _world        = world;
        _dataManager  = dataManager;
    }

    /// <summary>The NPC this AI drives; set by <see cref="AiEngine.Create"/> right after construction.</summary>
    public Npc Owner { get; internal set; } = null!;

    protected Npc getOwner() => Owner;

    public AiState State { get; set; } = AiState.Created;
    public AiSubState SubState { get; set; } = AiSubState.None;

    private readonly List<CancellationTokenSource> _scheduledTaskTokens = new();

    // --- Lifecycle hooks (Java handleSpawned/handleRespawned/handleDespawned/handleDied) ---
    public virtual void OnSpawned() { }
    public virtual void OnDespawned() { }

    /// <summary>Java <c>handleDied</c>. Override cancels any scheduled tasks first — call
    /// <c>base.OnDied()</c> if you override this to keep that cleanup.</summary>
    public virtual void OnDied() => CancelTasks();

    // --- Combat hooks (Java handleAttack/handleAttackComplete/handleCreatureAggro) ---
    public virtual void OnAttack(Creature attacker) { }
    public virtual void OnAttackComplete() { }
    public virtual void OnCreatureAggro(Creature creature) { }

    /// <summary>Java <c>chooseAttackIntention</c>; default mirrors <c>AITemplate</c>'s SIMPLE_ATTACK.</summary>
    public virtual AttackIntention ChooseAttackIntention() => AttackIntention.SimpleAttack;

    // --- Dialog hooks (Java handleDialogStart/handleDialogFinish) ---
    public virtual void OnDialogStart(Player player) { }
    public virtual void OnDialogFinish(Player player) { }

    // --- Perception hooks (Java handleCreatureSee/handleCreatureMoved) ---
    public virtual void OnCreatureSee(Creature creature) { }
    public virtual void OnCreatureMoved(Creature creature) { }

    // --- Movement/target hooks (Java handleTargetReached/TooFar/Giveup, handleBackHome/NotAtHome, handleMoveArrived) ---
    public virtual void OnTargetReached() { }
    public virtual void OnTargetTooFar() { }
    public virtual void OnTargetGiveup() { }
    public virtual void OnBackHome() { }
    public virtual void OnNotAtHome() { }
    public virtual void OnMoveArrived() { }

    // --- Skill hooks ---
    /// <summary>Java-side NPCs fire an end-of-cast callback after an NPC skill resolves; NpcAiService
    /// currently owns NPC skill casting directly (see <see cref="NpcAiService"/>), so this hook is not
    /// yet invoked by the tick loop — it exists so scripts have somewhere to react once that wiring lands.</summary>
    public virtual void OnEndUseSkill(int skillId) { }

    // --- Think loop (Java AI2.think/canThink) ---
    public virtual void OnThink() { }

    /// <summary>Runs the AI's think hook. NpcAiService does not call this yet (see <see cref="OnEndUseSkill"/>
    /// note) — scripts may call it themselves from a scheduled task in the interim.</summary>
    protected void Think() => OnThink();

    /// <summary>Java <c>AIQuestion.SHOULD_REWARD</c> poll target: called when this NPC's death should
    /// grant XP/loot/AP to <paramref name="player"/>. Not yet invoked by NpcAiService (which uses
    /// <see cref="Model.Ai.AiNameRegistry.ShouldReward"/> instead) — kept for scripts that want to
    /// react to their own reward grant once that wiring lands.</summary>
    public virtual void OnReward(Player player) { }

    // --- Protected helpers scripts call ---

    /// <summary>Spawns an NPC in the same world/instance as this AI's owner (Java <c>AbstractAI.spawn</c>).</summary>
    protected Npc? Spawn(int npcId, float x, float y, float z, byte heading = 0)
    {
        var template = _dataManager?.Npcs.GetTemplate(npcId);
        if (template is null || _spawnService is null || Owner is null) return null;
        return _spawnService.SpawnNpcAt(template, new Position(x, y, z, heading, Owner.Position.WorldId, Owner.Position.InstanceId));
    }

    /// <summary>First NPC of the given id in the owner's world/instance scope, or null.</summary>
    protected Npc? GetNpc(int npcId)
    {
        if (_world is null || Owner is null) return null;
        return _world.GetNpcsInScope(Owner.Position).FirstOrDefault(n => n.Template.NpcId == npcId);
    }

    /// <summary>
    /// Schedules <paramref name="action"/> to run once after <paramref name="delayMs"/>, or
    /// repeatedly every <paramref name="periodMs"/> after that first run when non-zero. Cancelled by
    /// <see cref="CancelTasks"/> (fired from the default <see cref="OnDied"/>).
    /// </summary>
    protected void ScheduleTask(Action action, int delayMs, int periodMs = 0)
    {
        var cts = new CancellationTokenSource();
        lock (_scheduledTaskTokens) _scheduledTaskTokens.Add(cts);
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                action();

                if (periodMs > 0)
                {
                    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(periodMs));
                    while (await timer.WaitForNextTickAsync(token))
                        action();
                }
            }
            catch (OperationCanceledException) { /* cancelled via CancelTasks */ }
            finally
            {
                lock (_scheduledTaskTokens) _scheduledTaskTokens.Remove(cts);
            }
        }, token);
    }

    /// <summary>Cancels every task scheduled via <see cref="ScheduleTask"/> that hasn't fired yet.</summary>
    protected void CancelTasks()
    {
        lock (_scheduledTaskTokens)
        {
            foreach (var cts in _scheduledTaskTokens) cts.Cancel();
            _scheduledTaskTokens.Clear();
        }
    }

    /// <summary>note: NPC skill casting is still owned by <see cref="NpcAiService"/>'s tick loop; this
    /// is a placeholder so scripts compile against the eventual script-driven cast path.</summary>
    protected void UseSkill(int skillId, int skillLevel = 1) { }

    /// <summary>note: NPC shout broadcasting lives in <see cref="NpcAiService"/> today (NpcShoutData
    /// lookups); this is a placeholder for scripts that want to fire a shout directly.</summary>
    protected void SendMsg(int msgId) { }

    /// <summary>note: always true until leash/return-to-spawn state is exposed to scripts.</summary>
    protected bool IsHome() => true;
}
