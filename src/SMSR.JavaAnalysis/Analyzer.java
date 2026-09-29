package smsr;
import com.sun.source.util.*;
import javax.tools.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;

final class Analyzer {
    static Object run(List<Source> sources) throws Exception {
        var compiler=ToolProvider.getSystemJavaCompiler();
        if(compiler==null) throw new IllegalStateException("JDK required");
        var diagnostics=new ArrayList<Object>(); var errors=new int[2];
        var inputs=new HashMap<java.net.URI,Source>(); sources.forEach(s->inputs.put(s.toUri(),s));
        DiagnosticListener<JavaFileObject> listener=d->{
            errors[1]++; if(d.getKind()==Diagnostic.Kind.ERROR) errors[0]++;
            var source=d.getSource()==null?null:inputs.get(d.getSource().toUri());
            if(diagnostics.size()<2000) diagnostics.add(Json.obj("code",d.getCode(),"severity",d.getKind().name(),
                "source",source==null?null:source.span(d.getStartPosition(),d.getEndPosition())));
        };
        try(var manager=compiler.getStandardFileManager(listener,Locale.ROOT,StandardCharsets.UTF_8)) {
            for(var location:List.of(StandardLocation.CLASS_PATH,StandardLocation.SOURCE_PATH,
                StandardLocation.ANNOTATION_PROCESSOR_PATH,StandardLocation.MODULE_PATH)) manager.setLocationFromPaths(location,List.of());
            var task=(JavacTask)compiler.getTask(new PrintWriter(OutputStream.nullOutputStream()),manager,listener,
                List.of("--release","17","-proc:none","-implicit:none","-Xmaxerrs","2000","-Xmaxwarns","2000"),null,sources);
            var units=new ArrayList<com.sun.source.tree.CompilationUnitTree>(); task.parse().forEach(units::add);
            task.analyze();
            var collector=new Collector(new SymbolFacts(Trees.instance(task),task.getTypes(),inputs),errors[0]==0);
            units.forEach(unit->collector.scan(unit,null));
            var connections=MethodSummaries.read(collector);
            return Json.obj("status",errors[0]==0?"BOUND_INPUT_BUNDLE":"COMPILATION_ERRORS",
                "profile","EXPLICIT_FILES_JDK17_NO_PROCESSORS","languageVersion","17","runtime",Runtime.version().toString(),
                "paths",sources.stream().map(s->s.path).toList(),"symbols",collector.symbols.values(),
                "calls",collector.calls,"references",collector.references,"methods",collector.methods,"connections",connections,"diagnostics",diagnostics,
                "diagnosticCount",errors[1],"diagnosticsTruncated",errors[1]>2000,
                "limitations",List.of("NO_BUILD_OR_DEPENDENCY_RESOLUTION","STATIC_DISPATCH_EVIDENCE","PARTIAL_NORMAL_CFG","NO_FULL_PDG_OR_TAINT"));
        }
    }
}
