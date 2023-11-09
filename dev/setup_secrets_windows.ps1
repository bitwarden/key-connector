param (
    [bool]$clear,
    [Parameter(ValueFromRemainingArguments = $true, Position=1)]
    $cmdArgs
)

# Try to Fetch Certificate
$Certificate = Get-ChildItem -Path cert:\LocalMachine\My | Where-Object { $_.Subject -like "*Bitwarden Key Connector*" } | Select-Object Thumbprint, Subject

if ($($Certificate.Thumbprint)) {
    Write-Host "## INFO --> Found Bitwarden Key Connector certificate : $($Certificate.Thumbprint)"
}
else {
    Write-Host "## INFO --> Creating Bitwarden Key Connector certificate..."
    try {
        # Create Key Connector Certificate
        New-SelfSignedCertificate -DnsName "Bitwarden Key Connector" -CertStoreLocation Cert:\LocalMachine\My -KeySpec Signature -KeyUsage DigitalSignature -KeyExportPolicy Exportable -Subject "CN=Bitwarden Key Connector" -NotBefore (Get-Date) -NotAfter (Get-Date).AddDays(36500)
    }
    catch {
        Write-Host "## ERROR --> An exception occurred: $_.Exception.Message"
        exit 1
    }
    Write-Host "## INFO --> Certificate created successfully"

    # Fetch newly created certificate
    $Certificate = Get-ChildItem -Path cert:\LocalMachine\My | Where-Object { $_.Subject -like "*Bitwarden Key Connector*" } | Select-Object Thumbprint, Subject
    
    # Adding a check to make sure the certificate exists to ensure no error on creation
    if ($null -eq $($Certificate.Thumbprint) -or "" -eq $($Certificate.Thumbprint)) {
        Write-Host "## INFO: Certificate not found"
        exit 1
    }
}

# Prompt the user for input (e.g., password)
$password = Read-Host "## INPUT --> Enter password for private key"
if ($null -ne $password -and "" -ne $password) {
    $SecureStringPassword = ConvertTo-SecureString -String $password -AsPlainText -Force
    Export-PfxCertificate -Cert cert:\LocalMachine\My\$($Certificate.Thumbprint) -FilePath .\bwkc.pfx -Password $SecureStringPassword | Out-Null
}
else {
    Write-Host "## ERROR: Password cannot be null or empty"
    exit 1
}

$pathToPFX = (Get-Item -Path ".\bwkc.pfx").FullName
Write-Host "## INFO --> Exported certificate to $pathToPFX"

# read secrets.json
Write-Host "## INFO --> creating secrets.json from secrets.json.example"
$secrets = Get-Content .\secrets.json.example | ConvertFrom-Json

# set PFX password
$secrets.keyConnectorSettings.certificate.filesystemPassword = $password
Write-Host "## INFO --> Certificate password set successfully in secrets.json"

# set PFX path
$secrets.keyConnectorSettings.certificate.filesystemPath = $pathToPFX
Write-Host "## INFO --> Path to bwkc.pfx set successfully in secrets.json"

# set database.json path
$pathToDatabase = $pathToPFX.Replace("bwkc.pfx", "database.json")
$secrets.keyConnectorSettings.database.jsonFilePath = $pathToDatabase
Write-Host "## INFO --> Path to database.json set successfully in secrets.json"

# save secrets.json
$secrets | ConvertTo-Json | Set-Content secrets.json

# set secrets
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

Write-Host "## INFO --> Setting secrets for each project"
foreach ($key in $projects.keys) {
    if ($clear -eq $true) {
        dotnet user-secrets clear -p $projects[$key]
    }
    $output = Get-Content secrets.json | & dotnet user-secrets set -p $projects[$key]
    Write-Output "$output - $key"
}
