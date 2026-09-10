param(
    [ValidateSet('Debug','Release')][string]$Configuration = 'Debug',
    [ValidateSet('x64','x86','ARM64')][string]$Platform = 'x64',
    [switch]$IncludeTests
)
$ErrorActionPreference = 'Stop'
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio/Installer/vswhere.exe'
$visualStudio = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -property installationPath
if (!$visualStudio) { throw 'Visual Studio 2026 with UWP and C++ tools is required.' }
Import-Module (Join-Path $visualStudio 'Common7/Tools/Microsoft.VisualStudio.DevShell.dll')
Enter-VsDevShell -VsInstallPath $visualStudio -SkipAutomaticLocation -DevCmdArguments "-arch=$($Platform.ToLowerInvariant()) -host_arch=x64"
if (!(Get-Command link.exe -ErrorAction SilentlyContinue)) { throw "C++ tools for $Platform are required for the audio effect component." }
$target = if ($IncludeTests) { 'Build' } else { 'UniversalSoundboard_Packaging' }
Push-Location $PSScriptRoot
try {
    & (Join-Path $visualStudio 'MSBuild/Current/Bin/MSBuild.exe') UniversalSoundBoard.sln /restore "/t:$target" "/p:Configuration=$Configuration" "/p:Platform=$Platform" "/p:RuntimeIdentifier=win-$($Platform.ToLowerInvariant())" /p:IlcUseEnvironmentalTools=true /p:AppxPackageSigningEnabled=false /p:AppxBundle=Never /p:UapAppxPackageBuildMode=SideloadOnly /v:minimal /nologo
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally { Pop-Location }
