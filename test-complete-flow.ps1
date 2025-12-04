#!/usr/bin/env pwsh

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "FLUJO COMPLETO: LOGIN -> CREAR PEDIDO" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# ============================================
# PASO 1: LOGIN
# ============================================
Write-Host "PASO 1: LOGIN" -ForegroundColor Yellow
Write-Host "=============" -ForegroundColor Yellow
Write-Host ""

$loginJson = @{
    email = "admin@apexvision.com"
    password = "Admin123!"
}

Write-Host "Endpoint: POST /api/Auth/login" -ForegroundColor Cyan
Write-Host "JSON enviado:" -ForegroundColor Green
Write-Host ($loginJson | ConvertTo-Json) -ForegroundColor Green
Write-Host ""

try {
    $loginResponse = Invoke-WebRequest -Uri "http://localhost:5132/api/Auth/login" `
        -Method POST `
        -Body ($loginJson | ConvertTo-Json) `
        -ContentType "application/json" `
        -UseBasicParsing
    
    $loginData = $loginResponse.Content | ConvertFrom-Json
    $token = $loginData.token
    
    Write-Host "Respuesta del servidor:" -ForegroundColor Green
    Write-Host $loginResponse.Content -ForegroundColor Green
    Write-Host ""
    Write-Host "Status: $($loginResponse.StatusCode)" -ForegroundColor Green
    Write-Host "Token obtenido: $($token.Substring(0, 50))..." -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR en login: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# ============================================
# PASO 2: CREAR PEDIDO
# ============================================
Write-Host "PASO 2: CREAR PEDIDO" -ForegroundColor Yellow
Write-Host "====================" -ForegroundColor Yellow
Write-Host ""

$orderJson = @{
    address = "Centro Comercial Santafe, Medellin"
    latitude = 6.198
    longitude = -75.578
    description = "Pedido de Prueba - Flujo Completo"
    requiresEvidence = $false
}

Write-Host "Endpoint: POST /api/Orders" -ForegroundColor Cyan
Write-Host "Headers:" -ForegroundColor Green
Write-Host "  Authorization: Bearer $($token.Substring(0, 50))..." -ForegroundColor Green
Write-Host ""
Write-Host "JSON enviado:" -ForegroundColor Green
Write-Host ($orderJson | ConvertTo-Json) -ForegroundColor Green
Write-Host ""

try {
    $orderResponse = Invoke-WebRequest -Uri "http://localhost:5132/api/Orders" `
        -Method POST `
        -Body ($orderJson | ConvertTo-Json) `
        -ContentType "application/json" `
        -Headers @{Authorization = "Bearer $token"} `
        -UseBasicParsing
    
    Write-Host "Respuesta del servidor:" -ForegroundColor Green
    Write-Host $orderResponse.Content -ForegroundColor Green
    Write-Host ""
    Write-Host "Status: $($orderResponse.StatusCode)" -ForegroundColor Green
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "EXITO! Pedido creado correctamente" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
} catch {
    $statusCode = $_.Exception.Response.StatusCode.Value__
    Write-Host "ERROR al crear pedido: Status Code $statusCode" -ForegroundColor Red
    Write-Host "Mensaje: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
