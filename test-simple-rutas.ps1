#!/usr/bin/env powershell
# TEST SIMPLIFICADO

$api = "http://localhost:5132"
Write-Host "TEST DE GUARDAR RUTAS" -ForegroundColor Cyan

# 1. Login Admin
Write-Host "`n1. Login Admin..." -ForegroundColor Yellow
$login = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login
$adminToken = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ OK" -ForegroundColor Green

# 2. Crear pedido simple
Write-Host "`n2. Crear pedido..." -ForegroundColor Yellow
$order = @{
    Description = "Test"
    Address = "Test Address"
    Latitude = 6.22
    Longitude = -75.53
    RequiresEvidence = $false
} | ConvertTo-Json

Write-Host "Enviando: $order" -ForegroundColor Gray
$resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $adminToken"} -ContentType "application/json" -Body $order
$orderId = ($resp.Content | ConvertFrom-Json).orderId
Write-Host "✅ Pedido creado: $orderId" -ForegroundColor Green

# 3. Registrar driver
Write-Host "`n3. Registrar driver..." -ForegroundColor Yellow
$reg = @{
    fullName="TestDriver"
    email="testdriver@test.com"
    password="Pass@123456!"
    phoneNumber="+573001234567"
    role="Driver"
} | ConvertTo-Json

Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $reg -ErrorAction SilentlyContinue | Out-Null
Write-Host "✅ OK" -ForegroundColor Green

# 4. Login driver
Write-Host "`n4. Login driver..." -ForegroundColor Yellow
$dlogin = @{email="testdriver@test.com"; password="Pass@123456!"} | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $dlogin
$dToken = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ OK" -ForegroundColor Green

# 5. Asignar pedido
Write-Host "`n5. Asignar pedido al driver..." -ForegroundColor Yellow
Invoke-WebRequest -Uri "$api/api/Orders/$orderId/assign/1" -Method PUT -Headers @{"Authorization"="Bearer $adminToken"} | Out-Null
Write-Host "✅ OK" -ForegroundColor Green

# 6. GUARDAR RUTA
Write-Host "`n6. GUARDAR RUTA..." -ForegroundColor Yellow
$save = @{
    routeName = "Mi Primera Ruta"
    orderIds = @($orderId)
} | ConvertTo-Json

Write-Host "Enviando: $save" -ForegroundColor Gray

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $dToken"} -ContentType "application/json" -Body $save -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    Write-Host "✅ RUTA GUARDADA" -ForegroundColor Green
    Write-Host "   ID: $($content.routeId)" -ForegroundColor Green
    Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Green
    Write-Host "   Respuesta completa: $($resp.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

# 7. VER RUTAS
Write-Host "`n7. VER RUTAS GUARDADAS..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" -Method GET -Headers @{"Authorization"="Bearer $dToken"} -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    Write-Host "✅ OK" -ForegroundColor Green
    Write-Host "   Total: $($content.totalSavedRoutes)" -ForegroundColor Green
    Write-Host "   Respuesta: $($resp.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
}

Write-Host "`n✅ TEST COMPLETADO" -ForegroundColor Green

