namespace Pdf2Image.Parsers;

internal static class ParserHelpers
{
    internal static int IndexOf(byte[] data, byte[] marker, int startIndex)
    {
        for (var i = startIndex; i <= data.Length - marker.Length; i++)
        {
            var found = !marker.Where((t, j) => data[i + j] != t).Any();
            if (found) return i;
        }

        return -1;
    }
}
