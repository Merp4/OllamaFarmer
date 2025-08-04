# Set current directory to the script's directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir
Set-Location ..

dotnet tool restore
dotnet ef migrations add $(Get-Date -Format "yyyyMMddHHmm") --context AppDbContext


Set-Location $scriptDir
