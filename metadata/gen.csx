#r "nuget: Newtonsoft.Json, 12.0.3"
using System;
using System.IO;
using Newtonsoft.Json;
class OpCode {
    public string v;
    public string n;
}

var content = File.ReadAllText("opcodes.def.json");
var ops = JsonConvert.DeserializeObject<OpCode[]>(content);

var result = new StringBuilder();

foreach(var i in ops)
    result.AppendLine($"{i.n.Replace(".", "_")} = {i.v},");

if (File.Exists("opcodes.def"))
    File.Delete("opcodes.def");
File.WriteAllText("opcodes.def", result.ToString());