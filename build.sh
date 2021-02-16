mcs $(find ./src/ -name "*.cs") -debug -nowarn:CS0219 -target:library -out:narchive.dll
