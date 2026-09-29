package main

import (
 "go/ast"
 "go/types"
)

func(a *analysis)arguments(c *ast.CallExpr)[]record{
 result:=[]record{}
 signature,_:=a.info.TypeOf(c.Fun).(*types.Signature)
 for index,arg:=range c.Args {
  a.tick();mode:="UNRESOLVED";var parameter any
  if signature!=nil {
   count:=signature.Params().Len()
   if tuple,ok:=a.info.TypeOf(arg).(*types.Tuple);ok && tuple.Len()>1 {
    mode="TUPLE_EXPANSION"
   }else if signature.Variadic() && index>=count-1 {
    parameter=count-1;mode="VARIADIC_ELEMENT";if c.Ellipsis.IsValid(){mode="VARIADIC_SLICE"}
   }else if index<count {parameter=index;mode="POSITIONAL"}
  }
  result=append(result,record{"index":index,"parameterIndex":parameter,"mode":mode,
   "source":a.span(arg),"type":a.shape(a.info.TypeOf(arg),0)})
 }
 return result
}
