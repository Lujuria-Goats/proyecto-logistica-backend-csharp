param(
    [string]$BaseUrl = 'http://localhost:5132',
    [string]$AdminIdentifier = 'admin@apexvision.com',
    [string]$AdminPassword = 'Admin123!'
)

$ErrorActionPreference = 'Stop'
Write-Host "BaseUrl: $BaseUrl"`n
function Safe-Invoke([scriptblock]$call) {
    try { & $call } catch { Write-Host "ERROR: $($_.Exception.Message)"; if ($_.Exception.Response) { try { $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream()); Write-Host "RESPONSE BODY:"; Write-Host $sr.ReadToEnd(); } catch {} } ; return $null }
}

# 1. Login Admin
Write-Host "== 1) Login Admin =="
$login = @{ identifier = $AdminIdentifier; password = $AdminPassword } | ConvertTo-Json
$resp = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Auth/login" -Method Post -ContentType 'application/json' -Body $login }
if (-not $resp) { Write-Host "Fallo login admin. Abortando tests."; exit 1 }
$adminToken = $resp.token
Write-Host "Admin token (trunc): $($adminToken.Substring(0,[Math]::Min(60,$adminToken.Length)))"

# 2. Create order
Write-Host "\n== 2) Crear pedido =="
$orderPayload = @{ address='Test Address'; latitude=6.198; longitude=-75.578; description='Prueba E2E'; requiresEvidence=$false } | ConvertTo-Json
$ord = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Orders" -Method Post -ContentType 'application/json' -Headers @{ Authorization = "Bearer $adminToken" } -Body $orderPayload }
if ($ord -ne $null) { $orderId = $ord.orderId; Write-Host "OrderId: $orderId" } else { Write-Host "Crear pedido falló" }

# 3. Register driver (random)
Write-Host "\n== 3) Registrar driver =="
$rand = (Get-Random -Maximum 99999)
$driverPhone = "30012$rand"
$driverEmail = "testdriver$rand@test.com"
$reg = @{ fullName='Test Driver'; email=$driverEmail; password='Pass@123456!'; phoneNumber=$driverPhone } | ConvertTo-Json
$regResp = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Auth/register/driver" -Method Post -ContentType 'application/json' -Body $reg }
if ($regResp -ne $null) { Write-Host "Driver registered: $driverEmail / $driverPhone"; $driverId = $regResp.userId } else { Write-Host "Registro driver falló (podría existir)" }

# 4. Login driver
Write-Host "\n== 4) Login Driver =="
$loginD = @{ identifier = $driverPhone; password = 'Pass@123456!' } | ConvertTo-Json
$dresp = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Auth/login" -Method Post -ContentType 'application/json' -Body $loginD }
if ($dresp -ne $null) { $driverToken = $dresp.token; Write-Host "Driver token (trunc): $($driverToken.Substring(0,[Math]::Min(60,$driverToken.Length)))" } else { Write-Host "Login driver falló" }

# 5. Assign driver to order
if ($orderId -and $driverId) {
    Write-Host "\n== 5) Asignar pedido =="
    $assign = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Orders/$orderId/assign/$driverId" -Method Put -Headers @{ Authorization = "Bearer $adminToken" } }
    if ($assign -ne $null) { Write-Host "Assign OK" }
}

# 6. Save route (driver)
Write-Host "\n== 6) Guardar ruta (driver) =="
$save = @{ orderIds = @( $orderId ); routeName = 'Ruta E2E Test' } | ConvertTo-Json
$saveResp = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Routes/save" -Method Post -ContentType 'application/json' -Headers @{ Authorization = "Bearer $driverToken" } -Body $save }
if ($saveResp -ne $null) { Write-Host "Save route OK: routeId = $($saveResp.routeId)"; $routeId = $saveResp.routeId } else { Write-Host "Save route falla" }

# 7. Get saved routes
Write-Host "\n== 7) Listar rutas guardadas (driver) =="
$list = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Routes/saved" -Method Get -Headers @{ Authorization = "Bearer $driverToken" } }
if ($list -ne $null) { Write-Host ($list | ConvertTo-Json -Depth 5) }

# 8. Load saved route
if ($routeId) {
    Write-Host "\n== 8) Cargar ruta guardada =="
    $load = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Routes/saved/$routeId/load" -Method Post -Headers @{ Authorization = "Bearer $driverToken" } }
    if ($load -ne $null) { Write-Host ($load | ConvertTo-Json -Depth 5) }

    Write-Host "\n== 9) Rename saved route =="
    $renameBody = @{ newName = 'Ruta E2E Renombrada' } | ConvertTo-Json
    $rn = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Routes/saved/$routeId/rename" -Method Post -Headers @{ Authorization = "Bearer $driverToken" } -ContentType 'application/json' -Body $renameBody }
    if ($rn -ne $null) { Write-Host ($rn | ConvertTo-Json -Depth 5) }

    Write-Host "\n== 10) Delete saved route =="
    $del = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Routes/saved/$routeId" -Method Delete -Headers @{ Authorization = "Bearer $driverToken" } }
    if ($del -ne $null) { Write-Host ($del | ConvertTo-Json -Depth 5) }
}

# 11. Drivers: list linked
Write-Host "\n== 11) Listar conductores vinculados (admin) =="
$linked = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Drivers" -Method Get -Headers @{ Authorization = "Bearer $adminToken" } }
if ($linked -ne $null) { Write-Host ($linked | ConvertTo-Json -Depth 5) }

# 12. Search driver by phone
Write-Host "\n== 12) Buscar conductor por phone (admin) =="
$search = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Drivers/search?phone=$driverPhone" -Method Get -Headers @{ Authorization = "Bearer $adminToken" } }
if ($search -ne $null) { Write-Host ($search | ConvertTo-Json -Depth 5) }

# 13. Try link driver (admin)
Write-Host "\n== 13) Intentar vincular conductor (admin) =="
$linkBody = @{ phoneNumber = $driverPhone } | ConvertTo-Json
$linkTry = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Drivers/link" -Method Post -Headers @{ Authorization = "Bearer $adminToken" } -ContentType 'application/json' -Body $linkBody }
if ($linkTry -ne $null) { Write-Host ($linkTry | ConvertTo-Json -Depth 5) }

# 14. Unlink driver
Write-Host "\n== 14) Desvincular conductor (admin) =="
$unlink = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Drivers/$driverId" -Method Delete -Headers @{ Authorization = "Bearer $adminToken" } }
if ($unlink -ne $null) { Write-Host ($unlink | ConvertTo-Json -Depth 5) }

# 15. Get all orders (admin)
Write-Host "\n== 15) Listar pedidos (admin) =="
$orders = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Orders" -Method Get -Headers @{ Authorization = "Bearer $adminToken" } }
if ($orders -ne $null) { Write-Host ($orders | ConvertTo-Json -Depth 5) }

# 16. Driver get my-route
Write-Host "\n== 16) Driver -> GET my-route =="
$myroute = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Orders/my-route" -Method Get -Headers @{ Authorization = "Bearer $driverToken" } }
if ($myroute -ne $null) { Write-Host ($myroute | ConvertTo-Json -Depth 5) }

# 17. Optimize my route (driver)
Write-Host "\n== 17) Driver -> Optimize my route =="
$opt = Safe-Invoke { Invoke-RestMethod -Uri "$BaseUrl/api/Orders/my-route/optimize" -Method Post -Headers @{ Authorization = "Bearer $driverToken" } }
if ($opt -ne $null) { Write-Host ($opt | ConvertTo-Json -Depth 5) }

Write-Host "\n=== TEST SUITE COMPLETED ==="

