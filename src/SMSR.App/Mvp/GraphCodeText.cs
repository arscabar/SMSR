using System.Text;

namespace SMSR.App.Mvp;

internal static class GraphCodeText
{
    internal static string Decode(byte[] bytes)
    {
        try
        {
            var text = new UTF8Encoding(false, true).GetString(bytes);
            return text.StartsWith('\uFEFF') ? text[1..] : text;
        }
        catch (DecoderFallbackException)
        {
            // ponytail: Korean Windows legacy source; other encodings require an explicit policy.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var encoding = Encoding.GetEncoding(949, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            var text = encoding.GetString(bytes);
            if (!encoding.GetBytes(text).AsSpan().SequenceEqual(bytes))
                throw new DecoderFallbackException("코드는 UTF-8 또는 왕복 가능한 CP949여야 합니다.");
            return text;
        }
    }
}
