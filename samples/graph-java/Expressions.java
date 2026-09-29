package sample;

public class Expressions {
    public static int pick(int key,int first,int second) {
        return switch(key) {
            case 0,1 -> first;
            default -> { yield second; }
        };
    }
    public static int nested(int key,int first,int second) {
        return switch(key) {
            default -> {
                int value=switch(key) { default -> { yield first; } };
                value=second;
                yield value;
            }
        };
    }
    public static int relayed(int key,int first,int second) {
        return pick(key,first,second);
    }
    public static int erased(int key,int input) {
        int value=switch(key) { default -> input; };
        value=0;
        return value;
    }
}
