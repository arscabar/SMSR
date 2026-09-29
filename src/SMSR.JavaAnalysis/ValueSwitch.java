package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueSwitch {
    static ValueState read(ValueStatements s,SwitchTree n,ValueState state,List<String> controls,ValueLoops.Frame outer) {
        var g=s.g;var nested=new ArrayList<>(controls);
        nested.add(s.expr.read(n.getExpression(),state,controls));
        boolean hasDefault=n.getCases().stream().anyMatch(c->c.getExpressions().isEmpty());
        ValueState exit=hasDefault?null:state.copy(),fall=null;
        var frame=new ValueLoops.Frame();
        // ponytail: path-insensitive MAY joins; add path predicates only with a solver.
        for(var c:n.getCases()) {
            g.budget.tick();var branch=ValueState.merge(state,fall,g.budget);
            if(c.getCaseKind()==CaseTree.CaseKind.RULE) {
                if(!(c.getBody() instanceof StatementTree)) g.reject("SWITCH_RULE_BODY");
                branch=s.read((StatementTree)c.getBody(),branch,nested,frame);
                exit=ValueState.merge(exit,branch,g.budget);fall=null;
            } else {
                for(var child:c.getStatements()) {
                    branch=s.read(child,branch,nested,frame);if(branch==null) break;
                }
                fall=branch;
            }
        }
        if(frame.continues!=null) {
            if(outer==null) g.reject("LABELED_OR_NONLOOP_TRANSFER");
            outer.continues=ValueState.merge(outer.continues,frame.continues,g.budget);
        }
        return ValueState.merge(ValueState.merge(exit,fall,g.budget),frame.breaks,g.budget);
    }
}
