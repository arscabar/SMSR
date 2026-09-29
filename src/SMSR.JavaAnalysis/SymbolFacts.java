package smsr;
import com.sun.source.util.*;
import javax.lang.model.element.*;
import javax.lang.model.util.Types;
import java.util.*;
import java.util.stream.Collectors;

final class SymbolFacts {
    final Trees trees; final Types types; final Map<java.net.URI,Source> sources;
    SymbolFacts(Trees trees,Types types,Map<java.net.URI,Source> sources) { this.trees=trees; this.types=types; this.sources=sources; }
    Map<String,Object> source(TreePath path) {
        if(path==null) return null;
        var s=sources.get(path.getCompilationUnit().getSourceFile().toUri()); if(s==null) return null;
        var positions=trees.getSourcePositions(); var unit=path.getCompilationUnit();
        return s.span(positions.getStartPosition(unit,path.getLeaf()),positions.getEndPosition(unit,path.getLeaf()));
    }
    String anchor(TreePath path) {
        var source=source(path);if(source==null) return null;
        var positions=trees.getSourcePositions();var unit=path.getCompilationUnit();
        return source.get("path")+":"+positions.getStartPosition(unit,path.getLeaf())+":"+positions.getEndPosition(unit,path.getLeaf());
    }
    String id(Element element) {
        if(element==null) return null;
        var path=trees.getPath(element); var source=source(path);
        String prefix=source!=null?"source:":path!=null?"generated:":"metadata:";
        if(element instanceof TypeElement type && !type.getQualifiedName().isEmpty())
            return prefix+type.getQualifiedName();
        if(element instanceof PackageElement p) return prefix+"package:"+p.getQualifiedName();
        if(element instanceof ModuleElement m) return prefix+"module:"+m.getQualifiedName();
        if(element instanceof ExecutableElement method) return id(method.getEnclosingElement())+"#"+
            method.getSimpleName()+method.getParameters().stream().map(p->TypeFacts.name(types.erasure(p.asType())))
                .collect(Collectors.joining(",","(",")"));
        if(source!=null) return "source:"+sources.get(path.getCompilationUnit().getSourceFile().toUri()).path+":"+
            trees.getSourcePositions().getStartPosition(path.getCompilationUnit(),path.getLeaf())+":"+element.getKind()+":"+element.getSimpleName();
        return id(element.getEnclosingElement())+":"+element.getKind()+":"+element.getSimpleName();
    }
    String signature(ExecutableElement method) {
        return id(method.getEnclosingElement())+"."+method.getSimpleName()+
            method.getParameters().stream().map(p->TypeFacts.name(p.asType())).collect(Collectors.joining(",","(",")"));
    }
}
