# Get the script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition

Write-Host ""
Write-Host "## INFO --> Building Key Connector"

$dotnetVersion = dotnet --version
Write-Host ".NET Core version $dotnetVersion"

Write-Host "Restore"
dotnet restore "$ScriptDir/KeyConnector.csproj"

Write-Host "Clean"
dotnet clean "$ScriptDir/KeyConnector.csproj" -c "Release" -o "$ScriptDir/obj/build-output/publish"

Write-Host "Publish"
dotnet publish "$ScriptDir/KeyConnector.csproj" -c "Release" -o "$ScriptDir/obj/build-output/publish"

Write-Host ""
Write-Host "## INFO --> Building docker image"
docker --version
docker build -t bitwarden/key-connector "$ScriptDir\."
