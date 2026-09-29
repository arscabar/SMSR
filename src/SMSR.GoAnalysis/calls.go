package main

import (
 "fmt"
 "go/ast"
 "go/types"
)

func unwrapped(e ast.Expr)ast.Expr {
 switch v:=e.(type){
 case *ast.ParenExpr:return unwrapped(v.X)
 case *ast.IndexExpr:return unwrapped(v.X)
 case *ast.IndexListExpr:return unwrapped(v.X)
 default:return e
 }
}

func(a *analysis)call(c *ast.CallExpr)record{
 var obj types.Object;dispatch:="UNRESOLVED";receiver:=any(nil)
 fun:=unwrapped(c.Fun)
 switch f:=fun.(type){
 case *ast.Ident:obj=a.info.ObjectOf(f)
 case *ast.SelectorExpr:
  obj=a.info.ObjectOf(f.Sel)
  if s:=a.info.Selections[f];s!=nil {
   receiver=record{"source":a.span(f.X),"type":a.shape(s.Recv(),0),"selectionKind":int(s.Kind()),"indirect":s.Indirect()}
  }
 }
 target:=""
 if a.info.Types[c.Fun].IsType(){dispatch="TYPE_CONVERSION"}else{
  switch v:=obj.(type){
  case *types.Func:
   target=a.identity(v);dispatch="DIRECT_FUNCTION"
   if s,ok:=v.Type().(*types.Signature);ok && s.Recv()!=nil {
    dispatch="DIRECT_METHOD"
    if types.IsInterface(s.Recv().Type()){dispatch="INTERFACE_DISPATCH"}
   }
  case *types.Builtin:target=a.identity(v);dispatch="BUILTIN"
  default:
   if _,ok:=a.info.TypeOf(c.Fun).(*types.Signature);ok{dispatch="FUNCTION_VALUE"}
  }
 }
 p:=a.fset.PositionFor(c.Pos(),false)
 return record{"id":fmt.Sprintf("%s:%d:call",p.Filename,p.Offset),"source":a.span(c),
  "targetId":target,"calleeSymbolId":a.identity(obj),"dispatch":dispatch,
  "resolution":a.resolution(obj),"receiver":receiver,"signature":a.shape(a.info.TypeOf(c.Fun),0),
  "resultType":a.shape(a.info.TypeOf(c),0),"arguments":a.arguments(c),"spread":c.Ellipsis.IsValid()}
}

type callVisitor struct{ a *analysis; calls *[]record; owner string }
func(v callVisitor)Visit(node ast.Node)ast.Visitor{
 if node==nil{return nil};v.a.tick()
 switch n:=node.(type){
 case *ast.FuncDecl:v.owner=v.a.identity(v.a.info.Defs[n.Name])
 case *ast.FuncLit:
  p:=v.a.fset.PositionFor(n.Pos(),false);v.owner=fmt.Sprintf("source:%s:%d:literal",p.Filename,p.Offset)
 case *ast.CallExpr:
  result:=v.a.call(n);result["callerId"]=v.owner;*v.calls=append(*v.calls,result)
 }
 return v
}
