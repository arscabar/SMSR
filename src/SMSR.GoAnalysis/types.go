package main

import "go/types"

// Do not serialize Type.String(): it can include source constants and struct tags.
func(a *analysis)shape(t types.Type,depth int)any {
 a.tick()
 if t==nil{return nil};if depth>8{return record{"kind":"DEPTH_LIMIT"}}
 child:=func(t types.Type)any{return a.shape(t,depth+1)}
 named:=func(o *types.TypeName,args *types.TypeList)any{
  pkg:="";if o.Pkg()!=nil{pkg=o.Pkg().Path()}
  list:=[]any{};if args!=nil{for i:=0;i<args.Len();i++{list=append(list,child(args.At(i)))}}
  return record{"kind":"NAMED","name":o.Name(),"package":pkg,"arguments":list}
 }
 switch v:=t.(type){
 case *types.Basic:return record{"kind":"BASIC","name":v.Name()}
 case *types.Named:return named(v.Obj(),v.TypeArgs())
 case *types.Alias:return named(v.Obj(),v.TypeArgs())
 case *types.TypeParam:return record{"kind":"TYPE_PARAMETER","name":v.Obj().Name()}
 case *types.Pointer:return record{"kind":"POINTER","element":child(v.Elem())}
 case *types.Slice:return record{"kind":"SLICE","element":child(v.Elem())}
 case *types.Array:return record{"kind":"ARRAY","element":child(v.Elem())}
 case *types.Map:return record{"kind":"MAP","key":child(v.Key()),"element":child(v.Elem())}
 case *types.Chan:return record{"kind":"CHANNEL","direction":int(v.Dir()),"element":child(v.Elem())}
 case *types.Signature:
  tuple:=func(t *types.Tuple)[]any{list:=[]any{};for i:=0;i<t.Len();i++{list=append(list,child(t.At(i).Type()))};return list}
  return record{"kind":"SIGNATURE","parameters":tuple(v.Params()),"results":tuple(v.Results()),"variadic":v.Variadic()}
 case *types.Struct:return record{"kind":"STRUCT","fieldCount":v.NumFields()}
 case *types.Interface:return record{"kind":"INTERFACE","methodCount":v.NumMethods()}
 default:return record{"kind":"OTHER"}
 }
}
