using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

/// <summary>
/// Java services.WeddingService — propose/accept marriage between two online players, persisting the
/// partner relationship (Player.partnerId) and clearing it again on divorce. Java's flow was
/// registerOffer(priest-driven, chat-command engine) + acceptWedding(WeddingCommand text command); no
/// chat-command engine or priest-NPC dialog is ported here (see migration_plan.md), so this port's
/// propose/accept trigger is the wedding-ring item (Java quest 1162's reward, itemId 182200563) used on
/// a targeted player — see CM_USE_ITEM's wedding-ring branch — with accept/decline via the existing
/// yes/no dialog round-trip (<see cref="PlayerResponseRegistry"/> + SM_QUESTION_WINDOW/CM_QUESTION_RESPONSE,
/// the same mechanism <see cref="DuelService"/>'s duel request uses).
/// note: Java's suit-requirement check (WEDDINGS_SUIT_ENABLE) and gift-item grant (WEDDINGS_GIFT_ENABLE)
/// are not ported — both default off in Java's own shipped config, and neither is required for the core
/// propose/marry/divorce + partner-state flow this task targets.
/// </summary>
public sealed class WeddingService
{
    /// <summary>Java quest 1162's wedding-ring reward item id — reused here as the propose trigger
    /// (see this class's doc comment).</summary>
    public const int RingItemId = 182200563;

    private const int KinahItemId = 182400001;

    /// <summary>SM_QUESTION_WINDOW question code for a marriage proposal.
    /// TODO: verify against a live 4.6 client capture — Java's own wedding flow never used a
    /// question-window dialog (chat-command based instead), so there is no canonical code to port;
    /// this reuses the duel-request code's neighborhood as a placeholder.</summary>
    private const int ProposalQuestionCode = 50037;

    private readonly IPlayerDao _playerDao;
    private readonly IItemDao _itemDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly PlayerResponseRegistry _responseRegistry;
    private readonly IOptions<WeddingOptions> _options;

    public WeddingService(IPlayerDao playerDao, IItemDao itemDao, PlayerConnectionRegistry connRegistry,
        PlayerResponseRegistry responseRegistry, IOptions<WeddingOptions> options)
    {
        _playerDao = playerDao;
        _itemDao = itemDao;
        _connRegistry = connRegistry;
        _responseRegistry = responseRegistry;
        _options = options;
    }

    /// <summary>Java WeddingService.registerOffer + acceptWedding — proposes marriage to
    /// <paramref name="target"/>, waits for their yes/no answer, and marries them on acceptance.
    /// Returns true only if the marriage actually completed.</summary>
    public async Task<bool> ProposeAsync(Player proposer, Player target, CancellationToken ct = default)
    {
        if (!_options.Value.Enable) return false;
        if (proposer.ObjectId == target.ObjectId) return false;

        if (!CanRegister(proposer, target)) return false;

        var targetConn = _connRegistry.Get(target.ObjectId);
        var proposerConn = _connRegistry.Get(proposer.ObjectId);
        if (targetConn is null) return false;

        var tcs = _responseRegistry.RegisterPending(target.ObjectId);
        try
        {
            await targetConn.SendAsync(new SM_QUESTION_WINDOW(ProposalQuestionCode, proposer.ObjectId, 0, proposer.Name), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return false;
        }

        bool accepted;
        try
        {
            accepted = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(_options.Value.ProposalTimeoutSeconds), ct);
        }
        catch
        {
            _responseRegistry.CancelPending(target.ObjectId);
            return false;
        }

        if (!accepted)
        {
            if (proposerConn is not null)
                try { await proposerConn.SendAsync(new SM_MESSAGE(proposer, $"{target.Name} declined your proposal.", SM_MESSAGE.ChatType.Normal), ct); } catch { }
            return false;
        }

        // Re-check: either side may have married/logged out while the dialog was pending.
        if (!CanRegister(proposer, target)) return false;

        return await MarryAsync(proposer, target, ct);
    }

