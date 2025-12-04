#!/usr/bin/env powershell
# Script de Prueba Detallado

$baseUrl = "http://localhost:5132"
$adminEmail = "admin@apexvision.com"
$adminPassword = "Admin123!"

Write-Host "========== TEST DETALLADO DE ENDPOINTS ==========" -ForegroundColor Cyan
Write-Host ""

# Test 1: Login
Write-Host "Test 1: LOGIN ADMIN" -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    $adminToken = $content.token
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Código: $($response.StatusCode)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

Write-Host ""

# Test 2: Verificar Swagger
Write-Host "Test 2: SWAGGER" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -ErrorAction Stop
    Write-Host "✅ Swagger disponible" -ForegroundColor Green
    Write-Host "   Código: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "   URL: $baseUrl/swagger/index.html" -ForegroundColor Gray
} catch {
    Write-Host "❌ Swagger no disponible" -ForegroundColor Red
}

Write-Host ""

# Test 3: Obtener Pedidos (Verificar que la BD está funcionando)
Write-Host "Test 3: OBTENER PEDIDOS" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/Orders" -Method GET -ContentType "application/json" -Headers @{"Authorization" = "Bearer $adminToken"} -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    Write-Host "✅ Pedidos obtenidos" -ForegroundColor Green
    Write-Host "   Código: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "   Total de pedidos: $($content.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Error al obtener pedidos" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Obtener Conductores
Write-Host "Test 4: OBTENER CONDUCTORES" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/users/drivers" -Method GET -ContentType "application/json" -Headers @{"Authorization" = "Bearer $adminToken"} -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    Write-Host "✅ Conductores obtenidos" -ForegroundColor Green
    Write-Host "   Código: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "   Total de conductores: $($content.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Error al obtener conductores" -ForegroundColor Red
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Crear Pedido - Mostrar exactamente qué se envía
Write-Host "Test 5: CREAR PEDIDO" -ForegroundColor Yellow

$orderData = @{
    address = "Test Address"
    latitude = 6.2176
    longitude = -75.5353
    description = "Test Order"
    requiresEvidence = $false
}

Write-Host "   Datos enviados:" -ForegroundColor Gray
Write-Host "   - Address: $($orderData.address)" -ForegroundColor Gray
Write-Host "   - Latitude: $($orderData.latitude)" -ForegroundColor Gray
Write-Host "   - Longitude: $($orderData.longitude)" -ForegroundColor Gray
Write-Host "   - Description: $($orderData.description)" -ForegroundColor Gray
Write-Host "   - RequiresEvidence: $($orderData.requiresEvidence)" -ForegroundColor Gray

$orderBody = $orderData | ConvertTo-Json
Write-Host "   JSON enviado: $orderBody" -ForegroundColor Gray

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/Orders" -Method POST -ContentType "application/json" -Body $orderBody -Headers @{"Authorization" = "Bearer $adminToken"} -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    Write-Host "✅ Pedido creado" -ForegroundColor Green
    Write-Host "   Código: $($response.StatusCode)" -ForegroundColor Gray
    Write-Host "   Order ID: $($content.orderId)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Error al crear pedido" -ForegroundColor Red
    Write-Host "   Código de error: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    Write-Host "   Mensaje: $($_.Exception.Message)" -ForegroundColor Red
    
    # Intentar leer el contenido de la respuesta
    try {
        $streamReader = [System.IO.StreamReader]::new($_.Exception.Response.GetResponseStream())
        $errorContent = $streamReader.ReadToEnd()
        $streamReader.Close()
        if ($errorContent) {
            Write-Host "   Respuesta del servidor: $errorContent" -ForegroundColor Red
        }
    } catch {
        Write-Host "   No se pudo leer respuesta del servidor" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========== PRUEBAS COMPLETADAS ==========" -ForegroundColor Cyan

