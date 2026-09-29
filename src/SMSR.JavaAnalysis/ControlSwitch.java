package smsr;
import com.sun.source.tree.*;
import java.util.List;

final class ControlSwitch {
    static int read(ControlStatements s,SwitchTree n,int next,ControlStatements.Loop outer) {
        return read(s,n.getExpression(),n.getCases(),next,outer,false);
    }
    static int expression(ControlExpressions e,SwitchExpressionTree n,int next) {
        var s=new ControlStatements(e.g);s.yieldTarget=next;
        return read(s,n.getExpression(),n.getCases(),next,null,true);
    }
    private static int read(ControlStatements s,ExpressionTree selector,List<? extends CaseTree> cases,
                            int next,ControlStatements.Loop outer,boolean expression) {
        var g=s.g;var entries=new int[cases.size()];
        var transfer=expression?null:new ControlStatements.Loop(next,outer==null?null:outer.resume());
        int fall=expression?-1:next,fallback=expression?g.step(selector,"UNMATCHED_SWITCH_THROW",-1):next;
        for(int i=cases.size()-1;i>=0;i--) {
            g.value.budget.tick();var c=cases.get(i);
            if(c.getCaseKind()==CaseTree.CaseKind.RULE) {
                if(expression && c.getBody() instanceof ExpressionTree value)
                    entries[i]=s.expr.read(value,g.step(value,"SWITCH_ARM_VALUE",next));
                else if(c.getBody() instanceof StatementTree body) entries[i]=s.read(body,next,transfer);
                else g.value.reject("SWITCH_RULE_BODY");
            } else entries[i]=s.sequence(c.getStatements(),fall,transfer);
            fall=entries[i];
            if(c.getExpressions().isEmpty()) fallback=entries[i];
        }
        int search=fallback;
        // Normalized constant selection, not runtime evaluation of case expressions.
        for(int i=cases.size()-1;i>=0;i--) {
            var labels=cases.get(i).getExpressions();
            for(int j=labels.size()-1;j>=0;j--) {
                g.value.budget.tick();int test=g.node(labels.get(j),"CASE_TEST");
                g.edge(test,entries[i],"CASE_MATCH");g.edge(test,search,"CASE_NO_MATCH");search=test;
            }
        }
        return s.expr.read(selector,g.step(selector,"SWITCH_VALUE",search));
    }
}
