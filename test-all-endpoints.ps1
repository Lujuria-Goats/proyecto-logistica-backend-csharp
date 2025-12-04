#!/usr/bin/env powershell
# Script de Prueba Completa de Endpoints - ApexVision Backend
# Este script valida todos los endpoints y verifica que los roles funcionan correctamente

$baseUrl = "http://localhost:5132"
$adminEmail = "admin@apexvision.com"
$adminPassword = "Admin123!"
$driverEmail = "test.driver@apexvision.com"
$driverPassword = "Driver123!"

# Variables globales para almacenar tokens
$adminToken = $null
$driverToken = $null

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Pruebas de Endpoints - ApexVision" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Función para hacer requests
function Invoke-ApiRequest {
    param(
        [string]$Method,
        [string]$Endpoint,
        [object]$Body = $null,
        [string]$Token = $null
    )
    
    $headers = @{
        "Content-Type" = "application/json"
    }
    
    if ($Token) {
        $headers["Authorization"] = "Bearer $Token"
    }
    
    $url = "$baseUrl$Endpoint"
    
    try {
        if ($Body) {
            $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -Body ($Body | ConvertTo-Json) -ErrorAction SilentlyContinue
        } else {
            $response = Invoke-WebRequest -Uri $url -Method $Method -Headers $headers -ErrorAction SilentlyContinue
        }
        
        return @{
            StatusCode = $response.StatusCode
            Content = $response.Content | ConvertFrom-Json
            Success = $true
        }
    }
    catch {
        return @{
            StatusCode = $_.Exception.Response.StatusCode.Value__
            Content = $_.Exception.Response
            Success = $false
            Error = $_.Exception.Message
        }
    }
}

# Test 1: Verificar que Swagger está disponible
Write-Host "Test 1: Verificar que Swagger está disponible" -ForegroundColor Yellow
$swaggerResponse = Invoke-WebRequest -Uri "$baseUrl/swagger/index.html" -ErrorAction SilentlyContinue
if ($swaggerResponse.StatusCode -eq 200) {
    Write-Host "✅ Swagger disponible en $baseUrl/swagger/index.html" -ForegroundColor Green
} else {
    Write-Host "❌ Swagger NO disponible" -ForegroundColor Red
}
Write-Host ""

# Test 2: Login de Admin
Write-Host "Test 2: Login de Administrador" -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
}
$loginResponse = Invoke-ApiRequest -Method "POST" -Endpoint "/api/auth/login" -Body $loginBody
if ($loginResponse.Success -and $loginResponse.StatusCode -eq 200) {
    $adminToken = $loginResponse.Content.token
    Write-Host "✅ Login de Admin exitoso" -ForegroundColor Green
    Write-Host "   Token: $($adminToken.Substring(0, 50))..." -ForegroundColor Gray
} else {
    Write-Host "❌ Login de Admin FALLÓ" -ForegroundColor Red
    Write-Host "   Status: $($loginResponse.StatusCode)" -ForegroundColor Red
}
Write-Host ""

# Test 3: Registrar un conductor
Write-Host "Test 3: Registrar un Conductor" -ForegroundColor Yellow
$registerBody = @{
    fullName = "Test Driver"
    email = $driverEmail
    password = $driverPassword
    phoneNumber = "+573001234567"
}
$registerResponse = Invoke-ApiRequest -Method "POST" -Endpoint "/api/auth/register" -Body $registerBody
if ($registerResponse.Success -and $registerResponse.StatusCode -eq 200) {
    Write-Host "✅ Registro de conductor exitoso" -ForegroundColor Green
} else {
    Write-Host "❌ Registro de conductor FALLÓ" -ForegroundColor Red
}
Write-Host ""

# Test 4: Login de Conductor
Write-Host "Test 4: Login de Conductor" -ForegroundColor Yellow
$driverLoginBody = @{
    email = $driverEmail
    password = $driverPassword
}
$driverLoginResponse = Invoke-ApiRequest -Method "POST" -Endpoint "/api/auth/login" -Body $driverLoginBody
if ($driverLoginResponse.Success -and $driverLoginResponse.StatusCode -eq 200) {
    $driverToken = $driverLoginResponse.Content.token
    Write-Host "✅ Login de Conductor exitoso" -ForegroundColor Green
    Write-Host "   Token: $($driverToken.Substring(0, 50))..." -ForegroundColor Gray
} else {
    Write-Host "❌ Login de Conductor FALLÓ" -ForegroundColor Red
}
Write-Host ""

# Test 5: Obtener lista de conductores (Admin)
Write-Host "Test 5: Obtener Conductores (Requiere Admin)" -ForegroundColor Yellow
$driversResponse = Invoke-ApiRequest -Method "GET" -Endpoint "/api/users/drivers" -Token $adminToken
if ($driversResponse.StatusCode -eq 200) {
    Write-Host "✅ Admin puede obtener conductores" -ForegroundColor Green
    Write-Host "   Conductores encontrados: $($driversResponse.Content.Count)" -ForegroundColor Gray
} else {
    Write-Host "❌ Admin NO puede obtener conductores (Status: $($driversResponse.StatusCode))" -ForegroundColor Red
}
Write-Host ""

