#!/usr/bin/env powershell
# Script de Prueba Mejorado con Manejo de Errores

$baseUrl = "http://localhost:5132"
$adminEmail = "admin@apexvision.com"
$adminPassword = "Admin123!"

Write-Host "========== TESTS DE ENDPOINTS ==========" -ForegroundColor Cyan
Write-Host ""

# Test 1: Login Admin
Write-Host "Test 1: Login Admin" -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/login" -Method POST -ContentType "application/json" -Body $loginBody -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    $adminToken = $content.token
    Write-Host "✅ Login exitoso" -ForegroundColor Green
    Write-Host "   Token recibido: $($adminToken.Substring(0, 30))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Login falló: $($_.Exception.Message)" -ForegroundColor Red
    exit
}

Write-Host ""

# Test 2: Crear Pedido
Write-Host "Test 2: Crear Pedido (Admin)" -ForegroundColor Yellow
$orderBody = @{
    address = "Parque Bolívar, Medellín"
    latitude = 6.2176
    longitude = -75.5353
    description = "Entrega de paquete de prueba"
    requiresEvidence = $false
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/Orders" -Method POST -ContentType "application/json" -Body $orderBody -Headers @{"Authorization" = "Bearer $adminToken"} -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    $orderId = $content.orderId
    Write-Host "✅ Pedido creado" -ForegroundColor Green
    Write-Host "   Order ID: $orderId" -ForegroundColor Gray
} catch {
    Write-Host "❌ Error al crear pedido" -ForegroundColor Red
    Write-Host "   Status: $($_.Exception.Response.StatusCode.Value__)" -ForegroundColor Red
    Write-Host "   Respuesta: $($_.Exception.Response.Content.ReadAsStringAsync().Result)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Obtener todos los pedidos
Write-Host "Test 3: Obtener Pedidos (Admin)" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/Orders" -Method GET -ContentType "application/json" -Headers @{"Authorization" = "Bearer $adminToken"} -ErrorAction Stop
    $content = $response.Content | ConvertFrom-Json
    Write-Host "✅ Pedidos obtenidos" -ForegroundColor Green
    Write-Host "   Total de pedidos: $($content.Count)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Error al obtener pedidos: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Verificar Swagger
Write-Host "Test 4: Verificar Swagger" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -ErrorAction Stop
    Write-Host "✅ Swagger disponible" -ForegroundColor Green
    Write-Host "   URL: $baseUrl/swagger/index.html" -ForegroundColor Gray
} catch {
    Write-Host "❌ Swagger no disponible: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Pruebas completadas. Revisa los resultados arriba." -ForegroundColor Cyan

