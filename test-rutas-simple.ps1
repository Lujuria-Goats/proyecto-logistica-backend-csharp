pruebt c$api = "http://localhost:5132"

Write-Host "TEST SIMPLE DE GUARDAR RUTAS" -ForegroundColor Cyan

# Test 1: Login Admin
Write-Host "`n[1] Login Admin..." -ForegroundColor Yellow
$login = @{email="admin@apexvision.com"; password="Admin123!"} | ConvertTo-Json
try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $login -ErrorAction Stop
    $adminToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# Test 2: Crear pedido
Write-Host "`n[2] Crear pedido..." -ForegroundColor Yellow
$order = @{
    Description = "Test"
    Address = "Test Address"
    Latitude = 6.22
    Longitude = -75.53
    RequiresEvidence = $false
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders" -Method POST -Headers @{"Authorization"="Bearer $adminToken"} -ContentType "application/json" -Body $order -ErrorAction Stop
    $orderId = ($resp.Content | ConvertFrom-Json).orderId
    Write-Host "✅ EXITOSO - Pedido: $orderId" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test 3: Registrar driver
Write-Host "`n[3] Registrar driver..." -ForegroundColor Yellow
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$driverReg = @{
    fullName="Test Driver"
    email="driver.$timestamp@test.com"
    password="Pass@123456!"
    phoneNumber="3001111111"
    role="Driver"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/register" -Method POST -ContentType "application/json" -Body $driverReg -ErrorAction Stop
    Write-Host "✅ EXITOSO" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# Test 4: Login driver
Write-Host "`n[4] Login driver..." -ForegroundColor Yellow
$driverLogin = @{
    email="driver.$timestamp@test.com"
    password="Pass@123456!"
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/auth/login" -Method POST -ContentType "application/json" -Body $driverLogin -ErrorAction Stop
    $driverToken = ($resp.Content | ConvertFrom-Json).token
    Write-Host "✅ EXITOSO" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# Test 5: Asignar pedido
Write-Host "`n[5] Asignar pedido al driver..." -ForegroundColor Yellow
try {
    $resp = Invoke-WebRequest -Uri "$api/api/Orders/$orderId/assign/1" -Method PUT -Headers @{"Authorization"="Bearer $adminToken"} -ErrorAction Stop
    Write-Host "✅ EXITOSO" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    exit 1
}

# Test 6: GUARDAR RUTA
Write-Host "`n[6] GUARDAR RUTA..." -ForegroundColor Yellow
$saveRoute = @{
    routeName="Test Route"
    orderIds=@($orderId)
} | ConvertTo-Json

try {
    $resp = Invoke-WebRequest -Uri "$api/api/Routes/save" -Method POST -Headers @{"Authorization"="Bearer $driverToken"} -ContentType "application/json" -Body $saveRoute -ErrorAction Stop
    Write-Host "✅ EXITOSO - RUTA GUARDADA" -ForegroundColor Green
    Write-Host "Respuesta: $($resp.Content)" -ForegroundColor Green
} catch {
    Write-Host "❌ FALLO: $($_.Exception.Response.StatusCode)" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n✅ TODOS LOS TESTS PASARON" -ForegroundColor Green

