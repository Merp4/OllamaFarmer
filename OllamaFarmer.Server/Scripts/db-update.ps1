# Set current directory to the script's directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
Set-Location $scriptDir
Set-Location ..

dotnet tool restore
dotnet ef database update

# Add migrations like:
#	dotnet ef migrations add
#	dotnet ef migrations add InitialCreate --project ../OllamaFarmer.Server/OllamaFarmer.Server.csproj


Set-Location $scriptDir
