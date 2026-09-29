package smsr;
import com.sun.source.tree.*;
import com.sun.source.util.*;
import javax.lang.model.element.*;
import java.util.*;

final class ValueGraph {
    final SymbolFacts facts; final ValueBudget budget;
    final Map<Tree,TreePath> paths=new IdentityHashMap<>();
    final Map<String,Object> nodes=new LinkedHashMap<>(),edges=new LinkedHashMap<>();
    ValueGraph(SymbolFacts facts,ValueBudget budget,TreePath method) {
        this.facts=facts; this.budget=budget; paths.put(method.getLeaf(),method);
        new TreePathScanner<Void,Void>() {
            public Void scan(Tree n,Void p) {
                budget.tick(); if(n!=null) paths.put(n,new TreePath(getCurrentPath(),n));
                return super.scan(n,p);
            }
        }.scan(method,null);
    }
    static final class Unsupported extends RuntimeException { Unsupported(String reason) { super(reason); } }
    void reject(String reason) { throw new Unsupported(reason); }
    String symbol(Tree n) { return facts.id(facts.trees.getElement(paths.get(n))); }
    boolean local(Tree n) {
        var e=facts.trees.getElement(paths.get(n));
        return e!=null && (e.getKind()==ElementKind.LOCAL_VARIABLE || e.getKind()==ElementKind.PARAMETER);
    }
    String node(Tree n,String kind) {
        budget.tick(); var path=paths.get(n); var source=facts.source(path);
        if(source==null) reject("SYNTHETIC_LOCATION");
        var id=facts.anchor(path)+":"+kind;
        if(!nodes.containsKey(id)) { budget.charge(); nodes.put(id,Json.obj("id",id,"kind",kind,"source",source,
            "symbolId",n instanceof VariableTree || n instanceof IdentifierTree?symbol(n):null)); }
        return id;
    }
    void edge(String from,String to,String kind) {
        budget.tick(); var id=Json.write(List.of(from,to,kind));
        if(!edges.containsKey(id)) { budget.charge(); edges.put(id,Json.obj("source",from,"target",to,"relation",kind)); }
    }
    void controls(String id,List<String> controls) { for(var c:controls) edge(c,id,"CONTROL_CONDITION_CANDIDATE"); }
    String write(Tree anchor,Tree variable,String value,ValueState state,List<String> controls) {
        if(!local(variable)) reject("NONLOCAL_WRITE");
        var id=node(anchor,"ASSIGNMENT"); edge(value,id,"ASSIGNMENT_INPUT"); controls(id,controls);
        state.put(symbol(variable),new LinkedHashSet<>(List.of(id))); return value;
    }
}
