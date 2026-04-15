cd /Zongsoft/framework/upgrading/deployer && \
dotnet publish \
	--self-contained \
	--runtime linux-musl-x64 \
	--framework net10.0 \
	--configuration Release \
	-p:PublishAot=true \
	-p:NativeLinker=lld \
	-p:PublishAotUsingRuntimePack=true \
	-p:StripSymbols=false
