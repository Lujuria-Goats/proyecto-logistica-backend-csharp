#!/usr/bin/env pwsh

Write-Host "Teste del endpoint POST /api/Orders" -ForegroundColor Cyan

# Test 1: Sin token (debería dar 401 o 403 si está protegido)
Write-Host "`n[Test 1] POST /api/Orders sin token:"
$orderBody = @{address="Test";latitude=6.198;longitude=-75.578;description="Test";requiresEvidence=$false} | ConvertTo-Json

$response = $null
$error_msg = $null

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5132/api/Orders" `
        -Method POST `
        -Body $orderBody `
        -ContentType "application/json" `
        -UseBasicParsing
    Write-Host "✅ Status Code: $($response.StatusCode)"
    Write-Host "Content: $($response.Content)"
} catch {
    $error_msg = $_.Exception.Response.StatusCode.Value__
    Write-Host "⚠️  Status Code: $error_msg"
    Write-Host "Message: $($_.Exception.Message)"
    
    if ($error_msg -eq 404) {
        Write-Host "❌ Endpoint no encontrado (404) - Posible problema en las rutas"
    } elseif ($error_msg -eq 401 -or $error_msg -eq 403) {
        Write-Host "✅ Status Code correcto - Endpoint existe, falta autenticación"
    }
}

# Test 2: Con token JWT válido
Write-Host "`n[Test 2] POST /api/Orders con token JWT:"

# Obtener token
$loginBody = @{email="admin@apexvision.com";password="Admin123!"} | ConvertTo-Json
try {
    $loginResponse = Invoke-WebRequest -Uri "http://localhost:5132/api/Auth/login" `
        -Method POST `
        -Body $loginBody `
        -ContentType "application/json" `
        -UseBasicParsing
    
    $token = ($loginResponse.Content | ConvertFrom-Json).token
    Write-Host "✅ Token JWT obtenido"
    
    # Crear pedido con token
    try {
        $orderResponse = Invoke-WebRequest -Uri "http://localhost:5132/api/Orders" `
            -Method POST `
            -Body $orderBody `
            -ContentType "application/json" `
            -Headers @{Authorization = "Bearer $token"} `
            -UseBasicParsing
        
        Write-Host "✅ SUCCESS! Status: $($orderResponse.StatusCode)"
        Write-Host "Response: $($orderResponse.Content)"
    } catch {
        $error_code = $_.Exception.Response.StatusCode.Value__
        Write-Host "❌ Error: $error_code"
        Write-Host "Message: $($_.Exception.Message)"
        
        if ($error_code -eq 200) {
            Write-Host "✅ Pedido creado exitosamente!"
        } elseif ($error_code -eq 401) {
            Write-Host "❌ No autenticado (401)"
        } elseif ($error_code -eq 403) {
            Write-Host "❌ No autorizado - Rol no válido (403)"
        } elseif ($error_code -eq 404) {
            Write-Host "❌ Endpoint no encontrado (404)"
        }
    }
} catch {
    Write-Host "❌ Error al obtener token: $($_.Exception.Message)"
}

