using System.Text.RegularExpressions;
using AionLightning.Game.Configs.Options;
using AionLightning.Game.Dao;
using AionLightning.Game.Model;
using AionLightning.Game.Network.Aion;
using AionLightning.Game.Network.Aion.ServerPackets;
using Microsoft.Extensions.Options;

namespace AionLightning.Game.Services;

public enum RenameResult
{
    Success,
    InvalidFormat,
    Forbidden,
    Unchanged,
    Taken,
}

/// <summary>
/// Java services.RenameService (renamePlayer) + services.NameRestrictionService — validates and applies
/// a character rename, persists it, and broadcasts SM_RENAME to every online player (Java iterated
/// <c>World.getPlayersIterator()</c> unconditionally, including the renamed player themselves).
/// note: Java also inserted an OldNamesDAO row (name-reservation coupon tracking) gated behind
/// CustomConfig.OLD_NAMES_COUPON_DISABLED — no OldNamesDAO/old-name-reuse-block subsystem is ported, so
/// this port allows immediate reuse of a vacated name once persisted (uniqueness is still enforced via
/// <see cref="IPlayerDao.ExistsByNameAsync"/> against currently-assigned names).
/// </summary>
public sealed class RenameService
{
    /// <summary>Java RenameService.renamePlayer's player-rename coupon item ids (distinct from the
    /// 169680000/169680001 legion-rename coupon already wired in CM_APPEARANCE's type=1 branch).</summary>
    public const int RenameCouponItemId1 = 169670000;
    public const int RenameCouponItemId2 = 169670001;

    private readonly IPlayerDao _playerDao;
    private readonly PlayerConnectionRegistry _connRegistry;
    private readonly IOptions<NameOptions> _options;

    public RenameService(IPlayerDao playerDao, PlayerConnectionRegistry connRegistry, IOptions<NameOptions> options)
    {
        _playerDao = playerDao;
        _connRegistry = connRegistry;
        _options = options;
    }

    /// <summary>Java NameRestrictionService.isValidName — format check against the configured pattern.</summary>
    public bool IsValidNameFormat(string name) => Regex.IsMatch(name, _options.Value.CharacterPattern);

    /// <summary>Java NameRestrictionService.isForbiddenWord — case-insensitive substring match against
    /// the configured forbidden-sequence list.</summary>
    public bool IsForbiddenWord(string name)
    {
        foreach (var sequence in _options.Value.ForbiddenSequences)
            if (!string.IsNullOrEmpty(sequence) && name.Contains(sequence, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>Java RenameService.renamePlayer — validates format/forbidden-word/uniqueness/unchanged,
    /// then persists the new name and broadcasts SM_RENAME to every online player. Item consumption is the
    /// caller's responsibility (see CM_APPEARANCE), matching Java's own separation (the coupon's item id
    /// is checked by the caller before invoking this).</summary>
    public async Task<RenameResult> RenameAsync(Player player, string newName, CancellationToken ct = default)
    {
        if (!IsValidNameFormat(newName)) return RenameResult.InvalidFormat;
        if (IsForbiddenWord(newName)) return RenameResult.Forbidden;
        if (string.Equals(player.Name, newName, StringComparison.Ordinal)) return RenameResult.Unchanged;
        if (await _playerDao.ExistsByNameAsync(newName, ct)) return RenameResult.Taken;

        string oldName = player.Name;
        player.Name = newName;
        await _playerDao.UpdateNameAsync(player.ObjectId, newName, ct);

        var renamePacket = new SM_RENAME(player.ObjectId, oldName, newName);
        foreach (var conn in _connRegistry.GetAll())
            try { await conn.SendAsync(renamePacket, ct); } catch { }

        return RenameResult.Success;
    }
}
