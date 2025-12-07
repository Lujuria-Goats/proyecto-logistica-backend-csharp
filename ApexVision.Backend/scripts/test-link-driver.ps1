param(
    [string]$AdminEmail = 'admin@apexvision.com',
    [string]$AdminPassword = 'Admin123!',
    [string]$Phone = '3001234567',
    [string]$BaseUrl = 'http://localhost:5132'
)

$ErrorActionPreference = 'Stop'
Write-Host "BaseUrl: $BaseUrl"

$loginPayload = @{ identifier = $AdminEmail; password = $AdminPassword }
$body = $loginPayload | ConvertTo-Json -Depth 6
Write-Host "\n=== LOGIN REQUEST ===\n$body\n"

try {
    $resp = Invoke-RestMethod -Uri "$BaseUrl/api/Auth/login" -Method Post -ContentType 'application/json' -Body $body
    Write-Host "=== LOGIN RESPONSE OBJECT ==="
    $resp | ConvertTo-Json -Depth 8 | Write-Host
} catch {
    Write-Host "LOGIN FAILED: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        try {
            $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host "RESPONSE BODY:"
            Write-Host $sr.ReadToEnd()
        } catch {}
    }
    exit 1
}

# Extraer token desde varios campos posibles
$token = $null
if ($resp -is [System.Collections.IDictionary]) {
    if ($resp.ContainsKey('token')) { $token = $resp.token }
    elseif ($resp.ContainsKey('accessToken')) { $token = $resp.accessToken }
    elseif ($resp.ContainsKey('data') -and $resp.data -and $resp.data.token) { $token = $resp.data.token }
}
if (-not $token -and $resp.token) { $token = $resp.token }
if (-not $token -and $resp.Token) { $token = $resp.Token }

if (-not $token) {
    Write-Host "NO TOKEN FOUND. Dumping response properties:"
    $resp | Format-List *
    exit 2
}

Write-Host "\n=== TOKEN (truncated) ==="
Write-Host ($token.Substring(0,[Math]::Min(60,$token.Length)))

# Llamada para vincular conductor
$linkPayload = @{ phoneNumber = $Phone } | ConvertTo-Json -Depth 4
Write-Host "\n=== LINK REQUEST ===\n$linkPayload\n"

try {
    $linkResp = Invoke-RestMethod -Uri "$BaseUrl/api/Drivers/link" -Method Post -Headers @{ Authorization = "Bearer $token" } -ContentType 'application/json' -Body $linkPayload
    Write-Host "=== LINK RESPONSE ==="
    $linkResp | ConvertTo-Json -Depth 8 | Write-Host
} catch {
    Write-Host "LINK FAILED: $($_.Exception.Message)"
    if ($_.Exception.Response) {
        try {
            $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            Write-Host "RESPONSE BODY:"
            Write-Host $sr.ReadToEnd()
        } catch {}
    }
    exit 3
}

Write-Host "\nTEST COMPLETED SUCCESSFULLY"
exit 0
