package main

import (
 "go/ast"
 "go/parser"
 "go/token"
 "path"
 "sort"
 "strings"
 "unicode"
 "unicode/utf8"
)

func validPath(value,suffix string) bool {
 if len(value)==0 || len(value)>1024 || !utf8.ValidString(value) ||
  strings.ContainsAny(value,"\\:") || strings.HasPrefix(value,"/") ||
  !strings.HasSuffix(value,suffix) { return false }
 for _,r:=range value { if unicode.IsControl(r) { return false } }
 for _,part:=range strings.Split(value,"/") {
  if part=="" || part=="." || part==".." { return false }
 }
 return true
}

func parse(input request,fset *token.FileSet) ([]*ast.File,map[string]string,bool) {
 if len(input.Files)<1 || len(input.Files)>500 { panic("input count") }
 sort.Slice(input.Files,func(i,j int)bool{return input.Files[i].Path<input.Files[j].Path})
 files:=[]*ast.File{};texts:=map[string]string{};seen:=map[string]bool{}
 total:=0;valid:=true;directory:="";packageName:=""
 for index,s:=range input.Files {
  lower:=strings.ToLower(s.Path);total+=len(s.Text)
  if !validPath(s.Path,".go") || seen[lower] || len(s.Text)>2<<20 ||
   !utf8.ValidString(s.Text) || total>16<<20 { panic("source boundary") }
  seen[lower]=true;texts[s.Path]=s.Text
  if index==0 { directory=path.Dir(s.Path) }
  if directory!=path.Dir(s.Path) { panic("one package directory required") }
  f,err:=parser.ParseFile(fset,s.Path,s.Text,parser.SkipObjectResolution)
  if err!=nil { valid=false }
  if f==nil { continue }
  if packageName=="" { packageName=f.Name.Name }
  if f.Name.Name!=packageName { valid=false }
  files=append(files,f)
 }
 return files,texts,valid
}
