package smsr;
import com.sun.source.tree.*;

final class ControlLoops {
    static int read(ControlStatements s,StatementTree n,int next) {
        var g=s.g;var expr=s.expr;int gate=g.node(n,"LOOP_GATE"),resume=gate;
        ExpressionTree condition;StatementTree body;
        if(n instanceof ForLoopTree f) {
            condition=f.getCondition();body=f.getStatement();
            resume=s.sequence(f.getUpdate(),gate,null);
        } else if(n instanceof WhileLoopTree w) {condition=w.getCondition();body=w.getStatement();}
        else {var d=(DoWhileLoopTree)n;condition=d.getCondition();body=d.getStatement();}
        int bodyEntry=s.read(body,resume,new ControlStatements.Loop(next,resume));
        g.edge(gate,condition==null?bodyEntry:expr.condition(condition,bodyEntry,next),"ALWAYS");
        if(n instanceof DoWhileLoopTree) return bodyEntry;
        if(n instanceof ForLoopTree f) return s.sequence(f.getInitializer(),gate,null);
        return gate;
    }
}
