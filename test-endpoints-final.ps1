#!/usr/bin/env pwsh

$api = "http://localhost:5132"
$testResults = @()

function Test-Endpoint {
    param(
        [string]$name,
        [string]$method,
        [string]$endpoint,
        [object]$body,
        [string]$token
    )
    
    try {
        $headers = @{ 'Content-Type' = 'application/json' }
        if ($token) {
            $headers['Authorization'] = "Bearer $token"
        }
        
        Write-Host "🧪 $name..." -ForegroundColor Cyan -NoNewline
        
        $params = @{
            Uri = "$api$endpoint"
            Method = $method
            Headers = $headers
            ErrorAction = "Stop"
        }
        
        if ($body) {
            $params['Body'] = ($body | ConvertTo-Json -Depth 5)
        }
        
        $response = Invoke-WebRequest @params
        Write-Host " ✅ EXITOSO (${response.StatusCode})" -ForegroundColor Green
        
        $testResults += @{
            Test = $name
            Status = "✅ EXITOSO"
            Code = $response.StatusCode
        }
        
        return $response
    }
    catch {
        Write-Host " ❌ FALLO" -ForegroundColor Red
        
        # Intentar obtener el cuerpo del error
        $errorBody = ""
        try {
            if ($_.Exception.Response -ne $null) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "   Detalle: $errorBody" -ForegroundColor Yellow
            }
        } catch {}
        
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $testResults += @{
            Test = $name
            Status = "❌ FALLO"
            Error = $_.Exception.Message
        }
        
        return $null
    }
}

Write-Host "`n========== TEST DE ENDPOINTS APEXVISION ==========" -ForegroundColor Yellow

# TEST 1: Login Admin
Write-Host "`n[TEST 1] Login Admin" -ForegroundColor Magenta
$loginResp = Test-Endpoint `
    -name "POST /api/Auth/login (Admin)" `
    -method "POST" `
    -endpoint "/api/auth/login" `
    -body @{
        email = "admin@apexvision.com"
        password = "Admin123!"
    }

if ($loginResp) {
    $adminToken = ($loginResp.Content | ConvertFrom-Json).token
    Write-Host "   Token obtenido: $($adminToken.Substring(0, 30))..." -ForegroundColor Green
}

