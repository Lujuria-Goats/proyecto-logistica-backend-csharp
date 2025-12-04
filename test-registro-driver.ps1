#!/usr/bin/env powershell
# Test de Registro - Simplificado

$api = "http://localhost:5132"

Write-Host "====== TEST DE REGISTRO ======" -ForegroundColor Cyan

# Test 1: Login Admin (debe funcionar)
Write-Host "`n[TEST 1] Login Admin..." -ForegroundColor Yellow
$adminLogin = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $adminLogin -ErrorAction Stop
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO - Token obtenido" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# Test 2: Registrar Driver
Write-Host "`n[TEST 2] Registrar Driver..." -ForegroundColor Yellow
$driverReg = @{
    fullName="Test Driver"
    email="testdriver999@test.com"
    password="Pass@123456!"
    phoneNumber="3001234567"
    role="Driver"
} | ConvertTo-Json

Write-Host "Enviando: $driverReg" -ForegroundColor Gray

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $driverReg -ErrorAction Stop
    Write-Host "✅ EXITOSO" -ForegroundColor Green
    Write-Host "Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Mensaje: $($_.Exception.Message)" -ForegroundColor Red
    
    # Intentar obtener body del error
    try {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $errorReader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $errorReader.ReadToEnd()
        Write-Host "Body del error: $errorBody" -ForegroundColor Red
    } catch {}
    exit 1
}

# Test 3: Login del Driver recién registrado
Write-Host "`n[TEST 3] Login del Driver..." -ForegroundColor Yellow
$driverLogin = @{
    email="testdriver999@test.com"
    password="Pass@123456!"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin -ErrorAction Stop
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO - Driver autenticado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "No se puede hacer login con las credenciales registradas" -ForegroundColor Red
    exit 1
}

Write-Host "`n====== TODOS LOS TESTS PASARON ======" -ForegroundColor Green

