namespace Pdf2Image.Parsers;

internal static class PngParser
{
    internal static List<byte[]> ParseBytes(byte[] data)
    {
        var images = new List<byte[]>();
        var iend = "IEND"u8.ToArray();
        int c1 = 0, c2 = 0;
        while (c2 < data.Length)
        {
            var idx = ParserHelpers.IndexOf(data, iend, c2);
            if (idx < 0)
            {
                break;
            }

            var endOfChunk = idx + iend.Length + 4;
            if (endOfChunk > data.Length)
            {
                endOfChunk = data.Length;
            }

            var length = endOfChunk - c1;
            var chunk = new byte[length];
            Array.Copy(data, c1, chunk, 0, length);

            images.Add(chunk);

            c2 = endOfChunk;
            c1 = endOfChunk;
        }

        return images;
    }
}
