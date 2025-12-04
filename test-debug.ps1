#!/usr/bin/env powershell
# Test Completo con Todos los Detalles

$api = "http://localhost:5132"

Write-Host "====== TEST ENDPOINTS APEXVISION ======" -ForegroundColor Cyan

# Verificar que la API está viva
Write-Host "`n1. VERIFICAR API EN VIVO..." -ForegroundColor Yellow
try {
    $test = Invoke-WebRequest -Uri "$api/swagger/index.html" -ErrorAction Stop
    Write-Host "✅ API VIVA - Status: $($test.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "❌ API NO RESPONDE" -ForegroundColor Red
    exit
}

# Login Admin
Write-Host "`n2. LOGIN ADMIN..." -ForegroundColor Yellow
$login = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login
$token = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ Token: $($token.Substring(0,30))..." -ForegroundColor Green

# Crear 2 pedidos
Write-Host "`n3. CREAR PEDIDOS..." -ForegroundColor Yellow
$oids = @()
for ($i = 1; $i -le 2; $i++) {
    $o = @{address="D$i"; latitude=6.2; longitude=-75.5; description="P$i"; requiresEvidence=$false} | ConvertTo-Json
    $r = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $token"} -ContentType "application/json" -Body $o
    $oid = ($r.Content | ConvertFrom-Json).orderId
    $oids += $oid
    Write-Host "  Pedido $oid ✅" -ForegroundColor Green
}

# Registrar Driver
Write-Host "`n4. REGISTRAR DRIVER..." -ForegroundColor Yellow
$reg = @{fullName="Test";email="driver@test.com";password="Driver123!";phoneNumber="+573001111111";role="Driver"} | ConvertTo-Json
try {
    Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $reg -ErrorAction Stop | Out-Null
} catch {}
Write-Host "✅ OK" -ForegroundColor Green

# Login Driver
Write-Host "`n5. LOGIN DRIVER..." -ForegroundColor Yellow
$dlog = @{email="driver@test.com"; password="Driver123!"} | ConvertTo-Json
$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $dlog
$dtoken = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ Token: $($dtoken.Substring(0,30))..." -ForegroundColor Green

# Asignar pedidos
Write-Host "`n6. ASIGNAR PEDIDOS..." -ForegroundColor Yellow
foreach ($oid in $oids) {
    Invoke-WebRequest -Uri "$api/api/Orders/$oid/assign/1" -Method PUT -Headers @{"Authorization"="Bearer $token"} -ErrorAction Stop | Out-Null
    Write-Host "  Pedido $oid asignado ✅" -ForegroundColor Green
}

# PROBAR /api/Routes/save
Write-Host "`n7. GUARDAR RUTA (POST /api/Routes/save)..." -ForegroundColor Yellow
$save = @{routeName="Mi Ruta";orderIds=$oids} | ConvertTo-Json
Write-Host "   Body: $save" -ForegroundColor Gray

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $dtoken"} -ContentType "application/json" -Body $save -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    $rid = $content.routeId
    Write-Host "✅ EXITOSO - ID: $rid" -ForegroundColor Green
    Write-Host "   Respuesta: $($resp.Content)" -ForegroundColor Gray
} catch {
    Write-Host "❌ ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    if ($_.Exception.Response.StatusCode -eq 404) {
        Write-Host "   >>> ENDPOINT NO ENCONTRADO (404)" -ForegroundColor Red
    }
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "   >>> NO AUTORIZADO (401)" -ForegroundColor Red
    }
}

Write-Host "`n====== FIN ======" -ForegroundColor Cyan

