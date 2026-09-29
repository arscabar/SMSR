package sample;
import static sample.Lib.pick;

public class Entry {
    public String run(int input) {
        int number = pick(input);
        String text = pick("example");
        String formatted = new Lib().format(number);
        return Lib.identity(text + formatted);
    }
}
