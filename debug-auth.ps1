#!/usr/bin/env pwsh

$api = "http://localhost:5132"

Write-Host "========== DEBUG AUTENTICACIÓN ==========" -ForegroundColor Yellow

# 1. Login
Write-Host "`n[1] Probando Login..." -ForegroundColor Cyan
$loginBody = @{
    email = "admin@apexvision.com"
    password = "Admin123!"
} | ConvertTo-Json

$loginResp = Invoke-WebRequest `
    -Uri "$api/api/auth/login" `
    -Method POST `
    -Headers @{ 'Content-Type' = 'application/json' } `
    -Body $loginBody

$adminToken = ($loginResp.Content | ConvertFrom-Json).token
Write-Host "✅ Admin login exitoso" -ForegroundColor Green
Write-Host "Token: $($adminToken.Substring(0, 50))..." -ForegroundColor Green

# Decodificar el token para ver los claims
Write-Host "`n[2] Decodificando token Admin..." -ForegroundColor Cyan
$parts = $adminToken.Split('.')
$payload = $parts[1]
$padding = 4 - ($payload.Length % 4)
if ($padding -ne 4) {
    $payload = $payload + ('=' * $padding)
}
$decoded = [System.Convert]::FromBase64String($payload)
$decodedString = [System.Text.Encoding]::UTF8.GetString($decoded)
Write-Host "Claims del token Admin:" -ForegroundColor Green
Write-Host ($decodedString | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor Green

# 2. Crear un Driver
Write-Host "`n[3] Registrando Driver..." -ForegroundColor Cyan
$driverEmail = "driver$(Get-Random)@test.com"
$registerBody = @{
    email = $driverEmail
    fullName = "Test Driver"
    password = "SecurePass123!"
    phoneNumber = "+573001234567"
    role = "Driver"
} | ConvertTo-Json

$registerResp = Invoke-WebRequest `
    -Uri "$api/api/auth/register" `
    -Method POST `
    -Headers @{ 'Content-Type' = 'application/json' } `
    -Body $registerBody

Write-Host "✅ Driver registrado: $driverEmail" -ForegroundColor Green
Write-Host ($registerResp.Content | ConvertFrom-Json | ConvertTo-Json) -ForegroundColor Green

# 3. Login con Driver
Write-Host "`n[4] Probando Login Driver..." -ForegroundColor Cyan
$driverLoginBody = @{
    email = $driverEmail
    password = "SecurePass123!"
} | ConvertTo-Json

$driverLoginResp = Invoke-WebRequest `
    -Uri "$api/api/auth/login" `
    -Method POST `
    -Headers @{ 'Content-Type' = 'application/json' } `
    -Body $driverLoginBody

$driverToken = ($driverLoginResp.Content | ConvertFrom-Json).token
Write-Host "✅ Driver login exitoso" -ForegroundColor Green
Write-Host "Token: $($driverToken.Substring(0, 50))..." -ForegroundColor Green

# Decodificar el token del driver
Write-Host "`n[5] Decodificando token Driver..." -ForegroundColor Cyan
$parts = $driverToken.Split('.')
$payload = $parts[1]
$padding = 4 - ($payload.Length % 4)
if ($padding -ne 4) {
    $payload = $payload + ('=' * $padding)
}
$decoded = [System.Convert]::FromBase64String($payload)
$decodedString = [System.Text.Encoding]::UTF8.GetString($decoded)
Write-Host "Claims del token Driver:" -ForegroundColor Green
Write-Host ($decodedString | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor Green

# 4. Probar endpoint de rutas guardadas
Write-Host "`n[6] Probando GET /api/Routes/saved con token Driver..." -ForegroundColor Cyan
try {
    $routesResp = Invoke-WebRequest `
        -Uri "$api/api/Routes/saved" `
        -Method GET `
        -Headers @{ 
            'Authorization' = "Bearer $driverToken"
            'Content-Type' = 'application/json'
        } `
        -ErrorAction Stop
    
    Write-Host "✅ GET /api/Routes/saved EXITOSO" -ForegroundColor Green
    Write-Host ($routesResp.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor Green
}
catch {
    Write-Host "❌ GET /api/Routes/saved FALLO" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Mostrar detalles del error
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "Detalle: $errorBody" -ForegroundColor Yellow
    } catch {}
}

# 5. Probar endpoint de mi ruta
Write-Host "`n[7] Probando GET /api/Orders/my-route con token Driver..." -ForegroundColor Cyan
try {
    $myRouteResp = Invoke-WebRequest `
        -Uri "$api/api/Orders/my-route" `
        -Method GET `
        -Headers @{ 
            'Authorization' = "Bearer $driverToken"
            'Content-Type' = 'application/json'
        } `
        -ErrorAction Stop
    
    Write-Host "✅ GET /api/Orders/my-route EXITOSO" -ForegroundColor Green
    Write-Host ($myRouteResp.Content | ConvertFrom-Json | ConvertTo-Json -Depth 10) -ForegroundColor Green
}
catch {
    Write-Host "❌ GET /api/Orders/my-route FALLO" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Mostrar detalles del error
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "Detalle: $errorBody" -ForegroundColor Yellow
    } catch {}
}

