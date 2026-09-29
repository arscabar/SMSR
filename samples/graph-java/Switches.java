package sample;

public class Switches {
    public static int pick(int key,int first,int second) {
        switch(key) {
            case 0: return first;
            default: return second;
        }
    }
    public static int fall(int key,int input) {
        int value=0;
        switch(key) {
            case 0: value=input;
            default: value=0;
        }
        return value;
    }
    public static int rule(int key,int first,int second) {
        int value=0;
        switch(key) {
            case 0,1 -> value=first;
            default -> value=second;
        }
        return value;
    }
    public static int relayed(int key,int first,int second) {
        return pick(key,first,second);
    }
    public static int erased(int key,int input) {
        return fall(key,input);
    }
}
