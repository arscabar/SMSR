using Alias = SmsrGraphSample.Converter;

namespace SmsrGraphSample;

public static class Demo
{
    public static string Run() => Alias.Format(42);
    public static string Echo() => Alias.Format("example");
    public static int Connect(int input) => Alias.Choose(right: 3, left: input);

    public static int Flow(bool choose, int input)
    {
        int value;
        if (choose) value = input;
        else value = 0;
        while (value < 3) value++;
        return value;
    }

    public static void Publish(int input)
    {
        var value = Alias.Choose(input);
        System.Console.WriteLine(value);
    }
}
