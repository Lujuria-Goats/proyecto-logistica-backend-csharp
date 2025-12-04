#!/usr/bin/env pwsh
$ErrorActionPreference = "Continue"
$api = "http://localhost:5132"
$pass = 0
$fail = 0

Write-Host "========== VERIFICACIÓN DE ENDPOINTS ==========" -ForegroundColor Cyan

# 1. Login Admin
Write-Host "`n[1] Login Admin..." -NoNewline
try {
    $resp = Invoke-WebRequest "$api/api/auth/login" -Method POST -ContentType "application/json" `
        -Body (@{email="admin@apexvision.com";password="Admin123!"} | ConvertTo-Json)
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌ $_" -ForegroundColor Red
    $fail++
    exit
}

# 2. POST /api/Orders
Write-Host "[2] POST /api/Orders..." -NoNewline
try {
    $body = @{address="Test";latitude=6.2;longitude=-75.5;description="Test";requiresEvidence=$false} | ConvertTo-Json
    $resp = Invoke-WebRequest "$api/api/Orders" -Method POST -ContentType "application/json" `
        -Headers @{Authorization="Bearer $adminToken"} -Body $body
    $orderId = ($resp.Content | ConvertFrom-Json).orderId
    Write-Host " ✅ (ID: $orderId)" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    Write-Host "  Error: $_" -ForegroundColor Red
    $fail++
}

# 3. GET /api/Orders
Write-Host "[3] GET /api/Orders..." -NoNewline
try {
    $resp = Invoke-WebRequest "$api/api/Orders" -Method GET `
        -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    $fail++
}

# 4. Register Driver
Write-Host "[4] POST /api/Auth/register (Driver)..." -NoNewline
try {
    $email = "driver$(Get-Random)@test.com"
    $body = @{email=$email;fullName="Test";password="SecurePass123!";phoneNumber="+573001234567";role="Driver"} | ConvertTo-Json
    $resp = Invoke-WebRequest "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $body
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    $fail++
}

# 5. Login Driver
Write-Host "[5] POST /api/Auth/login (Driver)..." -NoNewline
try {
    $body = @{email=$email;password="SecurePass123!"} | ConvertTo-Json
    $resp = Invoke-WebRequest "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $body
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    $fail++
    exit
}

# 6. GET /api/Orders/my-route
Write-Host "[6] GET /api/Orders/my-route..." -NoNewline
try {
    $resp = Invoke-WebRequest "$api/api/Orders/my-route" -Method GET `
        -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    $fail++
}

# 7. GET /api/Routes/saved
Write-Host "[7] GET /api/Routes/saved..." -NoNewline
try {
    $resp = Invoke-WebRequest "$api/api/Routes/saved" -Method GET `
        -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    $pass++
} catch {
    Write-Host " ❌" -ForegroundColor Red
    $fail++
}

# 7.5 Asignar pedido al driver (necesario antes de guardar ruta)
if ($orderId) {
    $parts = $driverToken.Split('.')
    $payload = $parts[1]
    $padding = 4 - ($payload.Length % 4)
    if ($padding -ne 4) {
        $payload = $payload + ('=' * $padding)
    }
    $decoded = [System.Convert]::FromBase64String($payload)
    $decodedString = [System.Text.Encoding]::UTF8.GetString($decoded)
    $decodedJson = $decodedString | ConvertFrom-Json
    $driverId = $decodedJson.nameid
    
    Write-Host "[7.5] PUT /api/Orders/{orderId}/assign/{driverId}..." -NoNewline
    try {
        $resp = Invoke-WebRequest "$api/api/Orders/$orderId/assign/$driverId" -Method PUT `
            -Headers @{Authorization="Bearer $adminToken"}
        Write-Host " ✅" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        $fail++
    }
}

# 8. POST /api/Routes/save
Write-Host "[8] POST /api/Routes/save..." -NoNewline
if ($orderId) {
    try {
        $body = @{orderIds=@($orderId);routeName="Test Route"} | ConvertTo-Json
        $resp = Invoke-WebRequest "$api/api/Routes/save" -Method POST -ContentType "application/json" `
            -Headers @{Authorization="Bearer $driverToken"} -Body $body
        $routeId = ($resp.Content | ConvertFrom-Json).routeId
        Write-Host " ✅ (ID: $routeId)" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        Write-Host "  Error: $_" -ForegroundColor Red
        $fail++
    }
}

# 9. GET /api/Routes/saved/{routeId}
if ($routeId) {
    Write-Host "[9] GET /api/Routes/saved/{routeId}..." -NoNewline
    try {
        $resp = Invoke-WebRequest "$api/api/Routes/saved/$routeId" -Method GET `
            -Headers @{Authorization="Bearer $driverToken"}
        Write-Host " ✅" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        $fail++
    }
}

# 10. POST /api/Routes/saved/{routeId}/load
if ($routeId) {
    Write-Host "[10] POST /api/Routes/saved/{routeId}/load..." -NoNewline
    try {
        $resp = Invoke-WebRequest "$api/api/Routes/saved/$routeId/load" -Method POST `
            -Headers @{Authorization="Bearer $driverToken"}
        Write-Host " ✅" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        $fail++
    }
}

# 11. POST /api/Routes/saved/{routeId}/rename
if ($routeId) {
    Write-Host "[11] POST /api/Routes/saved/{routeId}/rename..." -NoNewline
    try {
        $body = @{newName="Renamed Route"} | ConvertTo-Json
        $resp = Invoke-WebRequest "$api/api/Routes/saved/$routeId/rename" -Method POST -ContentType "application/json" `
            -Headers @{Authorization="Bearer $driverToken"} -Body $body
        Write-Host " ✅" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        $fail++
    }
}

# 12. DELETE /api/Routes/saved/{routeId}
if ($routeId) {
    Write-Host "[12] DELETE /api/Routes/saved/{routeId}..." -NoNewline
    try {
        $resp = Invoke-WebRequest "$api/api/Routes/saved/$routeId" -Method DELETE `
            -Headers @{Authorization="Bearer $driverToken"}
        Write-Host " ✅" -ForegroundColor Green
        $pass++
    } catch {
        Write-Host " ❌" -ForegroundColor Red
        $fail++
    }
}

Write-Host "`n========== RESUMEN ==========" -ForegroundColor Cyan
Write-Host "✅ Exitosos: $pass" -ForegroundColor Green
Write-Host "❌ Fallos: $fail" -ForegroundColor Red

if ($fail -eq 0) {
    Write-Host "`n🎉 ¡TODOS LOS ENDPOINTS FUNCIONAN!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n⚠️ Algunos endpoints fallaron" -ForegroundColor Yellow
    exit 1
}

