package smsr;
import javax.lang.model.type.*;
import javax.lang.model.element.TypeElement;
import java.util.stream.Collectors;

final class TypeFacts {
    static String name(TypeMirror type) {
        if(type==null) return null;
        return switch(type.getKind()) {
            case ARRAY -> name(((ArrayType)type).getComponentType())+"[]";
            case DECLARED -> {
                var d=(DeclaredType)type; var args=d.getTypeArguments();
                yield ((TypeElement)d.asElement()).getQualifiedName()+
                    (args.isEmpty()?"":args.stream().map(TypeFacts::name).collect(Collectors.joining(",","<",">")));
            }
            case TYPEVAR -> ((TypeVariable)type).asElement().getSimpleName().toString();
            case WILDCARD -> {
                var w=(WildcardType)type;
                yield w.getExtendsBound()!=null?"? extends "+name(w.getExtendsBound()):
                    w.getSuperBound()!=null?"? super "+name(w.getSuperBound()):"?";
            }
            case EXECUTABLE -> {
                var e=(ExecutableType)type;
                yield e.getParameterTypes().stream().map(TypeFacts::name).collect(Collectors.joining(",","(",")"))+name(e.getReturnType());
            }
            default -> type.getKind().name().toLowerCase(java.util.Locale.ROOT);
        };
    }
}
