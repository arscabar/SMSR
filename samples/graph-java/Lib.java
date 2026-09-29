package sample;

public class Lib {
    public static int pick(int value) { return value; }
    public static String pick(String value) { return value; }
    public static <T> T identity(T value) { return value; }
    public String format(int value) { return String.valueOf(value); }
    public Lib() { }
    public static int overwrite(int value) { value = 0; return value; }
    public static int relayed(int value) { return identity(value); }
    public static int erased(int value) { return overwrite(value); }
    public static int choose(int value, boolean enabled) {
        int result = 0;
        if (enabled) result = value;
        return result;
    }
    public static int decide(boolean enabled, int value, int fallback) {
        if (enabled) return value;
        return fallback;
    }
    public static int selected(boolean first, boolean second, int value, int fallback) {
        return first && second ? value : fallback;
    }
    public static void endless() { while (true) { } }
    public static int counted(int value) {
        int result = 0;
        for (int i = 0; i < 1; result = value) {
            i++;
            continue;
        }
        return result;
    }
}
