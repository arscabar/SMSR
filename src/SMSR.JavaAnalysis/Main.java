package smsr;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.ByteBuffer;
import java.util.*;

public final class Main {
    public static void main(String[] args) {
        try {
            if(args.length!=0) throw new IllegalArgumentException();
            var raw=System.in.readNBytes(32*1024*1024+1);
            if(raw.length>32*1024*1024) throw new IllegalArgumentException();
            var input=new DataInputStream(new ByteArrayInputStream(raw));
            int count=input.readInt(); if(count<1 || count>500) throw new IllegalArgumentException();
            var sources=new ArrayList<Source>(); var paths=new HashSet<String>(); int total=0;
            for(int i=0;i<count;i++) {
                String path=read(input,4096),text=read(input,2*1024*1024);
                total+=text.getBytes(StandardCharsets.UTF_8).length;
                if(total>16*1024*1024 || !paths.add(path.toLowerCase(Locale.ROOT))) throw new IllegalArgumentException();
                sources.add(new Source(path,text));
            }
            if(input.read()!=-1) throw new IllegalArgumentException();
            var result=Json.write(Analyzer.run(sources)).getBytes(StandardCharsets.UTF_8);
            if(result.length>16*1024*1024) throw new IllegalArgumentException();
            System.out.write(result);
        } catch(Throwable error) {
            System.out.print("{\"error\":\"Java analysis failed: check input, JDK and limits\"}");
            System.exit(1);
        }
    }
    private static String read(DataInputStream input,int limit) throws Exception {
        int length=input.readInt(); if(length<0 || length>limit) throw new IllegalArgumentException();
        var bytes=input.readNBytes(length); if(bytes.length!=length) throw new EOFException();
        return StandardCharsets.UTF_8.newDecoder().decode(ByteBuffer.wrap(bytes)).toString();
    }
}
