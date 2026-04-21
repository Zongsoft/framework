podman exec ^
    -w /Zongsoft/framework/upgrading/deployer ^
    zongsoft-framework           ^
    dotnet publish               ^
        --self-contained         ^
        --runtime linux-musl-x64 ^
        --framework net10.0      ^
        --configuration Release  ^
        -p:PublishAot=true       ^
        -p:NativeLinker=lld      ^
        -p:StripSymbols=false    ^
        -p:PublishAotUsingRuntimePack=true
