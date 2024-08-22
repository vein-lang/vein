rm  -r -fo ./compiler/bin
rm  -r -fo ./compiler/obj
rm  -r -fo ./runtime/bin
rm  -r -fo ./runtime/obj

cd ./compiler
& "C:\git\vein.lang\tools\compiler\bin\Debug\net8.0\veinc.exe" build --gen-shard
rune publish
cd ..
cd ./runtime
& "C:\git\vein.lang\tools\compiler\bin\Debug\net8.0\veinc.exe" build --gen-shard
rune publish
cd ..