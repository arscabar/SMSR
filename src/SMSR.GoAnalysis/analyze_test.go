package main

import (
 "encoding/json"
 "path/filepath"
 "strings"
 "testing"
)

func fixture(t *testing.T,text string)record{
 t.Helper()
 return analyze(request{Files:[]source{{"input.go",text}}},filepath.Join("..","..","artifacts","go-analysis","exports"))
}

func TestCompiler(t *testing.T){
 text:=`package sample
type Box struct { Value int `+"`json:\"SECRET_TAG\"`"+` }
func (b Box) Get() int { return b.Value }
type Getter interface { Get() int }
func Identity[T any](x T) T { return x }
func Many(x ...int) int { return len(x) }
func Use(b Box, i Getter, f func(int)int) int {
 a:=Identity(1); d:=b.Get(); e:=i.Get(); g:=f(a)
 return Many(a,d,e,g)
}
const secret="SECRET_LITERAL"
func init(){panic("TARGET_MUST_NOT_RUN")}
`
 r:=fixture(t,text)
 if r["status"]!="COMPILER_BOUND_INPUT"{t.Fatal("valid input rejected",r["diagnosticCount"])}
 raw,_:=json.Marshal(r)
 for _,value:=range []string{"SECRET_TAG","SECRET_LITERAL","TARGET_MUST_NOT_RUN"}{
  if strings.Contains(string(raw),value){t.Fatal("source constant exposed")}
 }
 dispatch:=map[string]int{}
 for _,c:=range r["calls"].([]record){
  dispatch[c["dispatch"].(string)]++
  if c["callerId"]==""{t.Fatal("caller missing")}
  if c["dispatch"]=="FUNCTION_VALUE" && c["targetId"]!=""{t.Fatal("function value target invented")}
 }
 for _,kind:=range []string{"DIRECT_FUNCTION","DIRECT_METHOD","INTERFACE_DISPATCH","FUNCTION_VALUE","BUILTIN"}{
  if dispatch[kind]==0{t.Fatal("missing dispatch",kind,dispatch)}
 }
 declarations:=map[string]bool{}
 for _,s:=range r["symbols"].([]record){declarations[s["id"].(string)]=true}
 for _,c:=range r["calls"].([]record){
  if strings.HasPrefix(c["targetId"].(string),"source:") && !declarations[c["targetId"].(string)]{t.Fatal("dangling call target")}
 }
 raw2,_:=json.Marshal(fixture(t,text));if string(raw)!=string(raw2){t.Fatal("nondeterministic facts")}
}

func TestMetadata(t *testing.T){
 r:=fixture(t,`package sample
import "strings"
func Trim(s string) string {return strings.TrimSpace(s)}
`)
 if r["status"]!="COMPILER_BOUND_INPUT"{t.Fatal("standard metadata missing")}
 c:=r["calls"].([]record)[0]
 if c["targetId"]!="metadata:strings:TrimSpace" || c["resolution"]!="BOUND_METADATA"{t.Fatal("standard target incorrect")}
}
