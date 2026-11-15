$ErrorActionPreference = 'Stop'

[string]$projectManifest = Join-Path $PSScriptRoot 'cake.projects'
[string]$projectAdministratives = Join-Path $PSScriptRoot '../Administratives/build.cake'
[string[]]$forwardedArguments = @($args)
$failedProjects = [System.Collections.Generic.List[string]]::new()

Get-Command dotnet -ErrorAction Stop | Out-Null

if(-not (Test-Path -LiteralPath $projectManifest -PathType Leaf))
{
	throw "The Cake project manifest was not found: $projectManifest"
}

$projects = Get-Content -LiteralPath $projectManifest |
	ForEach-Object { $_.Trim() } |
	Where-Object { $_ -and -not $_.StartsWith('#') }

if(Test-Path -LiteralPath $projectAdministratives -PathType Leaf)
{
	$projects = @($projects) + $projectAdministratives
}

Push-Location $PSScriptRoot

try
{
	foreach($project in $projects)
	{
		Write-Host "`n==> dotnet cake $project" -ForegroundColor Magenta
		$projectPath = Join-Path $PSScriptRoot $project
		$projectDirectory = (Resolve-Path -LiteralPath (Split-Path -Parent $projectPath)).Path
		$projectScript = Split-Path -Leaf $projectPath

		Push-Location $projectDirectory
		try
		{
			& dotnet cake $projectScript "--working=$projectDirectory" '--verbosity=normal' @forwardedArguments
		}
		finally
		{
			Pop-Location
		}

		if($LASTEXITCODE -ne 0)
		{
			$failedProjects.Add($project)
			Write-Host "<== Failed ($LASTEXITCODE): $project" -ForegroundColor Red
		}
	}
}
finally
{
	Pop-Location
}

if($failedProjects.Count -gt 0)
{
	Write-Host "The following Cake projects failed:" -ForegroundColor Red
	$failedProjects | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
	exit 1
}

Write-Host "`nAll Cake projects completed successfully." -ForegroundColor Green
