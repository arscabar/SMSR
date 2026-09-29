package smsr;
import com.sun.source.tree.*;

final class ControlExpressions {
    final ControlGraph g;
    ControlExpressions(ControlGraph g) {this.g=g;}
    int read(ExpressionTree n,int next) {
        g.value.budget.tick();if(n==null) return next;
        if(n instanceof ParenthesizedTree p) return read(p.getExpression(),next);
        int done=g.step(n,"EVALUATE",next);
        if(n instanceof ConditionalExpressionTree c)
            return condition(c.getCondition(),read(c.getTrueExpression(),done),read(c.getFalseExpression(),done));
        if(n.getKind()==Tree.Kind.CONDITIONAL_AND || n.getKind()==Tree.Kind.CONDITIONAL_OR)
            return condition(n,done,done);
        return ControlOperands.read(this,n,done);
    }
    int condition(ExpressionTree n,int yes,int no) {
        g.value.budget.tick();
        if(n instanceof ParenthesizedTree p) return condition(p.getExpression(),yes,no);
        if(n instanceof UnaryTree u && n.getKind()==Tree.Kind.LOGICAL_COMPLEMENT)
            return condition(u.getExpression(),no,yes);
        if(n instanceof BinaryTree b) {
            if(n.getKind()==Tree.Kind.CONDITIONAL_AND) return condition(b.getLeftOperand(),condition(b.getRightOperand(),yes,no),no);
            if(n.getKind()==Tree.Kind.CONDITIONAL_OR) return condition(b.getLeftOperand(),yes,condition(b.getRightOperand(),yes,no));
        }
        if(n instanceof ConditionalExpressionTree c)
            return condition(c.getCondition(),condition(c.getTrueExpression(),yes,no),condition(c.getFalseExpression(),yes,no));
        int test=g.node(n,"CONDITION");
        if(n instanceof LiteralTree l && l.getValue() instanceof Boolean b) g.edge(test,b?yes:no,b?"TRUE":"FALSE");
        else {g.edge(test,yes,"TRUE");g.edge(test,no,"FALSE");}
        return ControlOperands.read(this,n,test);
    }
}
