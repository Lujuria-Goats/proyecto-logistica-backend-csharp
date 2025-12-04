$api = "http://localhost:5132"
Write-Host "====== TEST COMPLETO - GUARDAR RUTAS ======" -ForegroundColor Cyan

Write-Host "`n[TEST 1] Login Admin..." -ForegroundColor Yellow
$login = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login -ErrorAction Stop
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO - Admin autenticado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[TEST 2] Crear pedido..." -ForegroundColor Yellow
$order = @{Description="Test"; Address="Test Address"; Latitude=6.22; Longitude=-75.53; RequiresEvidence=$false} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $adminToken"} -ContentType "application/json" -Body $order -ErrorAction Stop
    $orderId = ($resp.Content | ConvertFrom-Json).orderId
    Write-Host "✅ EXITOSO - Pedido creado: $orderId" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[TEST 3] Registrar Driver..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$driverReg = @{fullName="Test Driver"; email="driver.$timestamp@test.com"; password="Pass@123456!"; phoneNumber="3001111111"; role="Driver"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $driverReg -ErrorAction Stop
    Write-Host "✅ EXITOSO - Driver registrado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[TEST 4] Login Driver..." -ForegroundColor Yellow
$driverLogin = @{email="driver.$timestamp@test.com"; password="Pass@123456!"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin -ErrorAction Stop
    $driverResp = $resp.Content | ConvertFrom-Json
    $driverToken = $driverResp.token
    $driverId = $driverResp.userId
    Write-Host "✅ EXITOSO - Driver autenticado (ID: $driverId)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[TEST 5] Asignar pedido al driver (ID: $driverId)..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders/$orderId/assign/$driverId" -Method PUT -Headers @{"Authorization"="Bearer $adminToken"} -ErrorAction Stop
    Write-Host "✅ EXITOSO - Pedido asignado" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

Write-Host "`n[TEST 6] GUARDAR RUTA..." -ForegroundColor Yellow
$saveRoute = @{routeName="Mi Primera Ruta"; orderIds=@($orderId)} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $driverToken"} -ContentType "application/json" -Body $saveRoute -ErrorAction Stop
    Write-Host "✅ EXITOSO - RUTA GUARDADA" -ForegroundColor Green
    Write-Host "Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n====== TEST COMPLETADO EXITOSAMENTE ======" -ForegroundColor Green
