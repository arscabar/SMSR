package main

import (
 "go/ast"
 "go/importer"
 "go/token"
 "go/types"
 "io"
 "os"
 "path/filepath"
 "runtime"
)

func analyze(input request,exports string) record {
 fset:=token.NewFileSet()
 files,texts,valid:=parse(input,fset)
 info:=&types.Info{Types:map[ast.Expr]types.TypeAndValue{},
  Defs:map[*ast.Ident]types.Object{},Uses:map[*ast.Ident]types.Object{},
  Selections:map[*ast.SelectorExpr]*types.Selection{},Instances:map[*ast.Ident]types.Instance{}}
 errors:=0
 // Export data is generated from the trusted SDK, never from repository builds.
 lookup:=func(name string)(io.ReadCloser,error){
  if !validPath(name,"") { return nil,os.ErrPermission }
  return os.Open(filepath.Join(exports,filepath.FromSlash(name)+".a"))
 }
 config:=types.Config{GoVersion:"go1.23",Sizes:types.SizesFor("gc","amd64"),
  Importer:importer.ForCompiler(fset,"gc",lookup),
  Error:func(error){errors++;if errors>200 { panic("diagnostic limit") }}}
 _,err:=config.Check("smsr/input",fset,files,info)
 valid=valid && err==nil
 a:=analysis{fset:fset,texts:texts,info:info,valid:valid}
 declarations,references,calls:=[]record{},[]record{},[]record{}
 for _,f:=range files {
  ast.Inspect(f,func(node ast.Node)bool{
   a.tick()
   if id,ok:=node.(*ast.Ident);ok {
    if obj:=info.Defs[id];obj!=nil { declarations=append(declarations,a.symbol(id,obj)) }
    if obj:=info.Uses[id];obj!=nil {
     references=append(references,record{"source":a.span(id),"targetId":a.identity(obj),"resolution":a.resolution(obj)})
    }
   }
   return true
  })
  ast.Walk(callVisitor{a,&calls,""},f)
 }
 status:="COMPILER_BOUND_INPUT";if !valid { status="COMPILATION_ERRORS" }
 return record{"status":status,"runtime":runtime.Version(),"languageVersion":"go1.23","architecture":"amd64",
  "diagnosticCount":errors,"symbols":declarations,"references":references,"calls":calls,
  "limitations":[]string{"EXPLICIT_SINGLE_PACKAGE_FILES","BUILD_TAGS_NOT_FILTERED","NO_MODULE_OR_CGO_BUILDS",
   "STATIC_CALL_TARGETS_NOT_RUNTIME_DISPATCH","TYPE_SHAPES_REDACTED","NOT_PDG_OR_TAINT"}}
}
