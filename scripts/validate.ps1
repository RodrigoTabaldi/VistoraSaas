param([switch]$WithoutDatabase)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot

function Invoke-Check {
    param([string]$Command, [string[]]$Arguments)
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Check failed: $Command $($Arguments -join ' ')"
    }
}

Push-Location $projectRoot
try {
    Invoke-Check docker @('compose', 'config', '--quiet')
    if (!$WithoutDatabase) {
        Invoke-Check docker @('info', '--format', '{{.ServerVersion}}')
    }
    Invoke-Check dotnet @('build', 'backend/Vistora.slnx', '--no-restore', '--configuration', 'Release')
    $testArguments = @('test', 'backend/Vistora.Tests/Vistora.Tests.csproj', '--no-build', '--no-restore', '--configuration', 'Release')
    if ($WithoutDatabase) {
        $testArguments += @('--filter', 'FullyQualifiedName!~Vistora.Tests.Integration')
    }
    Invoke-Check dotnet $testArguments
    Push-Location frontend
    try {
        Invoke-Check npm.cmd @('test')
        Invoke-Check npm.cmd @('run', 'typecheck')
        Invoke-Check npm.cmd @('run', 'build')
    }
    finally { Pop-Location }

    if ($WithoutDatabase) {
        Write-Warning 'Partial validation only: PostgreSQL migrations, RLS and concurrency were NOT tested.'
    }
    else {
        Write-Host 'Automated checks passed. Complete the functional checklist in infra/VALIDATION.md.'
    }
}
finally { Pop-Location }
