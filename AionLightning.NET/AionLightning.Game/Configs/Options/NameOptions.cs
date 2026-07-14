namespace AionLightning.Game.Configs.Options;

/// <summary>
/// Java configs.main.NameConfig — character-name validation rules consumed by
/// <see cref="Services.RenameService"/> (Java services.NameRestrictionService).
/// </summary>
public sealed record NameOptions
{
    /// <summary>Java gameserver.name.characterpattern default — plain ASCII letters/digits, 2-16 chars.
    /// (Java's own default additionally allowed a Hangul Unicode block; this port keeps the ASCII-only
    /// baseline and lets an operator widen it via config if their client needs it.)</summary>
    public string CharacterPattern { get; init; } = "^[a-zA-Z0-9]{2,16}$";

    /// <summary>Java gameserver.name.forbidden.sequences — comma-separated substrings blocked from
    /// appearing anywhere in a name (case-insensitive). Empty by default; server operators configure
    /// their own list (Java shipped a large hardcoded list of competitor-brand names not worth porting
    /// verbatim into source).</summary>
    public string[] ForbiddenSequences { get; init; } = [];
}
