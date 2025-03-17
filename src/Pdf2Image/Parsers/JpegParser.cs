namespace Pdf2Image.Parsers;

internal static class JpegParser
{
    internal static List<byte[]> ParseBytes(byte[] data)
    {
        var images = new List<byte[]>();
        var marker = new byte[] { 0xFF, 0xD9 };

        var startIndex = 0;
        while (true)
        {
            var endIndex = ParserHelpers.IndexOf(data, marker, startIndex);
            if (endIndex < 0)
            {
                break;
            }

            var length = endIndex + marker.Length - startIndex;
            var chunk = new byte[length];
            Array.Copy(data, startIndex, chunk, 0, length);

            images.Add(chunk);

            startIndex = endIndex + marker.Length;
        }

        return images;
    }
}
