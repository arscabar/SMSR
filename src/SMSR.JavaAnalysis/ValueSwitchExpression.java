package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueSwitchExpression {
    final ValueGraph g;final String result;ValueState exit;
    ValueSwitchExpression(ValueGraph g,SwitchExpressionTree n) {this.g=g;result=g.node(n,"SWITCH_RESULT");}
    void add(Tree n,String value,ValueState state,List<String> controls) {
        var id=g.node(n,n instanceof YieldTree?"YIELD":"SWITCH_ARM_VALUE");
        g.edge(value,id,"YIELD_VALUE");g.edge(id,result,"SWITCH_RESULT_VALUE");g.controls(id,controls);
        exit=ValueState.merge(exit,state,g.budget);
    }
    static String read(ValueGraph g,SwitchExpressionTree n,ValueState state,List<String> controls) {
        var value=new ValueSwitchExpression(g,n);var s=new ValueStatements(g);s.yields=value;
        var nested=new ArrayList<>(controls);nested.add(s.expr.read(n.getExpression(),state,controls));
        ValueState fall=null;
        // ponytail: path-insensitive MAY joins; predicates require a separate solver.
        for(var c:n.getCases()) {
            g.budget.tick();var branch=ValueState.merge(state,fall,g.budget);
            if(c.getCaseKind()==CaseTree.CaseKind.RULE) {
                if(c.getBody() instanceof ExpressionTree e) value.add(e,s.expr.read(e,branch,nested),branch,nested);
                else if(c.getBody() instanceof StatementTree body) {
                    if(s.read(body,branch,nested,null)!=null) g.reject("SWITCH_RULE_COMPLETES_NORMALLY");
                } else g.reject("SWITCH_RULE_BODY");
                fall=null;
            } else {
                for(var child:c.getStatements()) {
                    branch=s.read(child,branch,nested,null);if(branch==null) break;
                }
                fall=branch;
            }
        }
        if(fall!=null) g.reject("SWITCH_COMPLETES_WITHOUT_VALUE");
        if(value.exit==null) g.reject("NO_NORMAL_SWITCH_VALUE");
        state.clear();state.putAll(value.exit);return value.result;
    }
}
