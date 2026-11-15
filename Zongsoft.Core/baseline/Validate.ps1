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
		foreach($framework in @('net10.0', 'net11.0'))
		{
			Invoke-DotNet @(
				"bin/Release/$framework/Zongsoft.Core.Benchmarks.dll",
				'--filter', '*Spooler*',
				'--job', 'Dry',
				'--artifacts', (Join-Path $artifactRoot "benchmarks/$framework"),
				'--noOverwrite'
			)
		}
	}
	finally { Pop-Location }
}

Write-Output "Validation artifacts: $artifactRoot"
