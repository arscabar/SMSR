package main

import (
 "fmt"
 "go/ast"
 "go/token"
 "go/types"
 "strings"
 "unicode/utf16"
)

type analysis struct { fset *token.FileSet; texts map[string]string; info *types.Info; valid bool; rows int }
func(a *analysis)tick(){a.rows++;if a.rows>50000 { panic("graph limit") }}
func(a *analysis)point(pos token.Pos)record{
 p:=a.fset.PositionFor(pos,false) // Ignore user-supplied //line directives.
 text,ok:=a.texts[p.Filename];if !ok || p.Offset<0 || p.Offset>len(text) { return nil }
 start:=strings.LastIndex(text[:p.Offset],"\n")+1
 return record{"line":p.Line-1,"character":len(utf16.Encode([]rune(text[start:p.Offset])))}
}
func(a *analysis)span(node ast.Node)record{
 if node==nil{return nil}
 return record{"path":a.fset.PositionFor(node.Pos(),false).Filename,"start":a.point(node.Pos()),"end":a.point(node.End())}
}
func(a *analysis)identity(obj types.Object)string{
 if obj==nil{return ""}
 if f,ok:=obj.(*types.Func);ok {obj=f.Origin()}
 p:=a.fset.PositionFor(obj.Pos(),false)
 if _,ok:=a.texts[p.Filename];ok {return fmt.Sprintf("source:%s:%d:%s",p.Filename,p.Offset,obj.Name())}
 pkg:="builtin";if obj.Pkg()!=nil {pkg=obj.Pkg().Path()}
 owner:=""
 if f,ok:=obj.(*types.Func);ok {
  if s,ok:=f.Type().(*types.Signature);ok && s.Recv()!=nil {
   t:=s.Recv().Type();if p,ok:=t.(*types.Pointer);ok{t=p.Elem()}
   if n,ok:=t.(*types.Named);ok{owner=n.Obj().Name()+"."}
  }
 }
 return "metadata:"+pkg+":"+owner+obj.Name()
}
func(a *analysis)resolution(obj types.Object)string{
 if obj==nil{return "UNRESOLVED"};if !a.valid{return "COMPILER_CANDIDATE"}
 if strings.HasPrefix(a.identity(obj),"source:"){return "BOUND_INPUT_BUNDLE"}
 return "BOUND_METADATA"
}
func(a *analysis)symbol(id *ast.Ident,obj types.Object)record{
 return record{"id":a.identity(obj),"name":obj.Name(),"kind":fmt.Sprintf("%T",obj),
  "type":a.shape(obj.Type(),0),"source":a.span(id),"resolution":a.resolution(obj)}
}
