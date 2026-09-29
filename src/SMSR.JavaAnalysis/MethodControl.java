package smsr;
import com.sun.source.tree.*;
import javax.lang.model.element.Modifier;
import java.util.*;

final class MethodControl {
    static Object read(ValueGraph values,MethodTree method,boolean valid) {
        try {
            if(!valid) values.reject("COMPILATION_ERRORS");
            if(method.getReturnType()==null) values.reject("CONSTRUCTOR_PROTOCOL");
            if(method.getBody()==null) values.reject("NO_BODY");
            if(method.getModifiers().getFlags().contains(Modifier.SYNCHRONIZED)) values.reject("SYNCHRONIZED_METHOD");
            var graph=new ControlGraph(values);
            var statements=new ControlStatements(graph);
            int body=statements.read(method.getBody(),-1,null);
            int entry=graph.step(method.getBody(),"ENTRY",body);
            return Json.obj("status","NORMAL_CFG","entry",entry,"exitNode",-1,"nodes",graph.nodes,"transfers",graph.edges);
        } catch(ValueGraph.Unsupported e) {
            return Json.obj("status","UNAVAILABLE","reason",e.getMessage(),"nodes",List.of(),"transfers",List.of());
        }
    }
}
