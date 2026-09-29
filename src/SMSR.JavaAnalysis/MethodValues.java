package smsr;
import com.sun.source.tree.*;
import com.sun.source.util.TreePath;
import java.util.*;

final class MethodValues {
    static Map<String,Object> read(TreePath path,SymbolFacts facts,ValueBudget budget,boolean valid) {
        var method=(MethodTree)path.getLeaf();var g=new ValueGraph(facts,budget,path);
        Object values;
        try {
            if(!valid) g.reject("COMPILATION_ERRORS");
            if(method.getReturnType()==null) g.reject("CONSTRUCTOR_PROTOCOL");
            if(method.getBody()==null) g.reject("NO_BODY");
            if(method.getModifiers().getFlags().contains(javax.lang.model.element.Modifier.SYNCHRONIZED))
                g.reject("SYNCHRONIZED_METHOD");
            var state=new ValueState();
            for(var p:method.getParameters()) state.put(g.symbol(p),new LinkedHashSet<>(List.of(g.node(p,"PARAMETER_INPUT"))));
            var after=new ValueStatements(g).read(method.getBody(),state,List.of(),null);
            if(after!=null) g.node(method.getBody(),"NORMAL_EXIT_CANDIDATE");
            values=Json.obj("status","VALUE_FLOW_CANDIDATES","nodes",g.nodes.values(),"edges",g.edges.values(),
                "boundary","Normal-flow data candidates; lexical controls only. No feasible-path, exception, heap/alias, conversion effects or interprocedural guarantee.");
        } catch(ValueGraph.Unsupported e) {
            values=Json.obj("status","UNAVAILABLE","reason",e.getMessage(),"nodes",List.of(),"edges",List.of());
        }
        budget.charge();return Json.obj("id",facts.id(facts.trees.getElement(path)),"source",facts.source(path),"values",values,
            "control",MethodControl.read(g,method,valid));
    }
}
