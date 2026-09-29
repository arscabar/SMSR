package smsr;
import java.util.*;
import static smsr.MethodSummaryState.*;

final class MethodSummaries {
    static Object read(Collector collector) {
        var budget=collector.valueBudget;var states=new LinkedHashMap<Object,MethodSummaryState>();
        for(var m:collector.methods) states.put(m.get("id"),new MethodSummaryState(m,budget));
        var connections=MethodConnections.read(collector.calls,states,budget);
        var bound=connections.stream().filter(c->"STATIC_BODY_CANDIDATE".equals(c.get("status"))).toList();
        for(var c:bound) states.get(c.get("callerId")).labels.get(c.get("callValueId")).clear();
        // ponytail: bounded bundle fixed point; upgrade to a worklist only if real bundles hit the work cap.
        boolean changed;
        do {
            changed=false;
            for(var s:states.values()) for(var edge:rows(s.values.get("edges"))) {
                budget.tick();if(!"CONTROL_CONDITION_CANDIDATE".equals(edge.get("relation")))
                    changed=add(s.labels.get(edge.get("target")),s.labels.get(edge.get("source")),budget)||changed;
            }
            for(var c:bound) {
                var caller=states.get(c.get("callerId"));var body=states.get(c.get("bodyId"));
                for(var out:body.returns) for(var label:body.labels.get(out.get("id"))) {
                    budget.tick();var target=caller.labels.get(c.get("callValueId"));var index=body.parameters.get(label);
                    if(index==null) {changed=add(target,List.of(label),budget)||changed;continue;}
                    var input=rows(c.get("inputs")).stream().filter(i-> {budget.tick();return index.equals(i.get("index"));}).findFirst().orElseThrow();
                    changed=add(target,caller.labels.get(input.get("argumentValueId")),budget)||changed;
                    var key=c.get("callId")+":"+index;
                    if(!caller.callEdges.containsKey(key)) {
                        budget.charge();caller.callEdges.put(key,Json.obj("callId",c.get("callId"),"bodyId",c.get("bodyId"),"parameterIndex",index,
                            "source",input.get("argumentValueId"),"target",c.get("callValueId"),"relation","BODY_DATA_DEPENDENCY_CANDIDATE"));
                    }
                }
            }
        } while(changed);
        states.values().forEach(s->s.finish(budget));
        if(collector.symbols.size()+collector.calls.size()+collector.references.size()+budget.rows>20_000)
            throw new IllegalArgumentException("Combined output limit");
        return connections;
    }
}
