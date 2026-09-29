package smsr;
import java.util.*;

final class Json {
    static Map<String,Object> obj(Object... pairs) {
        var result=new LinkedHashMap<String,Object>();
        for(int i=0;i<pairs.length;i+=2) result.put((String)pairs[i],pairs[i+1]);
        return result;
    }
    static String write(Object value) {
        if(value==null) return "null";
        if(value instanceof Number || value instanceof Boolean) return value.toString();
        if(value instanceof Map<?,?> map) {
            var parts=new ArrayList<String>();
            map.forEach((k,v)->parts.add(write(k.toString())+":"+write(v)));
            return "{"+String.join(",",parts)+"}";
        }
        if(value instanceof Iterable<?> list) {
            var parts=new ArrayList<String>();
            list.forEach(v->parts.add(write(v)));
            return "["+String.join(",",parts)+"]";
        }
        var out=new StringBuilder("\"");
        for(char c:value.toString().toCharArray()) {
            if(c=='"' || c=='\\') out.append('\\').append(c);
            else if(c<32 || Character.isSurrogate(c)) out.append(String.format("\\u%04x",(int)c));
            else out.append(c);
        }
        return out.append('"').toString();
    }
}
