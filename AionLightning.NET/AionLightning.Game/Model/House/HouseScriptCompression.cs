using System.IO.Compression;
using System.Text;

namespace AionLightning.Game.Model.House;

/// <summary>
/// Java utils.xml.CompressUtil, scoped down to house-script use (its only caller in this port). Java's
/// Deflater/Inflater defaults produce/consume a zlib stream (RFC 1950: 2-byte header + deflate data +
/// Adler-32 trailer) over UTF-16LE text — <see cref="ZLibStream"/> is the exact .NET analog, so no manual
/// header/trailer handling is needed. Trailing bytes after the logical end of the zlib stream (e.g. the
/// NC-padding <see cref="Compress"/> appends, or whatever the client appended before sending a slot back)
/// are tolerated exactly like Java's Inflater tolerates them — both stop reading once the deflate stream's
/// own end-of-data marker is hit, regardless of how many extra bytes follow in the buffer.
/// </summary>
internal static class HouseScriptCompression
{
    public static bool TryDecompress(byte[] compressed, out string text)
    {
        try
        {
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            text = Encoding.Unicode.GetString(output.ToArray());
            return true;
        }
        catch (InvalidDataException)
        {
            text = string.Empty;
            return false;
        }
    }

    /// <summary>
    /// Java PlayerScripts.addScript(int, String)'s compression path — zlib-compresses the text, then
    /// appends 8 trailing 0xCD bytes (Java's own comment: "Add NC shit bytes, without which fails to load
    /// :)"). Kept verbatim as a documented, deliberate client-compatibility quirk rather than an accident.
    /// </summary>
    public static byte[] Compress(string text)
    {
        byte[] plain = Encoding.Unicode.GetBytes(text);

        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
            zlib.Write(plain);

        byte[] compressed = output.ToArray();
        byte[] padded = new byte[compressed.Length + 8];
        compressed.CopyTo(padded, 0);
        for (int i = compressed.Length; i < padded.Length; i++)
            padded[i] = 0xCD;
        return padded;
    }
}
