param(
	[switch]$SkipBenchmarks,
	[string]$ArtifactPath = (Join-Path ([IO.Path]::GetTempPath()) ('zongsoft-core-validation-' + [Guid]::NewGuid().ToString('N')))
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceProject = Join-Path $projectRoot 'src\Zongsoft.Core.csproj'
$testProject = Join-Path $projectRoot 'test\unit\Zongsoft.Core.Tests.csproj'
$benchmarkProject = Join-Path $projectRoot 'benchmark\Zongsoft.Core.Benchmarks.csproj'
$artifactRoot = [IO.Path]::GetFullPath($ArtifactPath)

function Invoke-DotNet([string[]]$Arguments)
{
	& dotnet @Arguments

	if($LASTEXITCODE -ne 0)
	{
		throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
	}
}

Invoke-DotNet @(
	'build', $sourceProject,
	'-c', 'Release',
	'-p:GeneratePackageOnBuild=false'
)

Invoke-DotNet @(
	'test', $testProject,
	'-c', 'Release',
	'--verbosity', 'minimal',
	'--blame-hang-timeout', '2m',
	'--results-directory', (Join-Path $artifactRoot 'tests'),
	'-p:GeneratePackageOnBuild=false'
)

Invoke-DotNet @(
	'pack', $sourceProject,
	'-c', 'Release',
	'--no-build', '--no-restore',
	'-p:EnablePackageValidation=true',
	"-p:PackageOutputPath=$(Join-Path $artifactRoot 'packages')"
)

if(!$SkipBenchmarks)
{
	Invoke-DotNet @(
		'build', $benchmarkProject,
		'-c', 'Release',
		'--verbosity', 'minimal',
		'-p:GeneratePackageOnBuild=false'
	)

	Push-Location (Split-Path -Parent $benchmarkProject)
	try
	{
		Invoke-DotNet @(
			'bin/Release/net10.0/Zongsoft.Core.Benchmarks.dll',
			'--filter', '*Spooler*',
			'--job', 'Dry',
			'--runtimes', 'net8.0', 'net9.0', 'net10.0',
			'--artifacts', (Join-Path $artifactRoot 'benchmarks'),
			'--noOverwrite'
		)
	}
	finally { Pop-Location }
}

Write-Output "Validation artifacts: $artifactRoot"
