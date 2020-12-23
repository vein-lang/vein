#!/bin/dotnet run
#r "nuget: Newtonsoft.Json, 12.0.3"
using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

record OpCode(string n);

var content = File.ReadAllText("opcodes.def.json");
var ops = JsonConvert.DeserializeObject<OpCode[]>(content);

var result = new StringBuilder();

foreach(var i in ops.Select((x, y) => (x.n, y)))
    result.AppendLine($"OP_DEF({i.n.Replace(".", "_")}, 0x{i.y:X2})");

if (File.Exists("opcodes.def"))
    File.Delete("opcodes.def");
File.WriteAllText("opcodes.def", result.ToString());