package smsr;
import java.net.URI;
import java.util.*;
import javax.tools.SimpleJavaFileObject;

final class Source extends SimpleJavaFileObject {
    final String path,text;
    private final List<Integer> lines=new ArrayList<>(List.of(0));
    Source(String path,String text) throws Exception {
        super(new URI("memory",null,"/"+path,null),Kind.SOURCE);
        if(path.length()>1024 || !path.endsWith(".java") || path.contains("\\") || path.contains(":") ||
            path.chars().anyMatch(Character::isISOControl) ||
            Arrays.stream(path.split("/",-1)).anyMatch(p->p.isEmpty()||p.equals(".")||p.equals("..")))
            throw new IllegalArgumentException("Invalid source path");
        this.path=path; this.text=text;
        for(int i=0;i<text.length();i++) {
            char c=text.charAt(i);
            if(c=='\r' && i+1<text.length() && text.charAt(i+1)=='\n') i++;
            if(c=='\r'||c=='\n') lines.add(i+1);
        }
    }
    public CharSequence getCharContent(boolean ignore) { return text; }
    Map<String,Object> span(long start,long end) {
        if(start<0 || end<start || end>text.length()) return null;
        return Json.obj("path",path,"start",position((int)start),"end",position((int)end));
    }
    private Object position(int offset) {
        int index=Collections.binarySearch(lines,offset);
        int line=index>=0?index:-index-2;
        return Json.obj("line",line,"character",offset-lines.get(line));
    }
}
