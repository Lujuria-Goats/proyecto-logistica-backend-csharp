#!/usr/bin/env pwsh

$api = "http://localhost:5132"

Write-Host "========== PRUEBA POST /api/Orders ==========" -ForegroundColor Yellow

# 1. Login
Write-Host "`n[1] Login Admin..." -ForegroundColor Cyan
$loginBody = @{
    email = "admin@apexvision.com"
    password = "Admin123!"
} | ConvertTo-Json

$loginResp = Invoke-WebRequest `
    -Uri "$api/api/auth/login" `
    -Method POST `
    -Headers @{ 'Content-Type' = 'application/json' } `
    -Body $loginBody

$adminToken = ($loginResp.Content | ConvertFrom-Json).token
Write-Host "✅ Admin login exitoso" -ForegroundColor Green

# 2. Crear pedido con JSON limpio
Write-Host "`n[2] Creando pedido..." -ForegroundColor Cyan

$orderBody = @{
    "address" = "Calle Principal 123, Medellín"
    "latitude" = 6.2442
    "longitude" = -75.5898
    "description" = "Test Delivery"
    "requiresEvidence" = $false
} | ConvertTo-Json

Write-Host "JSON a enviar:" -ForegroundColor Cyan
Write-Host $orderBody -ForegroundColor Green

try {
    $response = Invoke-WebRequest `
        -Uri "$api/api/Orders" `
        -Method POST `
        -Headers @{ 
            'Authorization' = "Bearer $adminToken"
            'Content-Type' = 'application/json'
        } `
        -Body $orderBody `
        -ErrorAction Stop
    
    Write-Host "`n✅ POST /api/Orders EXITOSO" -ForegroundColor Green
    $responseData = $response.Content | ConvertFrom-Json
    Write-Host ($responseData | ConvertTo-Json) -ForegroundColor Green
    $orderId = $responseData.orderId
    Write-Host "Order ID: $orderId" -ForegroundColor Green
}
catch {
    Write-Host "`n❌ POST /api/Orders FALLO" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        $reader.Close()
        Write-Host "Detalle: $errorBody" -ForegroundColor Yellow
    } catch {}
}