# TEST 2: Crear Pedido (Admin)
Write-Host "`n[TEST 2] Crear Pedido (Requiere Admin)" -ForegroundColor Magenta
if ($adminToken) {
    try {
        Write-Host "🧪 POST /api/Orders..." -ForegroundColor Cyan -NoNewline
        
        $orderBody = '{
            "address": "Calle Principal 123, Medellín",
            "latitude": 6.2442,
            "longitude": -75.5898,
            "description": "Test Delivery",
            "requiresEvidence": false
        }'
        
        $response = Invoke-WebRequest `
            -Uri "$api/api/Orders" `
            -Method POST `
            -Headers @{ 
                'Authorization' = "Bearer $adminToken"
                'Content-Type' = 'application/json'
            } `
            -Body $orderBody `
            -ErrorAction Stop
        
        Write-Host " ✅ EXITOSO (${response.StatusCode})" -ForegroundColor Green
        $orderId = ($response.Content | ConvertFrom-Json).orderId
        Write-Host "   Pedido creado: ID $orderId" -ForegroundColor Green
        
        $testResults += @{
            Test = "POST /api/Orders"
            Status = "✅ EXITOSO"
            Code = $response.StatusCode
        }
    }
    catch {
        Write-Host " ❌ FALLO" -ForegroundColor Red
        
        $errorBody = ""
        try {
            if ($_.Exception.Response -ne $null) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "   Detalle: $errorBody" -ForegroundColor Yellow
            }
        } catch {}
        
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $testResults += @{
            Test = "POST /api/Orders"
            Status = "❌ FALLO"
            Error = $_.Exception.Message
        }
    }
}

# TEST 3: Obtener todos los pedidos
Write-Host "`n[TEST 3] Listar Pedidos (Admin)" -ForegroundColor Magenta
Test-Endpoint `
    -name "GET /api/Orders" `
    -method "GET" `
    -endpoint "/api/Orders" `
    -token $adminToken | Out-Null

# TEST 4: Registrar Driver
Write-Host "`n[TEST 4] Registrar Driver" -ForegroundColor Magenta
$uniqueEmail = "driver$(Get-Date -Format 'yyyyMMddHHmmss')@test.com"
$driverResp = Test-Endpoint `
    -name "POST /api/Auth/register (Driver)" `
    -method "POST" `
    -endpoint "/api/auth/register" `
    -body @{
        email = $uniqueEmail
        fullName = "Test Driver User"
        password = "SecurePass123!"
        phoneNumber = "+573001234567"
        role = "Driver"
    }

# TEST 5: Login Driver
Write-Host "`n[TEST 5] Login Driver" -ForegroundColor Magenta
$driverLoginResp = Test-Endpoint `
    -name "POST /api/Auth/login (Driver)" `
    -method "POST" `
    -endpoint "/api/auth/login" `
    -body @{
        email = $uniqueEmail
        password = "SecurePass123!"
    }

if ($driverLoginResp) {
    $driverToken = ($driverLoginResp.Content | ConvertFrom-Json).token
    Write-Host "   Driver token obtenido: $($driverToken.Substring(0, 30))..." -ForegroundColor Green
    
    # Extraer driver ID del token de forma segura
    try {
        $parts = $driverToken.Split('.')
        if ($parts.Count -eq 3) {
            $payload = $parts[1]
            # Agregar padding si es necesario
            $padding = 4 - ($payload.Length % 4)
            if ($padding -ne 4) {
                $payload = $payload + ('=' * $padding)
            }
            $decoded = [System.Convert]::FromBase64String($payload)
            $decodedString = [System.Text.Encoding]::UTF8.GetString($decoded)
            $decodedJson = $decodedString | ConvertFrom-Json
            $driverId = $decodedJson.nameid
            Write-Host "   Driver ID: $driverId" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "   ⚠️ No se pudo extraer Driver ID del token: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# TEST 6: Asignar pedido al driver
if ($orderId -and $driverId) {
    Write-Host "`n[TEST 6] Asignar Pedido al Driver" -ForegroundColor Magenta
    Test-Endpoint `
        -name "PUT /api/Orders/{orderId}/assign/{driverId}" `
        -method "PUT" `
        -endpoint "/api/Orders/$orderId/assign/$driverId" `
        -token $adminToken | Out-Null
}

# TEST 7: Obtener ruta del driver
if ($driverToken) {
    Write-Host "`n[TEST 7] Obtener Ruta del Driver" -ForegroundColor Magenta
    Test-Endpoint `
        -name "GET /api/Orders/my-route" `
        -method "GET" `
        -endpoint "/api/Orders/my-route" `
        -token $driverToken | Out-Null
}

# TEST 8: Guardar Ruta
if ($orderId -and $driverToken) {
    Write-Host "`n[TEST 8] Guardar Ruta" -ForegroundColor Magenta
    $saveRouteResp = Test-Endpoint `
        -name "POST /api/Routes/save" `
        -method "POST" `
        -endpoint "/api/Routes/save" `
        -body @{
            orderIds = @($orderId)
            routeName = "Mi Ruta Test $(Get-Date -Format 'HHmmss')"
        } `
        -token $driverToken
    
    if ($saveRouteResp) {
        $routeContent = $saveRouteResp.Content | ConvertFrom-Json
        $routeId = $routeContent.routeId
        Write-Host "   Ruta guardada: ID $routeId" -ForegroundColor Green
    }
}

# TEST 9: Obtener rutas guardadas
if ($driverToken) {
    Write-Host "`n[TEST 9] Obtener Rutas Guardadas" -ForegroundColor Magenta
    Test-Endpoint `
        -name "GET /api/Routes/saved" `
        -method "GET" `
        -endpoint "/api/Routes/saved" `
        -token $driverToken | Out-Null
}

# TEST 10: Obtener ruta específica
if ($driverToken -and $routeId) {
    Write-Host "`n[TEST 10] Obtener Ruta Específica" -ForegroundColor Magenta
    Test-Endpoint `
        -name "GET /api/Routes/saved/{routeId}" `
        -method "GET" `
        -endpoint "/api/Routes/saved/$routeId" `
        -token $driverToken | Out-Null
}

# TEST 11: Cargar ruta guardada
if ($driverToken -and $routeId) {
    Write-Host "`n[TEST 11] Cargar Ruta Guardada" -ForegroundColor Magenta
    Test-Endpoint `
        -name "POST /api/Routes/saved/{routeId}/load" `
        -method "POST" `
        -endpoint "/api/Routes/saved/$routeId/load" `
        -token $driverToken | Out-Null
}

# TEST 12: Renombrar ruta
if ($driverToken -and $routeId) {
    Write-Host "`n[TEST 12] Renombrar Ruta" -ForegroundColor Magenta
    Test-Endpoint `
        -name "POST /api/Routes/saved/{routeId}/rename" `
        -method "POST" `
        -endpoint "/api/Routes/saved/$routeId/rename" `
        -body @{
            newName = "Ruta Renombrada $(Get-Date -Format 'HHmmss')"
        } `
        -token $driverToken | Out-Null
}

# TEST 13: Completar pedido
if ($orderId -and $driverToken) {
    Write-Host "`n[TEST 13] Completar Pedido" -ForegroundColor Magenta
    try {
        Write-Host "🧪 POST /api/Orders/{orderId}/complete..." -ForegroundColor Cyan -NoNewline
        
        $response = Invoke-WebRequest `
            -Uri "$api/api/Orders/$orderId/complete" `
            -Method POST `
            -Headers @{ 'Authorization' = "Bearer $driverToken" } `
            -ErrorAction Stop
        
        Write-Host " ✅ EXITOSO (${response.StatusCode})" -ForegroundColor Green
        $testResults += @{
            Test = "POST /api/Orders/{orderId}/complete"
            Status = "✅ EXITOSO"
            Code = $response.StatusCode
        }
    }
    catch {
        Write-Host " ❌ FALLO" -ForegroundColor Red
        
        # Intentar obtener detalles del error
        $errorBody = ""
        try {
            if ($_.Exception.Response -ne $null) {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorBody = $reader.ReadToEnd()
                $reader.Close()
                Write-Host "   Detalle: $errorBody" -ForegroundColor Yellow
            }
        } catch {}
        
        Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $testResults += @{
            Test = "POST /api/Orders/{orderId}/complete"
            Status = "❌ FALLO"
            Error = $_.Exception.Message
        }
    }
}

# TEST 14: Eliminar ruta
if ($driverToken -and $routeId) {
    Write-Host "`n[TEST 14] Eliminar Ruta" -ForegroundColor Magenta
    Test-Endpoint `
        -name "DELETE /api/Routes/saved/{routeId}" `
        -method "DELETE" `
        -endpoint "/api/Routes/saved/$routeId" `
        -token $driverToken | Out-Null
}

# Resumen
Write-Host "`n========== RESUMEN DE TESTS ==========" -ForegroundColor Yellow
$successCount = ($testResults | Where-Object { $_.Status -eq "✅ EXITOSO" }).Count
$failCount = ($testResults | Where-Object { $_.Status -eq "❌ FALLO" }).Count

Write-Host "`n✅ Exitosos: $successCount" -ForegroundColor Green
Write-Host "❌ Fallos: $failCount" -ForegroundColor Red

if ($failCount -eq 0) {
    Write-Host "`n🎉 ¡TODOS LOS TESTS PASARON!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️ Algunos tests fallaron. Revisa los detalles arriba." -ForegroundColor Yellow
}

