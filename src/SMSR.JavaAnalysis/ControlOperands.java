package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ControlOperands {
    static int read(ControlExpressions e,ExpressionTree n,int next) {
        if(n instanceof SwitchExpressionTree s) return ControlSwitch.expression(e,s,next);
        var children=new ArrayList<ExpressionTree>();
        if(n instanceof LiteralTree || n instanceof IdentifierTree || n instanceof LambdaExpressionTree) return next;
        if(n instanceof MemberSelectTree s) children.add(s.getExpression());
        else if(n instanceof BinaryTree b) {children.add(b.getLeftOperand());children.add(b.getRightOperand());}
        else if(n instanceof UnaryTree u) children.add(u.getExpression());
        else if(n instanceof TypeCastTree c) children.add(c.getExpression());
        else if(n instanceof InstanceOfTree i) children.add(i.getExpression());
        else if(n instanceof AssignmentTree a) {children.add(a.getVariable());children.add(a.getExpression());}
        else if(n instanceof CompoundAssignmentTree a) {children.add(a.getVariable());children.add(a.getExpression());}
        else if(n instanceof ArrayAccessTree a) {children.add(a.getExpression());children.add(a.getIndex());}
        else if(n instanceof MemberReferenceTree r) children.add(r.getQualifierExpression());
        else if(n instanceof MethodInvocationTree c) {
            if(c.getMethodSelect() instanceof MemberSelectTree s) children.add(s.getExpression());
            children.addAll(c.getArguments());
        } else if(n instanceof NewClassTree c) {
            if(c.getClassBody()!=null) e.g.value.reject("ANONYMOUS_CLASS_INITIALIZATION");
            if(c.getEnclosingExpression()!=null) children.add(c.getEnclosingExpression());children.addAll(c.getArguments());
        } else if(n instanceof NewArrayTree a) {
            children.addAll(a.getDimensions());if(a.getInitializers()!=null) children.addAll(a.getInitializers());
        } else e.g.value.reject("EXPRESSION_"+n.getKind());
        for(int i=children.size()-1;i>=0;i--) next=e.read(children.get(i),next);
        return next;
    }
}
