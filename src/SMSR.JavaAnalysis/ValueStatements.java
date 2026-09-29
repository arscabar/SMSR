package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueStatements {
    final ValueGraph g; final ValueExpressions expr;
    ValueSwitchExpression yields;
    ValueStatements(ValueGraph g) { this.g=g; expr=new ValueExpressions(g); }
    ValueState read(StatementTree n,ValueState state,List<String> controls,ValueLoops.Frame loop) {
        g.budget.tick();if(state==null) return null;
        if(n instanceof BlockTree block) {
            for(var child:block.getStatements()) {state=read(child,state,controls,loop);if(state==null) break;}
            return state;
        }
        if(n instanceof VariableTree v) {
            if(v.getInitializer()!=null) g.write(v,v,expr.read(v.getInitializer(),state,controls),state,controls);
            else state.put(g.symbol(v),new LinkedHashSet<>(List.of(g.node(v,"UNINITIALIZED_LOCAL"))));
            return state;
        }
        if(n instanceof ExpressionStatementTree e) {expr.read(e.getExpression(),state,controls);return state;}
        if(n instanceof YieldTree y) {
            if(yields==null) g.reject("YIELD_WITHOUT_SWITCH");
            yields.add(y,expr.read(y.getValue(),state,controls),state,controls);return null;
        }
        if(n instanceof ReturnTree r) {
            var id=g.node(n,"RETURN");
            var value=r.getExpression()==null?g.node(n,"VOID_RETURN"):expr.read(r.getExpression(),state,controls);
            g.edge(value,id,"RETURN_VALUE");g.controls(id,controls);return null;
        }
        if(n instanceof IfTree branch) {
            var condition=expr.read(branch.getCondition(),state,controls);
            var nested=new ArrayList<>(controls);nested.add(condition);
            return ValueState.merge(read(branch.getThenStatement(),state.copy(),nested,loop),
                branch.getElseStatement()==null?state.copy():read(branch.getElseStatement(),state.copy(),nested,loop),g.budget);
        }
        if(n instanceof WhileLoopTree || n instanceof ForLoopTree || n instanceof DoWhileLoopTree)
            return ValueLoops.read(this,n,state,controls);
        if(n instanceof SwitchTree choice) return ValueSwitch.read(this,choice,state,controls,loop);
        if(n instanceof BreakTree b) {
            if(loop==null || b.getLabel()!=null) g.reject("LABELED_OR_NONLOOP_TRANSFER");
            loop.breaks=ValueState.merge(loop.breaks,state,g.budget);return null;
        }
        if(n instanceof ContinueTree c) {
            if(loop==null || c.getLabel()!=null) g.reject("LABELED_OR_NONLOOP_TRANSFER");
            loop.continues=ValueState.merge(loop.continues,state,g.budget);return null;
        }
        if(n instanceof EmptyStatementTree) return state;
        g.reject("STATEMENT_"+n.getKind());return null;
    }
}
