namespace SmsrGraphSample;

public static class Converter
{
    public static string Format(int value) => value.ToString();
    public static string Format(string value) => value.Trim();

    public static int Choose(int left, int right = 7)
    {
        if (left > 0) return left;
        return right;
    }
}
