#!/usr/bin/env powershell
# TEST COMPLETO - Guardar y Cargar Rutas

$api = "http://localhost:5132"

Write-Host "
╔════════════════════════════════════════════════════════════╗
║         TEST COMPLETO - GUARDAR Y CARGAR RUTAS             ║
║                   ApexVision Backend                       ║
╚════════════════════════════════════════════════════════════╝
" -ForegroundColor Cyan

# PASO 1: Verificar que API está en vivo
Write-Host "`n[1/9] Verificando que API está en vivo..." -ForegroundColor Yellow
try {
    $test = Invoke-WebRequest -Uri "$api/swagger/index.html" -ErrorAction Stop
    Write-Host "✅ API RESPONDIENDO (Status: $($test.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API NO RESPONDE - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 2: Login como Admin
Write-Host "`n[2/9] Login como Admin..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "admin@apexvision.com"
        password = "Admin123!"
    } | ConvertTo-Json
    
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ Admin autenticado" -ForegroundColor Green
    Write-Host "   Token: $($adminToken.Substring(0,30))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Fallo en login - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 3: Crear 3 pedidos
Write-Host "`n[3/9] Creando 3 pedidos..." -ForegroundColor Yellow
$orderIds = @()
try {
    for ($i = 1; $i -le 3; $i++) {
        $orderBody = @{
            address = "Dirección Test $i, Medellín"
            latitude = 6.2 + ($i * 0.01)
            longitude = -75.5 - ($i * 0.01)
            description = "Pedido de prueba #$i"
            requiresEvidence = $false
        } | ConvertTo-Json
        
        $resp = Invoke-WebRequest -Uri "$api/api/Orders" `
            -Method POST `
            -Headers @{"Authorization" = "Bearer $adminToken"} `
            -ContentType "application/json" `
            -Body $orderBody `
            -ErrorAction Stop
        
        $orderId = ($resp.Content | ConvertFrom-Json).orderId
        $orderIds += $orderId
        Write-Host "   ✅ Pedido #$orderId creado" -ForegroundColor Green
    }
    Write-Host "✅ Todos los pedidos creados: $orderIds" -ForegroundColor Green
} catch {
    Write-Host "❌ Error creando pedidos - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 4: Crear/registrar conductor
Write-Host "`n[4/9] Registrando conductor..." -ForegroundColor Yellow
try {
    $regBody = @{
        fullName = "Conductor Test"
        email = "conductor.test@apexvision.com"
        password = "ConductorTest123!"
        phoneNumber = "+573001111111"
        role = "Driver"
    } | ConvertTo-Json
    
    Invoke-WebRequest -Uri "$api/api/auth/register" `
        -Method POST `
        -ContentType "application/json" `
        -Body $regBody `
        -ErrorAction Stop | Out-Null
    
    Write-Host "✅ Conductor registrado" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Conductor ya existe (OK)" -ForegroundColor Yellow
}

# PASO 5: Login como conductor
Write-Host "`n[5/9] Login como conductor..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = "conductor.test@apexvision.com"
        password = "ConductorTest123!"
    } | ConvertTo-Json
    
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $loginBody `
        -ErrorAction Stop
    
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ Conductor autenticado" -ForegroundColor Green
    Write-Host "   Token: $($driverToken.Substring(0,30))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Fallo en login conductor - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 6: Asignar pedidos al conductor
Write-Host "`n[6/9] Asignando pedidos al conductor..." -ForegroundColor Yellow
try {
    foreach ($orderId in $orderIds) {
        Invoke-WebRequest -Uri "$api/api/Orders/$orderId/assign/1" `
            -Method PUT `
            -Headers @{"Authorization" = "Bearer $adminToken"} `
            -ErrorAction Stop | Out-Null
        Write-Host "   ✅ Pedido #$orderId asignado" -ForegroundColor Green
    }
    Write-Host "✅ Todos los pedidos asignados" -ForegroundColor Green
} catch {
    Write-Host "❌ Error asignando - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 7: GUARDAR RUTA
Write-Host "`n[7/9] GUARDANDO RUTA (POST /api/Routes/save)..." -ForegroundColor Yellow
try {
    $saveBody = @{
        routeName = "Ruta Test - Centro Medellín"
        orderIds = $orderIds
    } | ConvertTo-Json
    
    Write-Host "   Enviando: $saveBody" -ForegroundColor Gray
    
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" `
        -Method POST `
        -Headers @{"Authorization" = "Bearer $driverToken"} `
        -ContentType "application/json" `
        -Body $saveBody `
        -ErrorAction Stop
    
    $content = $resp.Content | ConvertFrom-Json
    $routeId = $content.routeId
    
    Write-Host "✅ RUTA GUARDADA EXITOSAMENTE" -ForegroundColor Green
    Write-Host "   ID: $routeId" -ForegroundColor Green
    Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Green
    Write-Host "   Pedidos: $($content.orderCount)" -ForegroundColor Green
    Write-Host "   Teléfono: $($content.phoneNumber)" -ForegroundColor Green
} catch {
    Write-Host "❌ ERROR al guardar ruta - $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# PASO 8: VER RUTAS GUARDADAS
Write-Host "`n[8/9] Viendo rutas guardadas (GET /api/Routes/saved)..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved" `
        -Method GET `
        -Headers @{"Authorization" = "Bearer $driverToken"} `
        -ErrorAction Stop
    
    $content = $resp.Content | ConvertFrom-Json
    
    Write-Host "✅ RUTAS OBTENIDAS" -ForegroundColor Green
    Write-Host "   Teléfono: $($content.phoneNumber)" -ForegroundColor Green
    Write-Host "   Total rutas: $($content.totalSavedRoutes)" -ForegroundColor Green
    
    if ($content.routes.Count -gt 0) {
        Write-Host "   Listado:" -ForegroundColor Green
        foreach ($route in $content.routes) {
            Write-Host "     • ID: $($route.id) | Nombre: $($route.routeName) | Pedidos: $($route.orderIds.Count)" -ForegroundColor Gray
        }
    }
} catch {
    Write-Host "❌ ERROR obteniendo rutas - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# PASO 9: CARGAR RUTA GUARDADA
Write-Host "`n[9/9] Cargando ruta guardada (POST /api/Routes/saved/$routeId/load)..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/saved/$routeId/load" `
        -Method POST `
        -Headers @{"Authorization" = "Bearer $driverToken"} `
        -ErrorAction Stop
    
    $content = $resp.Content | ConvertFrom-Json
    
    Write-Host "✅ RUTA CARGADA EXITOSAMENTE" -ForegroundColor Green
    Write-Host "   Nombre: $($content.routeName)" -ForegroundColor Green
    Write-Host "   Teléfono: $($content.phoneNumber)" -ForegroundColor Green
    Write-Host "   Total pedidos: $($content.totalOrders)" -ForegroundColor Green
    Write-Host "   Pedidos:" -ForegroundColor Green
    foreach ($order in $content.orders) {
        Write-Host "     • ID: $($order.id) | $($order.address) | Estado: $($order.status)" -ForegroundColor Gray
    }
} catch {
    Write-Host "❌ ERROR cargando ruta - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# RESUMEN FINAL
Write-Host "
╔════════════════════════════════════════════════════════════╗
║                   ✅ TODOS LOS TESTS PASARON               ║
║                                                            ║
║  Funcionalidad de guardar y cargar rutas: FUNCIONANDO ✓   ║
║  Base de datos: FUNCIONANDO ✓                             ║
║  Autenticación: FUNCIONANDO ✓                             ║
║  Endpoints /api/Routes/*: FUNCIONANDO ✓                   ║
║                                                            ║
║              LISTO PARA HACER PUSH A GIT                  ║
╚════════════════════════════════════════════════════════════╝
" -ForegroundColor Green

