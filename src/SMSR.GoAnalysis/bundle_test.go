package main

import "testing"

func TestCrossFile(t *testing.T){
 r:=analyze(request{Files:[]source{
  {"pkg/entry.go",`package sample;func Run() int{return Twice(3)}`},
  {"pkg/lib.go",`package sample;func Twice(x int) int{return x*2}`},
 }},"")
 if r["status"]!="COMPILER_BOUND_INPUT"{t.Fatal("bundle failed")}
 var target string
 for _,s:=range r["symbols"].([]record){if s["name"]=="Twice"{target=s["id"].(string)}}
 calls:=r["calls"].([]record)
 if target=="" || len(calls)!=1 || calls[0]["targetId"]!=target{t.Fatal("cross-file target missing")}
 args:=calls[0]["arguments"].([]record)
 if len(args)!=1 || args[0]["parameterIndex"]!=0 || args[0]["mode"]!="POSITIONAL"{t.Fatal("parameter mapping missing")}
}

func TestShadow(t *testing.T){
 r:=fixture(t,`package sample
func Target(x int)int{return x}
func F(Target func(int)int)int{return Target(1)}
`)
 if r["status"]!="COMPILER_BOUND_INPUT"{t.Fatal("shadow fixture invalid")}
 c:=r["calls"].([]record)[0]
 if c["dispatch"]!="FUNCTION_VALUE" || c["targetId"]!=""{t.Fatal("same-name function falsely bound")}
}
