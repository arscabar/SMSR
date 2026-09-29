package smsr;
import java.util.*;

final class ValueState extends LinkedHashMap<String,Set<String>> {
    ValueState copy() { var result=new ValueState(); forEach((k,v)->result.put(k,new LinkedHashSet<>(v))); return result; }
    static ValueState merge(ValueState a,ValueState b,ValueBudget budget) {
        if(a==null) return b==null?null:b.copy();
        var result=a.copy();
        if(b!=null) b.forEach((k,v)-> { budget.tick(); result.computeIfAbsent(k,x->new LinkedHashSet<>()).addAll(v); });
        return result;
    }
}