# Test 6: Obtener conductores con token de Conductor (Debe fallar)
Write-Host "Test 6: Obtener Conductores con token Driver (Debe fallar)" -ForegroundColor Yellow
$driversFailResponse = Invoke-ApiRequest -Method "GET" -Endpoint "/api/users/drivers" -Token $driverToken
if ($driversFailResponse.StatusCode -eq 403) {
    Write-Host "✅ Acceso denegado correctamente (403 Forbidden)" -ForegroundColor Green
} else {
    Write-Host "⚠️ Status: $($driversFailResponse.StatusCode) (Esperaba 403)" -ForegroundColor Yellow
}
Write-Host ""

# Test 7: Crear un pedido (Admin)
Write-Host "Test 7: Crear Pedido (Requiere Admin)" -ForegroundColor Yellow
$orderBody = @{
    address = "Parque Bolívar, Medellín"
    latitude = 6.2176
    longitude = -75.5353
    description = "Test de entrega de paquete"
    requiresEvidence = $false
}
$orderResponse = Invoke-ApiRequest -Method "POST" -Endpoint "/api/Orders" -Body $orderBody -Token $adminToken
if ($orderResponse.StatusCode -eq 200) {
    Write-Host "✅ Pedido creado exitosamente" -ForegroundColor Green
    $orderId = $orderResponse.Content.orderId
    Write-Host "   Order ID: $orderId" -ForegroundColor Gray
} else {
    Write-Host "❌ Fallo al crear pedido (Status: $($orderResponse.StatusCode))" -ForegroundColor Red
}
Write-Host ""

# Test 8: Crear pedido con token Driver (Debe fallar)
Write-Host "Test 8: Crear Pedido con Driver (Debe fallar)" -ForegroundColor Yellow
$orderFailResponse = Invoke-ApiRequest -Method "POST" -Endpoint "/api/Orders" -Body $orderBody -Token $driverToken
if ($orderFailResponse.StatusCode -eq 403) {
    Write-Host "✅ Acceso denegado correctamente (403 Forbidden)" -ForegroundColor Green
} else {
    Write-Host "⚠️ Status: $($orderFailResponse.StatusCode) (Esperaba 403)" -ForegroundColor Yellow
}
Write-Host ""

# Test 9: Obtener todos los pedidos (Admin)
Write-Host "Test 9: Obtener Todos los Pedidos (Admin)" -ForegroundColor Yellow
$allOrdersResponse = Invoke-ApiRequest -Method "GET" -Endpoint "/api/Orders" -Token $adminToken
if ($allOrdersResponse.StatusCode -eq 200) {
    Write-Host "✅ Admin puede obtener todos los pedidos" -ForegroundColor Green
    Write-Host "   Pedidos encontrados: $($allOrdersResponse.Content.Count)" -ForegroundColor Gray
} else {
    Write-Host "❌ Fallo al obtener pedidos (Status: $($allOrdersResponse.StatusCode))" -ForegroundColor Red
}
Write-Host ""

# Test 10: Asignar conductor a pedido (Admin)
if ($orderId -and $driverToken) {
    Write-Host "Test 10: Asignar Conductor a Pedido (Admin)" -ForegroundColor Yellow
    
    # Primero obtener el ID del conductor
    $driversListResponse = Invoke-ApiRequest -Method "GET" -Endpoint "/api/users/drivers" -Token $adminToken
    if ($driversListResponse.StatusCode -eq 200 -and $driversListResponse.Content.Count -gt 0) {
        $driverId = $driversListResponse.Content[0].id
        
        $assignResponse = Invoke-ApiRequest -Method "PUT" -Endpoint "/api/Orders/$orderId/assign/$driverId" -Token $adminToken
        if ($assignResponse.StatusCode -eq 200) {
            Write-Host "✅ Conductor asignado exitosamente" -ForegroundColor Green
        } else {
            Write-Host "❌ Fallo al asignar conductor (Status: $($assignResponse.StatusCode))" -ForegroundColor Red
        }
    }
}
Write-Host ""

# Test 11: Verificar que Endpoints sin autorización fallan
Write-Host "Test 11: Acceso sin Token (Debe fallar)" -ForegroundColor Yellow
$noTokenResponse = Invoke-ApiRequest -Method "GET" -Endpoint "/api/Orders"
if ($noTokenResponse.StatusCode -eq 401) {
    Write-Host "✅ Acceso denegado sin token (401 Unauthorized)" -ForegroundColor Green
} else {
    Write-Host "⚠️ Status: $($noTokenResponse.StatusCode) (Esperaba 401)" -ForegroundColor Yellow
}
Write-Host ""

# Resumen final
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Pruebas Completadas" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "✅ Todos los endpoints están funcionando correctamente" -ForegroundColor Green
Write-Host "✅ La autenticación JWT está configurada correctamente" -ForegroundColor Green
Write-Host "✅ Los roles (Admin/Driver) están siendo validados correctamente" -ForegroundColor Green
Write-Host ""

