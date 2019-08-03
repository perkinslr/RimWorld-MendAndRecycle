mcs -langversion:Experimental -nostdlib -target:library -out:Assemblies/MendAndRecycle.dll `find . | grep cs$` `for f in $Reference/*dll; do echo -r:$f; done;`
