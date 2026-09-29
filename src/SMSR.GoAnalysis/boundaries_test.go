package main

import (
 "encoding/json"
 "go/token"
 "strings"
 "testing"
)

func TestBoundaries(t *testing.T){
 for _,text:=range []string{`package sample;func F(){missing("SECRET_ERROR")}`,
  `package sample;import bad "example.invalid/package";func F(){bad.F()}`,
  `package sample;import "C"`, `package sample;func Broken(`}{
  r:=fixture(t,text)
  if r["status"]!="COMPILATION_ERRORS"{t.Fatal("invalid input resolved")}
  raw,_:=json.Marshal(r)
  if strings.Contains(string(raw),"SECRET_ERROR"){t.Fatal("diagnostic source exposed")}
 }
 for _,input:=range []request{{}, {Files:[]source{{"../a.go","package p"}}},
  {Files:[]source{{"a.go","package p"},{"A.go","package p"}}},
  {Files:[]source{{"a.go","package p"},{"dir/b.go","package p"}}}}{
  func(){defer func(){if recover()==nil{t.Fatal("invalid boundary accepted")}}()
   parse(input,token.NewFileSet())
  }()
 }
 r:=fixture(t,"package sample\r\n//line secret.go:900\r\nfunc F(){ _ = \"😀\"; 값:=1; _=값 }\r\n")
 found:=false
 for _,s:=range r["symbols"].([]record){
  if s["name"]!="값"{continue}
  found=true
  span:=s["source"].(record);start:=span["start"].(record)
  if span["path"]!="input.go" || start["line"]!=2 || start["character"]!=20{t.Fatal("physical UTF16 position incorrect",span)}
 }
 if !found{t.Fatal("Unicode declaration missing")}
 raw,_:=json.Marshal(r);if len(raw)==0{t.Fatal("missing output")}
}

func TestArgumentModes(t *testing.T){
 r:=fixture(t,`package sample
func Pair()(int,int){return 1,2}
func Add(a,b int)int{return a+b}
func Many(x ...int){}
func F(x []int){_ = Add(Pair());Many(1,2);Many(x...)}
`)
 modes:=map[string]int{}
 for _,c:=range r["calls"].([]record){for _,arg:=range c["arguments"].([]record){modes[arg["mode"].(string)]++}}
 if modes["TUPLE_EXPANSION"]!=1 || modes["VARIADIC_ELEMENT"]!=2 || modes["VARIADIC_SLICE"]!=1{t.Fatal("argument boundary missing",modes)}
}
