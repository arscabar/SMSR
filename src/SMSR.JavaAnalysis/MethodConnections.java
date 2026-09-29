package smsr;
import java.util.*;
import static smsr.MethodSummaryState.*;

final class MethodConnections {
    static List<Map<String,Object>> read(List<Map<String,Object>> calls,Map<Object,MethodSummaryState> states,ValueBudget budget) {
        var result=new ArrayList<Map<String,Object>>();
        for(var call:calls) {
            budget.charge();var caller=states.get(call.get("callerId"));var body=states.get(call.get("targetId"));
            var valueId=call.get("id")+":OPAQUE_CALL_RESULT";
            String reason=!"METHOD_INVOCATION".equals(call.get("kind"))?"NON_METHOD_CALL":
                !"DIRECT".equals(call.get("dispatch"))?"NON_DIRECT_DISPATCH":
                !"BOUND_INPUT_BUNDLE".equals(call.get("resolution"))?"UNBOUND_CALL":
                Boolean.TRUE.equals(call.get("varArgsDeclared"))?"VARARGS_PROTOCOL":
                body==null?"NO_INPUT_BODY":!body.valid()?"BODY_VALUES_UNAVAILABLE":
                caller==null||!caller.labels.containsKey(valueId)?"CALLER_VALUES_UNAVAILABLE":null;
            var inputs=new ArrayList<Object>();var outputs=new ArrayList<Object>();
            if(reason==null) {
                var args=new ArrayList<>(rows(call.get("arguments")));
                for(var p:body.parameters.entrySet()) {
                    budget.charge();int index=p.getValue();var arg=args.get(index);
                    var argumentId=arg.get("anchorId")+":CALL_ARGUMENT_"+index;
                    if(!caller.labels.containsKey(argumentId)) throw new IllegalStateException("Missing call argument value");
                    inputs.add(Json.obj("index",index,"parameterValueId",p.getKey(),"argumentValueId",argumentId));
                }
                for(var out:body.returns) {budget.charge();outputs.add(Json.obj("returnValueId",out.get("id"),"callValueId",valueId));}
            }
            result.add(Json.obj("callId",call.get("id"),"callerId",call.get("callerId"),"bodyId",body==null?null:body.method.get("id"),
                "callValueId",caller!=null&&caller.labels.containsKey(valueId)?valueId:null,
                "status",reason==null?"STATIC_BODY_CANDIDATE":"UNAVAILABLE","reason",reason,"inputs",inputs,"returns",outputs));
        }
        return result;
    }
}
