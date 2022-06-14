#!/usr/bin/env pwsh
# Helper script for applying the same user secrets to each project
# Duplicated from the server repo
param (
    [bool]$clear,
    [Parameter(ValueFromRemainingArguments = $true, Position=1)]
    $cmdArgs
)

if (!(Test-Path "secrets.json")) {
    Write-Warning "No secrets.json file found, please copy and modify the provided example";
    exit;
}

if ($clear -eq $true) {
    Write-Output "Deleting all existing user secrets"
}

$projects = @{
    KeyConnector = "../src/KeyConnector"
}

foreach ($key in $projects.keys) {
    if ($clear -eq $true) {
        dotnet user-secrets clear -p $projects[$key]
    }
    $output = Get-Content secrets.json | & dotnet user-secrets set -p $projects[$key]
    Write-Output "$output - $key"
}
