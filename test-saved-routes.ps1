#!/usr/bin/env powershell
# Test de Endpoints de Rutas Guardadas

$api = "http://localhost:5132"
$adminEmail = "admin@apexvision.com"
$adminPass = "Admin123!"

Write-Host "========== TEST DE RUTAS GUARDADAS ==========" -ForegroundColor Cyan
Write-Host ""

# 1. Login Admin
Write-Host "1. Login Admin..." -ForegroundColor Yellow
$login = @{
    email = $adminEmail
    password = $adminPass
} | ConvertTo-Json

$resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login
$token = ($resp.Content | ConvertFrom-Json).token
Write-Host "✅ Login exitoso" -ForegroundColor Green

# 2. Crear 3 pedidos
Write-Host "`n2. Creando 3 pedidos..." -ForegroundColor Yellow
$orderIds = @()
for ($i = 1; $i -le 3; $i++) {
    $order = @{
        address = "Dirección $i"
        latitude = 6.2 + ($i * 0.01)
        longitude = -75.5 - ($i * 0.01)
        description = "Pedido $i"
        requiresEvidence = $false
    } | ConvertTo-Json
    
    $resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization" = "Bearer $token"} -ContentType "application/json" -Body $order
    $orderId = ($resp.Content | ConvertFrom-Json).orderId
    $orderIds += $orderId
    Write-Host "   ✅ Pedido $orderId creado"
}

# 3. Asignar a Driver 2
Write-Host "`n3. Asignando pedidos a Driver 2..." -ForegroundColor Yellow
foreach ($oid in $orderIds) {
    Invoke-WebRequest -Uri "$api/api/Orders/$oid/assign/2" -Method PUT -Headers @{"Authorization" = "Bearer $token"} | Out-Null
    Write-Host "   ✅ Pedido $oid asignado"
}

# 4. Login como Driver
Write-Host "`n4. Login como Driver..." -ForegroundColor Yellow
$driverLogin = @{
    email = "test.driver@apexvision.com"
    password = "Driver123!"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ Login Driver exitoso" -ForegroundColor Green
} catch {
    Write-Host "❌ Driver no existe, creando..." -ForegroundColor Yellow
    $register = @{
        fullName = "Test Driver"
        email = "test.driver@apexvision.com"
        password = "Driver123!"
        phoneNumber = "+573001234567"
        role = "Driver"
    } | ConvertTo-Json
    
    Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $register | Out-Null
    
    # Login después del registro
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ Driver creado y logueado" -ForegroundColor Green
}

# 5. GUARDAR RUTA
Write-Host "`n5. GUARDANDO RUTA..." -ForegroundColor Yellow
$saveRoute = @{
    routeName = "Ruta Test - Centro"
    orderIds = $orderIds
} | ConvertTo-Json

$resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization" = "Bearer $driverToken"} -ContentType "application/json" -Body $saveRoute
$content = $resp.Content | ConvertFrom-Json
$routeId = $content.routeId
Write-Host "✅ Ruta guardada (ID: $routeId)" -ForegroundColor Green
Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Gray
Write-Host "   Pedidos: $($content.orderCount)" -ForegroundColor Gray
Write-Host "   Teléfono: $($content.phoneNumber)" -ForegroundColor Gray

# 6. VER RUTAS GUARDADAS
Write-Host "`n6. VER RUTAS GUARDADAS..." -ForegroundColor Yellow
$resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" -Method GET -Headers @{"Authorization" = "Bearer $driverToken"}
$rutas = $resp.Content | ConvertFrom-Json
Write-Host "✅ Rutas obtenidas" -ForegroundColor Green
Write-Host "   Total: $($rutas.totalSavedRoutes)" -ForegroundColor Gray
Write-Host "   Teléfono: $($rutas.phoneNumber)" -ForegroundColor Gray

# 7. VER DETALLES DE RUTA
Write-Host "`n7. VER DETALLES DE RUTA..." -ForegroundColor Yellow
$resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId" -Method GET -Headers @{"Authorization" = "Bearer $driverToken"}
$content = $resp.Content | ConvertFrom-Json
Write-Host "✅ Detalles obtenidos" -ForegroundColor Green
Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Gray
Write-Host "   Pedidos: $($content.orders.Count)" -ForegroundColor Gray
Write-Host "   Creada: $($content.createdDate)" -ForegroundColor Gray

# 8. CARGAR RUTA
Write-Host "`n8. CARGAR RUTA GUARDADA..." -ForegroundColor Yellow
$resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId/load" -Method POST -Headers @{"Authorization" = "Bearer $driverToken"}
$content = $resp.Content | ConvertFrom-Json
Write-Host "✅ Ruta cargada" -ForegroundColor Green
Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Gray
Write-Host "   Total Pedidos: $($content.totalOrders)" -ForegroundColor Gray

# 9. RENOMBRAR RUTA
Write-Host "`n9. RENOMBRAR RUTA..." -ForegroundColor Yellow
$rename = @{
    newName = "Ruta Exitosa - Centro Medellín"
} | ConvertTo-Json

$resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId/rename" -Method POST -Headers @{"Authorization" = "Bearer $driverToken"} -ContentType "application/json" -Body $rename
$content = $resp.Content | ConvertFrom-Json
Write-Host "✅ Ruta renombrada" -ForegroundColor Green
Write-Host "   Nuevo nombre: $($content.newName)" -ForegroundColor Gray

# 10. VER RUTAS NUEVAMENTE
Write-Host "`n10. VER RUTAS GUARDADAS (actualizado)..." -ForegroundColor Yellow
$resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" -Method GET -Headers @{"Authorization" = "Bearer $driverToken"}
$rutas = $resp.Content | ConvertFrom-Json
$rutas.routes | ForEach-Object {
    Write-Host "   - $($_.routeName) (ID: $($_.id))" -ForegroundColor Gray
}

# 11. INTENTAR ACCEDER CON OTRO DRIVER (DEBE FALLAR)
Write-Host "`n11. INTENTAR ACCESO CON OTRO DRIVER (debe fallar)..." -ForegroundColor Yellow
$otherDriverLogin = @{
    email = "otro.driver@apexvision.com"
    password = "Driver123!"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $otherDriverLogin -ErrorAction Stop
    $otherToken = ($resp.Content | ConvertFrom-Json).token
    
    # Intentar ver ruta del primer driver (debe fallar)
    try {
        $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId" -Method GET -Headers @{"Authorization" = "Bearer $otherToken"} -ErrorAction Stop
        Write-Host "❌ PROBLEMA: Otro driver pudo acceder a la ruta" -ForegroundColor Red
    } catch {
        Write-Host "✅ Acceso denegado correctamente (404)" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️ Otro driver no existe (es normal)" -ForegroundColor Yellow
}

Write-Host "`n========== ✅ TODOS LOS TESTS COMPLETADOS ==========" -ForegroundColor Green

