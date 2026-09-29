package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ValueUpdates {
    static String unary(ValueExpressions expr,UnaryTree n,ValueState state,List<String> controls) {
        var g=expr.g; var value=expr.read(n.getExpression(),state,controls);
        boolean update=switch(n.getKind()) {
            case PREFIX_INCREMENT,PREFIX_DECREMENT,POSTFIX_INCREMENT,POSTFIX_DECREMENT -> true;
            default -> false;
        };
        var prior=g.node(n,update?"UPDATE_PRIOR_VALUE":"OPERATION");g.edge(value,prior,"OPERAND_CANDIDATE");
        if(!update) return prior;
        var next=g.node(n,"UPDATE_NEXT_VALUE");g.edge(prior,next,"OPERAND_CANDIDATE");
        g.write(n,n.getExpression(),next,state,controls);
        return n.getKind()==Tree.Kind.POSTFIX_INCREMENT || n.getKind()==Tree.Kind.POSTFIX_DECREMENT?prior:next;
    }
    static String compound(ValueExpressions expr,CompoundAssignmentTree n,ValueState state,List<String> controls) {
        var g=expr.g;var left=expr.read(n.getVariable(),state,controls);
        var right=expr.read(n.getExpression(),state,controls);var id=g.node(n,"COMPOUND_OPERATION");
        g.edge(left,id,"OPERAND_CANDIDATE");g.edge(right,id,"OPERAND_CANDIDATE");
        g.write(n,n.getVariable(),id,state,controls);return id;
    }
}
