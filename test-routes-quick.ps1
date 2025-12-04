#!/usr/bin/env powershell
# Test Rápido de Endpoints

$api = "http://localhost:5132"

Write-Host "TEST ENDPOINTS NUEVOS - ApexVision" -ForegroundColor Cyan
Write-Host ""

# 1. Login
Write-Host "1. LOGIN..." -ForegroundColor Yellow
try {
    $login = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login -ErrorAction Stop
    $token = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ OK - Token: $($token.Substring(0,20))..." -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

# 2. Crear pedidos
Write-Host "`n2. CREAR PEDIDOS..." -ForegroundColor Yellow
$orderIds = @()
for ($i = 1; $i -le 2; $i++) {
    $order = @{
        address = "Dir $i"
        latitude = 6.2 + ($i * 0.01)
        longitude = -75.5
        description = "Pedido $i"
        requiresEvidence = $false
    } | ConvertTo-Json
    
    $resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $token"} -ContentType "application/json" -Body $order -ErrorAction Stop
    $oid = ($resp.Content | ConvertFrom-Json).orderId
    $orderIds += $oid
}
Write-Host "✅ OK - Pedidos: $orderIds" -ForegroundColor Green

# 3. Registrar Driver
Write-Host "`n3. REGISTRAR DRIVER..." -ForegroundColor Yellow
try {
    $reg = @{
        fullName="Test Driver"
        email="driver@test.com"
        password="Driver123!"
        phoneNumber="+573001111111"
        role="Driver"
    } | ConvertTo-Json
    
    Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $reg -ErrorAction Stop | Out-Null
    Write-Host "✅ OK - Driver registrado" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Driver ya existe" -ForegroundColor Yellow
}

# 4. Login Driver
Write-Host "`n4. LOGIN DRIVER..." -ForegroundColor Yellow
$dlogin = @{email="driver@test.com"; password="Driver123!"} | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $dlogin -ErrorAction Stop
$dtoken = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ OK" -ForegroundColor Green

# 5. Asignar pedidos
Write-Host "`n5. ASIGNAR PEDIDOS A DRIVER..." -ForegroundColor Yellow
foreach ($oid in $orderIds) {
    Invoke-WebRequest -Uri "$api/api/Orders/$oid/assign/1" -Method PUT -Headers @{"Authorization"="Bearer $token"} -ErrorAction Stop | Out-Null
}
Write-Host "✅ OK - Pedidos asignados" -ForegroundColor Green

# 6. PROBAR GUARDAR RUTA
Write-Host "`n6. GUARDAR RUTA..." -ForegroundColor Yellow
try {
    $save = @{
        routeName = "Mi Ruta de Test"
        orderIds = $orderIds
    } | ConvertTo-Json
    
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $dtoken"} -ContentType "application/json" -Body $save -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    $rid = $content.routeId
    Write-Host "✅ OK - Ruta ID: $rid" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Code: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
}

# 7. VER RUTAS
Write-Host "`n7. VER RUTAS GUARDADAS..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" -Method GET -Headers @{"Authorization"="Bearer $dtoken"} -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    Write-Host "✅ OK - Total rutas: $($content.totalSavedRoutes)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Message)" -ForegroundColor Red
}

# 8. CARGAR RUTA
Write-Host "`n8. CARGAR RUTA..." -ForegroundColor Yellow
if ($rid) {
    try {
        $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$rid/load" -Method POST -Headers @{"Authorization"="Bearer $dtoken"} -ErrorAction Stop
        $content = $resp.Content | ConvertFrom-Json
        Write-Host "✅ OK - Ruta: $($content.routeName)" -ForegroundColor Green
    } catch {
        Write-Host "❌ FALLO: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "TESTS COMPLETADOS" -ForegroundColor Green

