$currentDir = Get-Location
$parentDir = Split-Path -Path $currentDir -Parent
$outputDir = "${currentDir}/publish/net10.0/linux-musl-x64"

New-Item -ItemType Directory -Path $outputDir -Force -ErrorAction SilentlyContinue

Write-Host "Building with Alpine image..." -ForegroundColor Cyan

podman run --rm `
    -v "${parentDir}:/source" `
    -v "${outputDir}:/output" `
    -w /source/upgrader `
    zongsoft-framework `
    dotnet publish `
        --self-contained `
        --runtime linux-musl-x64 `
        --framework net10.0 `
        --configuration Release `
        -p:PublishAot=true `
        -p:NativeLinker=lld `
        -p:PublishAotUsingRuntimePack=true `
        -p:StripSymbols=false

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nPublish completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: $outputDir" -ForegroundColor Green
} else {
    Write-Host "`nPublish failed with error code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}