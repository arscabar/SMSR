package main

import (
 "bytes"
 "encoding/json"
 "fmt"
 "io"
 "os"
 "path/filepath"
 "runtime/debug"
)

type record = map[string]any
type source struct { Path string `json:"path"`; Text string `json:"text"` }
type request struct { Files []source `json:"files"` }

func main() {
 debug.SetMemoryLimit(512 << 20)
 defer func() { if recover()!=nil { fail() } }()
 raw,err:=io.ReadAll(io.LimitReader(os.Stdin,32<<20+1))
 if err!=nil || len(raw)>32<<20 || !json.Valid(raw) { fail() }
 decoder:=json.NewDecoder(bytes.NewReader(raw));decoder.DisallowUnknownFields()
 var input request
 if decoder.Decode(&input)!=nil { fail() }
 executable,err:=os.Executable();if err!=nil { fail() }
 result:=analyze(input,filepath.Join(filepath.Dir(executable),"exports"))
 output,err:=json.Marshal(result)
 if err!=nil || len(output)>16<<20 { fail() }
 fmt.Print(string(output))
}

func fail() {
 fmt.Print(`{"error":"Go analysis failed: check input, helper and limits"}`)
 os.Exit(1)
}