    /// <summary>Java WeddingService.canRegister — neither side is already married (or has a pending offer,
    /// implicitly enforced here by PlayerResponseRegistry only allowing one pending answer per target) —
    /// plus this port's same-sex/same-race policy gates (Java WEDDINGS_SAME_SEX/WEDDINGS_DIFF_RACES).</summary>
    private bool CanRegister(Player a, Player b)
    {
        if (a.IsMarried || b.IsMarried) return false;
        if (!_options.Value.AllowSameSex && a.Gender == b.Gender) return false;
        if (!_options.Value.AllowDifferentRaces && a.Race != b.Race) return false;
        return true;
    }

    /// <summary>Java WeddingService.doWedding — sets/persists both partners' partnerId, deducts the
    /// configured kinah cost (if any), and announces the marriage (if configured).
    /// note: Java's couple teleport/summon skill grant is not ported — see this class's doc comment;
    /// the partner relationship itself is fully functional (queryable via <see cref="Player.PartnerId"/>).</summary>
    public async Task<bool> MarryAsync(Player a, Player b, CancellationToken ct = default)
    {
        if (a.IsMarried || b.IsMarried) return false;

        if (_options.Value.KinahCost > 0 && !await TryPayAsync(a, ct))
            return false;
        if (_options.Value.KinahCost > 0 && !await TryPayAsync(b, ct))
        {
            await RefundAsync(a, ct);
            return false;
        }

        a.PartnerId = b.ObjectId;
        b.PartnerId = a.ObjectId;
        await _playerDao.UpdatePartnerIdAsync(a.ObjectId, b.ObjectId, ct);
        await _playerDao.UpdatePartnerIdAsync(b.ObjectId, a.ObjectId, ct);

        await SendIfOnlineAsync(a.ObjectId, new SM_MESSAGE(a, $"You have married {b.Name}.", SM_MESSAGE.ChatType.Normal), ct);
        await SendIfOnlineAsync(b.ObjectId, new SM_MESSAGE(b, $"You have married {a.Name}.", SM_MESSAGE.ChatType.Normal), ct);

        if (_options.Value.Announce)
        {
            var announcement = new SM_MESSAGE(a, $"{a.Name} and {b.Name} are now married.", SM_MESSAGE.ChatType.Command);
            foreach (var conn in _connRegistry.GetAll())
                try { await conn.SendAsync(announcement, ct); } catch { }
        }

        return true;
    }

    /// <summary>Java WeddingService.unDoWedding — clears both partners' partnerId. Works even if the
    /// partner is currently offline (their in-memory state, if any, is updated too).</summary>
    public async Task<bool> DivorceAsync(Player player, CancellationToken ct = default)
    {
        if (!player.IsMarried) return false;
        int partnerId = player.PartnerId;

        player.PartnerId = 0;
        await _playerDao.UpdatePartnerIdAsync(player.ObjectId, 0, ct);
        await _playerDao.UpdatePartnerIdAsync(partnerId, 0, ct);

        if (_connRegistry.Get(partnerId)?.ActivePlayer is { } partner)
            partner.PartnerId = 0;

        await SendIfOnlineAsync(player.ObjectId, new SM_MESSAGE(player, "Wedding canceled.", SM_MESSAGE.ChatType.Normal), ct);
        await SendIfOnlineAsync(partnerId, new SM_MESSAGE(player, "Wedding canceled.", SM_MESSAGE.ChatType.Normal), ct);

        return true;
    }

    private async Task SendIfOnlineAsync(int objectId, SM_MESSAGE packet, CancellationToken ct)
    {
        if (_connRegistry.Get(objectId) is { } conn)
            try { await conn.SendAsync(packet, ct); } catch { }
    }

    private async Task<bool> TryPayAsync(Player player, CancellationToken ct)
    {
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if ((kinahItem?.Count ?? 0) < _options.Value.KinahCost) return false;

        kinahItem!.Count -= _options.Value.KinahCost;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
        return true;
    }

    private async Task RefundAsync(Player player, CancellationToken ct)
    {
        var kinahItem = player.Inventory.FindByItemId(KinahItemId);
        if (kinahItem is null) return;
        kinahItem.Count += _options.Value.KinahCost;
        await _itemDao.SaveAllAsync(player.ObjectId, player.Inventory.All, ct);
    }
}
