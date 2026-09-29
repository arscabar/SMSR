package smsr;
import java.util.*;

final class MethodSummaryState {
    final Map<String,Object> method,values;
    final Map<String,Set<String>> labels=new LinkedHashMap<>();
    final Map<String,Integer> parameters=new LinkedHashMap<>();
    final List<Map<String,Object>> returns=new ArrayList<>();
    final Map<String,Object> callEdges=new LinkedHashMap<>();
    MethodSummaryState(Map<String,Object> method,ValueBudget budget) {
        this.method=method;values=map(method.get("values"));
        for(var node:rows(values.get("nodes"))) {
            budget.tick();var id=(String)node.get("id");var kind=node.get("kind");
            var seed=new LinkedHashSet<String>();
            if("PARAMETER_INPUT".equals(kind)) {parameters.put(id,parameters.size());seed.add(id);}
            if("EXTERNAL_READ".equals(kind)||"OPAQUE_CALL_RESULT".equals(kind)) seed.add("unknown:"+id);
            labels.put(id,seed);
            if("RETURN".equals(kind)||"NORMAL_EXIT_CANDIDATE".equals(kind)) returns.add(node);
        }
    }
    boolean valid() {return "VALUE_FLOW_CANDIDATES".equals(values.get("status"));}
    static boolean add(Set<String> target,Collection<String> source,ValueBudget budget) {
        boolean changed=false;
        for(var label:source) {budget.tick();if(target.add(label)) {budget.charge();changed=true;}}
        return changed;
    }
    // These maps are produced inside the trusted analyzer, never parsed from client JSON.
    @SuppressWarnings("unchecked") static Map<String,Object> map(Object value) {return (Map<String,Object>)value;}
    @SuppressWarnings("unchecked") static Collection<Map<String,Object>> rows(Object value) {return (Collection<Map<String,Object>>)value;}
    void finish(ValueBudget budget) {
        var outputs=new ArrayList<Object>();
        for(var node:returns) {
            budget.charge();var data=labels.get(node.get("id"));
            outputs.add(Json.obj("returnValueId",node.get("id"),"parameterIndices",data.stream().filter(parameters::containsKey).map(parameters::get).sorted().toList(),
                "unknownValueIds",data.stream().filter(x->x.startsWith("unknown:")).map(x->x.substring(8)).sorted().toList()));
        }
        method.put("valueSummary",Json.obj("status",valid()?"DATA_DEPENDENCY_CANDIDATES":"UNAVAILABLE",
            "reason",values.get("reason"),"returns",outputs,"callEdges",callEdges.values()));
    }
}
