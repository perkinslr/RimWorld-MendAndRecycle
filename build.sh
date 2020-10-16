mcs -nostdlib -target:library -out:Assemblies/MendAndRecycle.dll `find . | grep cs$` `for f in ~/rwreference-1.2/*dll; do echo -r:$f; done;` $@
