package smsr;
import com.sun.source.tree.*;
import com.sun.source.util.TreePath;
import javax.lang.model.element.*;
import java.util.*;

final class CallFacts {
    static Map<String,Object> read(TreePath path,List<? extends ExpressionTree> arguments,SymbolFacts facts,boolean valid) {
        var tree=path.getLeaf(); var element=facts.trees.getElement(path);
        var method=element instanceof ExecutableElement e?e:null;
        var modifiers=method==null?Set.<Modifier>of():method.getModifiers();
        boolean direct=method!=null && (method.getKind()==ElementKind.CONSTRUCTOR || modifiers.contains(Modifier.STATIC) ||
            modifiers.contains(Modifier.PRIVATE) || modifiers.contains(Modifier.FINAL) ||
            method.getEnclosingElement().getModifiers().contains(Modifier.FINAL) || isSuper(tree));
        String dispatch=method==null?"UNRESOLVED":tree instanceof MemberReferenceTree?"METHOD_REFERENCE":direct?"DIRECT":"VIRTUAL";
        var slots=new ArrayList<Object>();
        for(int i=0;i<arguments.size();i++) {
            var argument=new TreePath(path,arguments.get(i));
            int ordinal=method==null?-1:Math.min(i,method.getParameters().size()-1);
            slots.add(Json.obj("anchorId",facts.anchor(argument),"source",facts.source(argument),"type",TypeFacts.name(facts.trees.getTypeMirror(argument)),
                "parameterOrdinal",ordinal<0?null:ordinal));
        }
        String caller=null,scope="INITIALIZER";
        for(var p=path.getParentPath();p!=null;p=p.getParentPath()) {
            if(p.getLeaf() instanceof LambdaExpressionTree) { scope="DEFERRED_LAMBDA"; break; }
            if(p.getLeaf() instanceof MethodTree) { caller=facts.id(facts.trees.getElement(p)); scope="METHOD"; break; }
        }
        return Json.obj("id",facts.anchor(path),"source",facts.source(path),"kind",tree.getKind().name(),"callerId",caller,"scope",scope,
            "targetId",facts.id(method),"signature",method==null?null:facts.signature(method),
            "resolution",method==null?"UNRESOLVED":!valid?"COMPILER_CANDIDATE":dispatch.equals("DIRECT")?"BOUND_INPUT_BUNDLE":"STATIC_TARGET_ONLY",
            "dispatch",dispatch,"expressionType",TypeFacts.name(facts.trees.getTypeMirror(path)),
            "resolvedType",tree instanceof MethodInvocationTree m?TypeFacts.name(facts.trees.getTypeMirror(new TreePath(path,m.getMethodSelect()))):null,
            "varArgsDeclared",method!=null&&method.isVarArgs(),"arguments",slots);
    }
    private static boolean isSuper(Tree tree) {
        if(!(tree instanceof MethodInvocationTree call) || !(call.getMethodSelect() instanceof MemberSelectTree select)) return false;
        return select.getExpression() instanceof IdentifierTree i && i.getName().contentEquals("super") ||
            select.getExpression() instanceof MemberSelectTree s && s.getIdentifier().contentEquals("super");
    }
}
