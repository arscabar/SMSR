package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueLoops {
    static final class Frame { ValueState breaks,continues; }
    static ValueState read(ValueStatements statements,StatementTree n,ValueState state,List<String> controls) {
        var g=statements.g;var expr=statements.expr;
        ExpressionTree condition;StatementTree body;
        if(n instanceof ForLoopTree f) {
            for(var init:f.getInitializer()) state=statements.read(init,state,controls,null);
            condition=f.getCondition();body=f.getStatement();
        } else if(n instanceof WhileLoopTree w) {condition=w.getCondition();body=w.getStatement();}
        else {var d=(DoWhileLoopTree)n;condition=d.getCondition();body=d.getStatement();}
        var entry=state.copy();var head=state.copy();boolean isDo=n instanceof DoWhileLoopTree;
        for(;;) {
            g.budget.tick();var tested=head.copy();var nested=new ArrayList<>(controls);var frame=new Frame();
            if(!isDo && condition!=null) nested.add(expr.read(condition,tested,controls));
            var back=statements.read(body,tested.copy(),nested,frame);
            back=ValueState.merge(back,frame.continues,g.budget);
            if(back!=null && n instanceof ForLoopTree f)
                for(var update:f.getUpdate()) back=statements.read(update,back,controls,null);
            var exit=condition!=null && !isDo?tested:null;
            if(isDo && back!=null) {expr.read(condition,back,controls);exit=back;}
            exit=ValueState.merge(exit,frame.breaks,g.budget);
            var next=ValueState.merge(entry,back,g.budget);
            if(head.equals(next)) return exit;
            head=next;
        }
    }
}
