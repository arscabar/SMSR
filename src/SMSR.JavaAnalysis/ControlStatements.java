package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ControlStatements {
    record Loop(int exit,Integer resume) {}
    final ControlGraph g;final ControlExpressions expr;
    Integer yieldTarget;
    ControlStatements(ControlGraph g) {this.g=g;expr=new ControlExpressions(g);}
    int sequence(List<? extends StatementTree> items,int next,Loop loop) {
        for(int i=items.size()-1;i>=0;i--) next=read(items.get(i),next,loop);
        return next;
    }
    int read(StatementTree n,int next,Loop loop) {
        g.value.budget.tick();
        if(n instanceof BlockTree b) return sequence(b.getStatements(),next,loop);
        if(n instanceof EmptyStatementTree) return next;
        if(n instanceof VariableTree v) return expr.read(v.getInitializer(),g.step(n,"DECLARE",next));
        if(n instanceof ExpressionStatementTree e) return expr.read(e.getExpression(),next);
        if(n instanceof ReturnTree r) return expr.read(r.getExpression(),g.step(n,"RETURN",-1));
        if(n instanceof ThrowTree t) return expr.read(t.getExpression(),g.step(n,"EXPLICIT_THROW",-1));
        if(n instanceof YieldTree y) {
            if(yieldTarget==null) g.value.reject("YIELD_WITHOUT_SWITCH");
            return expr.read(y.getValue(),g.step(n,"YIELD",yieldTarget));
        }
        if(n instanceof IfTree b) return expr.condition(b.getCondition(),read(b.getThenStatement(),next,loop),
            b.getElseStatement()==null?next:read(b.getElseStatement(),next,loop));
        if(n instanceof WhileLoopTree || n instanceof DoWhileLoopTree || n instanceof ForLoopTree)
            return ControlLoops.read(this,n,next);
        if(n instanceof SwitchTree choice) return ControlSwitch.read(this,choice,next,loop);
        if(n instanceof BreakTree b) {
            if(loop==null || b.getLabel()!=null) g.value.reject("LABELED_OR_NONLOOP_TRANSFER");
            return g.step(n,"BREAK",loop.exit());
        }
        if(n instanceof ContinueTree c) {
            if(loop==null || loop.resume()==null || c.getLabel()!=null) g.value.reject("LABELED_OR_NONLOOP_TRANSFER");
            return g.step(n,"CONTINUE",loop.resume());
        }
        g.value.reject("STATEMENT_"+n.getKind());return -1;
    }
}
