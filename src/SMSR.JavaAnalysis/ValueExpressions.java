package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueExpressions {
    final ValueGraph g;
    ValueExpressions(ValueGraph g) { this.g=g; }
    String read(ExpressionTree n,ValueState state,List<String> controls) {
        g.budget.tick();
        if(n instanceof ParenthesizedTree p) return read(p.getExpression(),state,controls);
        if(n instanceof SwitchExpressionTree s) return ValueSwitchExpression.read(g,s,state,controls);
        if(n instanceof LiteralTree) return g.node(n,"CONSTANT_VALUE_REDACTED");
        if(n instanceof IdentifierTree) {
            var id=g.node(n,g.local(n)?"LOCAL_READ":"EXTERNAL_READ");
            for(var d:state.getOrDefault(g.symbol(n),Set.of())) g.edge(d,id,"REACHING_DEFINITION");
            return id;
        }
        if(n instanceof AssignmentTree a) return g.write(n,a.getVariable(),read(a.getExpression(),state,controls),state,controls);
        if(n instanceof BinaryTree b) {
            var left=read(b.getLeftOperand(),state,controls);
            boolean shortCircuit=n.getKind()==Tree.Kind.CONDITIONAL_AND || n.getKind()==Tree.Kind.CONDITIONAL_OR;
            var branch=shortCircuit?state.copy():state;
            var right=read(b.getRightOperand(),branch,controls);var id=g.node(n,"OPERATION");
            g.edge(left,id,"OPERAND_CANDIDATE"); g.edge(right,id,"OPERAND_CANDIDATE");
            if(shortCircuit) {var joined=ValueState.merge(state,branch,g.budget);state.clear();state.putAll(joined);}
            return id;
        }
        if(n instanceof TypeCastTree c) {
            var value=read(c.getExpression(),state,controls);var id=g.node(n,"CAST_CANDIDATE");
            g.edge(value,id,"OPERAND_CANDIDATE"); return id;
        }
        if(n instanceof UnaryTree u) return ValueUpdates.unary(this,u,state,controls);
        if(n instanceof CompoundAssignmentTree c) return ValueUpdates.compound(this,c,state,controls);
        if(n instanceof MethodInvocationTree call) {
            var site=g.node(n,"CALL_INPUT_BOUNDARY");
            if(call.getMethodSelect() instanceof MemberSelectTree select) {
                var e=g.facts.trees.getElement(g.paths.get(select.getExpression()));
                if(!(e instanceof javax.lang.model.element.TypeElement))
                    g.edge(read(select.getExpression(),state,controls),site,"RECEIVER_CANDIDATE");
            }
            int i=0; for(var arg:call.getArguments()) {
                var value=read(arg,state,controls);var id=g.node(arg,"CALL_ARGUMENT_"+i++);
                g.edge(value,id,"ARGUMENT_VALUE");g.edge(id,site,"CALL_ARGUMENT");
            }
            g.controls(site,controls); return g.node(n,"OPAQUE_CALL_RESULT");
        }
        g.reject("EXPRESSION_"+n.getKind()); return null;
    }
}
