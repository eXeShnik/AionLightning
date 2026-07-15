using AionLightning.Commons.Services;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Model.Mail;
using AionLightning.Game.Services.Mail;
using Microsoft.Extensions.Logging;

namespace AionLightning.Game.Services;

/// <summary>
/// Port of Java <c>services.veteranreward.VeteranRewardsService</c>. Despite the name, this is not
/// account-age login rewards: it's a DB-queued admin reward-mail dispatcher — an admin inserts a row into
/// <c>veteran_rewards</c> naming a recipient and an item/kinah/message payload, and this service's minute
/// cron sweep (Java's <c>"0 * * ? * *"</c>) delivers each pending row as system mail via
/// <see cref="Mail.SystemMailService"/>, then deletes the row. Java deleted every processed row
/// unconditionally — even when the player name failed to resolve (see <c>SendVeteranRewardMail</c>'s
/// early-return branches, which still fall through to <c>RecycleVeteranReward</c> in the caller) — so a
/// bad row is drained rather than retried forever; this port matches that (logs the failure, deletes
/// anyway). Java's stray <c>if (id > 1)</c> guard that skipped deleting the very first queued row is not
/// reproduced here: nothing in the surrounding code explains it, and an admin-queue table is expected to
/// always drain to empty.
/// </summary>
public sealed class VeteranRewardService(
    IVeteranRewardDao dao,
    IPlayerDao playerDao,
    SystemMailService systemMailService,
    CronService cronService,
    ILogger<VeteranRewardService> log)
{
    /// <summary>Java <c>VeteranRewardsService.VETERAN_REWARDS_LOOP_STATUS_BROADCAST_SCHEDULE</c> — every
    /// minute, on the minute.</summary>
    public const string RewardCron = "0 * * ? * *";

    /// <summary>Arms the minute sweep. Called once at startup by <see cref="VeteranRewardServiceHostedService"/>.</summary>
    public async Task ScheduleAsync(CancellationToken ct = default)
    {
        await cronService.Schedule(() => FireAndForget(ProcessPendingAsync(default), "veteran reward dispatch"), RewardCron, longRunningTask: true);
        log.LogInformation("VeteranRewardService: cron armed ({Cron}).", RewardCron);
    }

    /// <summary>Java <c>Init_VeteranRewards()</c> + <c>StartVeteranReward()</c> combined into a single
    /// sweep. No-ops gracefully when the table is empty (fresh DB).</summary>
    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var pending = await dao.LoadPendingAsync(ct);
        if (pending.Count == 0)
            return;

        log.LogInformation("VeteranRewardService: processing {Count} pending reward(s).", pending.Count);
        foreach (var reward in pending)
        {
            await DeliverAsync(reward, ct);
            await dao.DeleteAsync(reward.Id, ct);
        }
    }

    /// <summary>Java <c>VerifyVeteranReward</c> + <c>SendVeteranRewardMail</c>'s recipient-resolution and
    /// item/count normalization, delegated to the shared <see cref="Mail.SystemMailService"/> pipeline for
    /// the actual mail persistence/notify (Java hand-rolled its own Item/Letter/Mailbox plumbing inline).</summary>
    private async Task DeliverAsync(VeteranReward reward, CancellationToken ct)
    {
        var recipient = await playerDao.FindByNameAsync(reward.PlayerName, ct);
        if (recipient is null)
        {
            log.LogWarning("VeteranRewardService: recipient '{Player}' does not exist (reward id {Id}) — dropping.",
                reward.PlayerName, reward.Id);
            return;
        }

        var letterType = reward.Type switch
        {
            1 => LetterType.Express,
            2 => LetterType.BlackCloud,
            _ => LetterType.Normal,
        };

        // Java's item<=0 -> 0 / count<=0 -> -1 normalization doesn't map cleanly onto SystemMailService's
        // API (which has no "-1 = uncounted" sentinel); a missing/invalid item just means no item attached.
        int itemId = reward.ItemId > 0 ? reward.ItemId : 0;
        long itemCount = itemId > 0 ? Math.Max(reward.Count, 1) : 0;

        bool sent = await systemMailService.SendSystemMailAsync(
            recipient.ObjectId,
            reward.Sender,
            reward.Title,
            reward.Message,
            attachedKinah: reward.Kinah,
            attachedItemId: itemId,
            attachedItemCount: itemCount,
            letterType: letterType,
            ct: ct);

        if (!sent)
            log.LogWarning("VeteranRewardService: mail delivery failed for '{Player}' (reward id {Id}).", reward.PlayerName, reward.Id);
    }

    /// <summary>Fire-and-forget helper for <see cref="CronService.Schedule"/> (which only accepts a
    /// synchronous <see cref="Action"/>). Faults are logged rather than silently dropped.</summary>
    private void FireAndForget(Task task, string description) =>
        _ = task.ContinueWith(t => log.LogError(t.Exception, "Unhandled exception in {Description}", description),
            TaskContinuationOptions.OnlyOnFaulted);
}
