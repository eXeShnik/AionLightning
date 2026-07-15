namespace AionLightning.Game.Model;

/// <summary>
/// Port of Java <c>model.veteranrewards.VeteranRewards</c> — one pending row in the <c>veteran_rewards</c>
/// admin-queued reward-mail table. This is not account-age login rewards: an admin inserts a row naming a
/// recipient and an item/kinah/message payload, and <c>Services/VeteranRewardService.cs</c>'s minute cron
/// drains the table, sending each row as system mail and then deleting it.
/// </summary>
public sealed class VeteranReward(
    int id, string playerName, int type, int itemId, int count, int kinah, string sender, string title, string message)
{
    public int Id { get; } = id;
    public string PlayerName { get; } = playerName;

    /// <summary>Java's mail-type flag: 0 = Normal, 1 = Express, 2 = BlackCloud (matches <see cref="Mail.LetterType"/>).</summary>
    public int Type { get; } = type;
    public int ItemId { get; } = itemId;
    public int Count { get; } = count;
    public int Kinah { get; } = kinah;
    public string Sender { get; } = sender;
    public string Title { get; } = title;
    public string Message { get; } = message;
}
