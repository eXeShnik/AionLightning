namespace AionLightning.Game.Model.House;

/// <summary>
/// Java model.gameobjects.player.PlayerScripts — an active house's fixed 8 decoration-script slots (the
/// Housing Panel script editor's slot count). Kept DB-free like the rest of HouseRegistry's model layer
/// (see its class doc): CM_HOUSE_SCRIPT persists via IHouseScriptsDao directly in the packet handler, and
/// HousingService.LoadHouseScriptsAsync populates this container from DB rows at house-load time via
/// <see cref="LoadPersisted"/>.
///
/// note: Java's dataholders.HouseScriptData (a catalog of predefined "LBox" script templates the client's
/// script editor can insert via createScript(scriptId, position, iconId)) is not ported. It has zero
/// callers anywhere in the Java source tree — nothing in AL 4.6 actually invokes createScript, so it's
/// dead template-catalog infrastructure, unrelated to the CM_HOUSE_SCRIPT wire flow this phase implements
/// (which always carries the client-authored script XML directly, never a template id).
/// </summary>
public sealed class PlayerScripts
{
    public const int SlotCount = 8;

    private readonly Dictionary<int, PlayerScript> _slots;

    public PlayerScripts()
    {
        _slots = new Dictionary<int, PlayerScript>(SlotCount);
        for (int position = 0; position < SlotCount; position++)
            _slots[position] = new PlayerScript();
    }

    public PlayerScript? Get(int position) => _slots.GetValueOrDefault(position);

    /// <summary>
    /// Java PlayerScripts.addScript(int, byte[], int) minus the DAO call. Decompresses
    /// <paramref name="compressedXml"/> (a zlib/Deflater stream — see <see cref="HouseScriptCompression"/>)
    /// to validate its declared <paramref name="uncompressedSize"/> and recover the plaintext for
    /// persistence, then stores the compressed bytes/size in the slot exactly as received — they are echoed
    /// back verbatim via SM_HOUSE_SCRIPTS, never recompressed server-side. A null
    /// <paramref name="compressedXml"/> (Java's "nothing to do" branch — reached when CM_HOUSE_SCRIPT's
    /// declared payload exceeded the 8150-byte wire cap and its body was never read off the socket) or a
    /// decompression/size-mismatch failure leaves the slot untouched and returns null; the caller must not
    /// persist anything in that case. <paramref name="hadPriorData"/> reports whether the slot already held
    /// data *before* this call, so the caller knows whether to INSERT or UPDATE (mirrors Java's own
    /// pre-check against the slot's previous compressedBytes).
    /// </summary>
    public string? TryApply(int position, byte[]? compressedXml, int uncompressedSize, out bool hadPriorData)
    {
        var script = Get(position);
        hadPriorData = script?.CompressedBytes is not null;
        if (script is null || compressedXml is null)
            return null;

        string content;
        int size;
        if (compressedXml.Length == 0)
        {
            content = string.Empty;
            size = 0;
        }
        else
        {
            if (!HouseScriptCompression.TryDecompress(compressedXml, out content))
                return null;
            if (System.Text.Encoding.Unicode.GetByteCount(content) != uncompressedSize)
                return null;
            size = uncompressedSize;
        }

        script.SetData(compressedXml, size);
        return content;
    }

    /// <summary>
    /// Java PlayerScripts(int) load-time population plus its String-overload of addScript, applied per row
    /// instead of in bulk — recompresses a persisted plaintext script back into wire-shaped compressed bytes
    /// (with the trailing NC-padding quirk, see <see cref="HouseScriptCompression.Compress"/>) so the slot
    /// is ready to be echoed back via SM_HOUSE_SCRIPTS without a further DB round-trip. Called by
    /// HousingService.LoadHouseScriptsAsync for every persisted (position, script) row.
    /// </summary>
    public void LoadPersisted(int position, string? scriptXml)
    {
        var script = Get(position);
        if (script is null) return;

        if (scriptXml is null)
            script.SetData(null, -1);
        else if (scriptXml.Length == 0)
            script.SetData([], 0);
        else
            script.SetData(HouseScriptCompression.Compress(scriptXml), scriptXml.Length * 2);
    }

    /// <summary>
    /// Java PlayerScripts.removeScript(int) minus the DAO call — clears a slot and reports whether it held
    /// data (so the caller knows a DB delete is needed). Unused by any CM handler currently, same as
    /// upstream Java (CM_HOUSE_SCRIPT never triggers a delete path either — see its class doc); kept for
    /// interface parity with the Java model.
    /// </summary>
    public bool Clear(int position)
    {
        var script = Get(position);
        if (script?.CompressedBytes is null) return false;
        script.SetData(null, -1);
        return true;
    }
}
