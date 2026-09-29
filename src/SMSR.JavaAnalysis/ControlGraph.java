package smsr;
import com.sun.source.tree.*;
import java.util.*;

final class ControlGraph {
    final ValueGraph value;
    final List<Object> nodes=new ArrayList<>(),edges=new ArrayList<>();
    ControlGraph(ValueGraph value) { this.value=value; }
    int node(Tree tree,String kind) {
        value.budget.charge();var source=value.facts.source(value.paths.get(tree));
        if(source==null) value.reject("SYNTHETIC_LOCATION");
        int id=nodes.size();nodes.add(Json.obj("id",id,"kind",kind,"source",source));return id;
    }
    void edge(int from,int to,String outcome) {
        value.budget.charge();edges.add(Json.obj("source",from,"target",to,"outcome",outcome));
    }
    int step(Tree tree,String kind,int next) {int id=node(tree,kind);edge(id,next,"ALWAYS");return id;}
}
