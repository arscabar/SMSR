package smsr;
import com.sun.source.tree.*;
import com.sun.source.util.*;
import java.util.*;

final class Collector extends TreePathScanner<Void,Void> {
    final Map<String,Object> symbols=new LinkedHashMap<>();
    final List<Object> references=new ArrayList<>();
    final List<Map<String,Object>> calls=new ArrayList<>(),methods=new ArrayList<>(); final ValueBudget valueBudget=new ValueBudget();
    final SymbolFacts facts; final boolean valid;
    private int visited=0;
    Collector(SymbolFacts facts,boolean valid) { this.facts=facts; this.valid=valid; }
    public Void scan(Tree node,Void unused) {
        if(++visited>100_000 || symbols.size()+calls.size()+references.size()+valueBudget.rows>20_000)
            throw new IllegalArgumentException("Node limit exceeded");
        return super.scan(node,unused);
    }
    private void declaration() {
        var path=getCurrentPath(); var source=facts.source(path); var e=facts.trees.getElement(path);
        if(source!=null && e!=null) symbols.put(facts.id(e),Json.obj("id",facts.id(e),"name",e.getSimpleName().toString(),
            "kind",e.getKind().name(),"source",source,"type",TypeFacts.name(e.asType())));
    }
    private void call(List<? extends ExpressionTree> args) {
        if(facts.source(getCurrentPath())!=null) calls.add(CallFacts.read(getCurrentPath(),args,facts,valid));
    }
    private void reference() {
        var p=getCurrentPath(); var source=facts.source(p); if(source==null) return;
        var e=facts.trees.getElement(p);
        references.add(Json.obj("source",source,"targetId",facts.id(e),
            "resolution",e==null?"UNRESOLVED":valid?"COMPILER_BOUND":"COMPILER_CANDIDATE"));
    }
    public Void visitClass(ClassTree n,Void p) { declaration(); return super.visitClass(n,p); }
    public Void visitMethod(MethodTree n,Void p) {
        declaration();if(facts.source(getCurrentPath())!=null) methods.add(MethodValues.read(getCurrentPath(),facts,valueBudget,valid));
        return super.visitMethod(n,p);
    }
    public Void visitVariable(VariableTree n,Void p) { declaration(); return super.visitVariable(n,p); }
    public Void visitMethodInvocation(MethodInvocationTree n,Void p) { call(n.getArguments()); return super.visitMethodInvocation(n,p); }
    public Void visitNewClass(NewClassTree n,Void p) { call(n.getArguments()); return super.visitNewClass(n,p); }
    public Void visitMemberReference(MemberReferenceTree n,Void p) { call(List.of()); return super.visitMemberReference(n,p); }
    public Void visitIdentifier(IdentifierTree n,Void p) { reference(); return super.visitIdentifier(n,p); }
    public Void visitMemberSelect(MemberSelectTree n,Void p) { reference(); return super.visitMemberSelect(n,p); }
}
