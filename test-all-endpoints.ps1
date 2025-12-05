$api = "http://localhost:5132"
$timestamp = Get-Date -Format "yyyyMMddHHmmssfff"
$phoneAdmin = "30" + $timestamp.Substring(8,9)
$phoneDriver = "31" + $timestamp.Substring(8,9)

Write-Host "========== TEST COMPLETO DE ENDPOINTS APEXVISION ==========" -ForegroundColor Cyan
Write-Host ""

$passed = 0
$failed = 0

# Variables globales
$adminToken = ""
$driverToken = ""
$adminId = 0
$driverId = 0
$orderId = 0
$routeId = 0

# ========== AUTENTICACIÓN ==========
Write-Host "=== AUTENTICACIÓN ===" -ForegroundColor Yellow

# 1. Registro Admin
Write-Host "[1] POST /api/Auth/register/admin..." -NoNewline
$adminDto = @{
    userName = "admin_$timestamp"
    fullName = "Admin Test $timestamp"
    email = "admin_$timestamp@test.com"
    password = "Admin@123456"
    phoneNumber = $phoneAdmin
    companyNit = "900$($timestamp.Substring(0,6))-1"
    companyName = "Empresa Test $timestamp"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/register/admin" -Method POST -ContentType "application/json" -Body $adminDto
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   UserId: $($resp.userId), Role: $($resp.role), Company: $($resp.companyName)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 2. Registro Driver
Write-Host "[2] POST /api/Auth/register/driver..." -NoNewline
$driverDto = @{
    userName = "driver_$timestamp"
    fullName = "Driver Test $timestamp"
    email = "driver_$timestamp@test.com"
    password = "Driver@123456"
    phoneNumber = $phoneDriver
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/register/driver" -Method POST -ContentType "application/json" -Body $driverDto
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   UserId: $($resp.userId), Role: $($resp.role)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 3. Login Admin
Write-Host "[3] POST /api/Auth/login (Admin)..." -NoNewline
$loginAdmin = @{
    identifier = "admin_$timestamp@test.com"
    password = "Admin@123456"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/login" -Method POST -ContentType "application/json" -Body $loginAdmin
    $adminToken = $resp.token
    $adminId = $resp.userId
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Role: $($resp.role), CompanyNit: $($resp.companyNit)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 4. Login Driver
Write-Host "[4] POST /api/Auth/login (Driver)..." -NoNewline
$loginDriver = @{
    identifier = "driver_$timestamp@test.com"
    password = "Driver@123456"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/login" -Method POST -ContentType "application/json" -Body $loginDriver
    $driverToken = $resp.token
    $driverId = $resp.userId
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   DriverId: $driverId, Role: $($resp.role)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 5. Login con teléfono
Write-Host "[5] POST /api/Auth/login (con telefono)..." -NoNewline
$loginPhone = @{
    identifier = $phoneDriver
    password = "Driver@123456"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/login" -Method POST -ContentType "application/json" -Body $loginPhone
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 6. Login con NIT
Write-Host "[6] POST /api/Auth/login (con NIT)..." -NoNewline
$loginNit = @{
    identifier = "900$($timestamp.Substring(0,6))-1"
    password = "Admin@123456"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/login" -Method POST -ContentType "application/json" -Body $loginNit
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 7. GET /api/Auth/me
Write-Host "[7] GET /api/Auth/me..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Auth/me" -Method GET -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   UserName: $($resp.userName), Role: $($resp.role)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# ========== GESTIÓN DE CONDUCTORES ==========
Write-Host ""
Write-Host "=== GESTIÓN DE CONDUCTORES ===" -ForegroundColor Yellow

# 8. Buscar conductor por teléfono
Write-Host "[8] GET /api/Drivers/search..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Drivers/search?phone=$phoneDriver" -Method GET -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Found: $($resp.found), AlreadyLinked: $($resp.alreadyLinked)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 9. Vincular conductor
Write-Host "[9] POST /api/Drivers/link..." -NoNewline
$linkDto = @{
    phoneNumber = $phoneDriver
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Drivers/link" -Method POST -ContentType "application/json" -Headers @{Authorization="Bearer $adminToken"} -Body $linkDto
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Message: $($resp.message)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 10. Listar conductores
Write-Host "[10] GET /api/Drivers..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Drivers" -Method GET -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   TotalDrivers: $($resp.totalDrivers)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 11. Obtener conductor específico
Write-Host "[11] GET /api/Drivers/{id}..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Drivers/$driverId" -Method GET -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   FullName: $($resp.fullName)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# ========== GESTIÓN DE PEDIDOS ==========
Write-Host ""
Write-Host "=== GESTIÓN DE PEDIDOS ===" -ForegroundColor Yellow

# 12. Crear pedido
Write-Host "[12] POST /api/Orders..." -NoNewline
$orderDto = @{
    address = "Calle 50 #30-20, Medellin"
    latitude = 6.2442
    longitude = -75.5812
    description = "Paquete test $timestamp"
    requiresEvidence = $true
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Orders" -Method POST -ContentType "application/json" -Headers @{Authorization="Bearer $adminToken"} -Body $orderDto
    $orderId = $resp.orderId
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   OrderId: $orderId" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 13. Listar pedidos
Write-Host "[13] GET /api/Orders..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Orders" -Method GET -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Total: $($resp.Count) pedidos" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 14. Asignar conductor a pedido
Write-Host "[14] PUT /api/Orders/{id}/assign/{driverId}..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Orders/$orderId/assign/$driverId" -Method PUT -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 15. Obtener mi ruta (Driver)
Write-Host "[15] GET /api/Orders/my-route (Driver)..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Orders/my-route" -Method GET -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   Orders: $($resp.orders.Count)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# ========== RUTAS GUARDADAS ==========
Write-Host ""
Write-Host "=== RUTAS GUARDADAS ===" -ForegroundColor Yellow

# 16. Guardar ruta
Write-Host "[16] POST /api/Routes/save..." -NoNewline
$saveRouteDto = @{
    routeName = "Ruta Test $timestamp"
    orderIds = @($orderId)
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/save" -Method POST -ContentType "application/json" -Headers @{Authorization="Bearer $driverToken"} -Body $saveRouteDto
    $routeId = $resp.routeId
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   RouteId: $routeId" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 17. Listar rutas guardadas
Write-Host "[17] GET /api/Routes/saved..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/saved" -Method GET -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    Write-Host "   TotalRoutes: $($resp.totalSavedRoutes)" -ForegroundColor Gray
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 18. Obtener ruta específica
Write-Host "[18] GET /api/Routes/saved/{id}..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/saved/$routeId" -Method GET -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 19. Cargar ruta
Write-Host "[19] POST /api/Routes/saved/{id}/load..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/saved/$routeId/load" -Method POST -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 20. Renombrar ruta
Write-Host "[20] POST /api/Routes/saved/{id}/rename..." -NoNewline
$renameDto = @{
    newName = "Ruta Renombrada $timestamp"
} | ConvertTo-Json

try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/saved/$routeId/rename" -Method POST -ContentType "application/json" -Headers @{Authorization="Bearer $driverToken"} -Body $renameDto
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 21. Desvincular conductor (ANTES de eliminar ruta para que no haya conflicto)
Write-Host "[21] DELETE /api/Drivers/{id} (desvincular)..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Drivers/$driverId" -Method DELETE -Headers @{Authorization="Bearer $adminToken"}
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# 22. Eliminar ruta
Write-Host "[22] DELETE /api/Routes/saved/{id}..." -NoNewline
try {
    $resp = Invoke-RestMethod -Uri "$api/api/Routes/saved/$routeId" -Method DELETE -Headers @{Authorization="Bearer $driverToken"}
    Write-Host " ✅" -ForegroundColor Green
    $passed++
} catch {
    Write-Host " ❌ $($_.Exception.Message)" -ForegroundColor Red
    $failed++
}

# ========== RESUMEN ==========
Write-Host ""
Write-Host "========== RESUMEN ==========" -ForegroundColor Cyan
Write-Host "✅ Exitosos: $passed" -ForegroundColor Green
Write-Host "❌ Fallidos: $failed" -ForegroundColor Red

if ($failed -eq 0) {
    Write-Host ""
    Write-Host "🎉 ¡TODOS LOS ENDPOINTS FUNCIONAN CORRECTAMENTE!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "⚠️ Algunos endpoints fallaron. Revisar logs." -ForegroundColor Yellow
}

