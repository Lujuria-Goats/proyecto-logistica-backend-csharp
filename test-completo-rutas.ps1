#!/usr/bin/env powershell
# Test Completo - Guardar Rutas

$api = "http://localhost:5132"

Write-Host "====== TEST COMPLETO - GUARDAR RUTAS ======" -ForegroundColor Cyan

# [TEST 1] Login Admin
Write-Host "`n[TEST 1] Login Admin..." -ForegroundColor Yellow
$adminLogin = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $adminLogin -ErrorAction Stop
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO - Admin autenticado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# [TEST 2] Crear pedido
Write-Host "`n[TEST 2] Crear pedido..." -ForegroundColor Yellow
$order = @{
    Description = "Pedido Test para Ruta"
    Address = "Calle Test 123"
    Latitude = 6.22
    Longitude = -75.53
    RequiresEvidence = $false
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $adminToken"} -ContentType "application/json" -Body $order -ErrorAction Stop
    $orderId = ($resp.Content | ConvertFrom-Json).orderId
    Write-Host "✅ EXITOSO - Pedido creado: $orderId" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# [TEST 3] Registrar Driver
Write-Host "`n[TEST 3] Registrar Driver..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$driverReg = @{
    fullName="Test Driver Ruta"
    email="driver.ruta.$timestamp@test.com"
    password="Pass@123456!"
    phoneNumber="3001111111"
    role="Driver"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $driverReg -ErrorAction Stop
    Write-Host "✅ EXITOSO - Driver registrado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# [TEST 4] Login Driver
Write-Host "`n[TEST 4] Login Driver..." -ForegroundColor Yellow
$driverLogin = @{
    email="driver.ruta.$timestamp@test.com"
    password="Pass@123456!"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin -ErrorAction Stop
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    
    # Decodificar el JWT para obtener el ID del driver
    $tokenParts = $driverToken.Split('.')
    $payload = $tokenParts[1]
    # Agregar padding si es necesario
    $padding = 4 - ($payload.Length % 4)
    if ($padding -ne 4) { $payload += '=' * $padding }
    $decodedPayload = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($payload))
    $tokenData = $decodedPayload | ConvertFrom-Json
    $driverId = [int]$tokenData.nameid
    
    Write-Host "✅ EXITOSO - Driver autenticado (ID: $driverId)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# [TEST 5] Asignar pedido al driver
Write-Host "`n[TEST 5] Asignar pedido al driver (ID: $driverId)..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders/$orderId/assign/$driverId" -Method PUT -Headers @{"Authorization"="Bearer $adminToken"} -ErrorAction Stop
    Write-Host "✅ EXITOSO - Pedido asignado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# [TEST 6] GUARDAR RUTA
Write-Host "`n[TEST 6] GUARDAR RUTA..." -ForegroundColor Yellow
$saveRoute = @{
    routeName="Ruta Test Exitosa"
    orderIds=@($orderId)
} | ConvertTo-Json

Write-Host "POST /api/Routes/save" -ForegroundColor Gray
Write-Host "Body: $saveRoute" -ForegroundColor Gray
Write-Host "Token: $($driverToken.Substring(0,30))..." -ForegroundColor Gray

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $driverToken"; "Content-Type"="application/json"} -Body $saveRoute -ErrorAction Stop
    Write-Host "Response Status: $($resp.StatusCode)" -ForegroundColor Gray
    $content = $resp.Content | ConvertFrom-Json
    $routeId = $content.routeId
    Write-Host "✅ EXITOSO - Ruta guardada" -ForegroundColor Green
    Write-Host "   ID: $routeId" -ForegroundColor Green
    Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Green
    Write-Host "   Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    # Intentar obtener el body del error para más detalles
    try {
        $errorStream = $_.Exception.Response.GetResponseStream()
        $errorReader = New-Object System.IO.StreamReader($errorStream)
        $errorBody = $errorReader.ReadToEnd()
        Write-Host "Body del error: $errorBody" -ForegroundColor Red
    } catch {
        Write-Host "No se pudo leer el body del error" -ForegroundColor Yellow
    }
    
    # Verificar si el endpoint existe listando todos los endpoints
    Write-Host "`nIntentando acceder a /api/swagger/index.html para verificar endpoints..." -ForegroundColor Yellow
    try {
        $swagger = Invoke-WebRequest -Uri "$api/swagger/index.html" -ErrorAction Stop
        Write-Host "Swagger disponible - endpoints deberían estar documentados" -ForegroundColor Yellow
    } catch {
        Write-Host "Swagger no disponible" -ForegroundColor Yellow
    }
    
    exit 1
}

# [TEST 7] VER RUTAS GUARDADAS
Write-Host "`n[TEST 7] VER RUTAS GUARDADAS..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" -Method GET -Headers @{"Authorization"="Bearer $driverToken"} -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    Write-Host "✅ EXITOSO - Rutas obtenidas" -ForegroundColor Green
    Write-Host "   Total: $($content.totalSavedRoutes)" -ForegroundColor Green
    Write-Host "   Teléfono: $($content.phoneNumber)" -ForegroundColor Green
    Write-Host "   Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# [TEST 8] CARGAR RUTA GUARDADA
Write-Host "`n[TEST 8] CARGAR RUTA GUARDADA..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId/load" -Method POST -Headers @{"Authorization"="Bearer $driverToken"} -ErrorAction Stop
    $content = $resp.Content | ConvertFrom-Json
    Write-Host "✅ EXITOSO - Ruta cargada" -ForegroundColor Green
    Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Green
    Write-Host "   Total Pedidos: $($content.totalOrders)" -ForegroundColor Green
    Write-Host "   Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n====== ✅ TODOS LOS TESTS PASARON ======" -ForegroundColor Green
Write-Host "Funcionalidad de guardar y cargar rutas: FUNCIONANDO CORRECTAMENTE" -ForegroundColor Green

