#!/usr/bin/env pwsh

# Detener procesos existentes
Write-Host "Deteniendo procesos..."
Get-Process dotnet -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 5

# Ejecutar la aplicación en background
Write-Host "Iniciando aplicación..."
$proc = Start-Process -FilePath "dotnet" -ArgumentList "run" -WorkingDirectory "C:\Users\user\OneDrive\Desktop\ApexVision\ApexVision.Backend" -NoNewWindow -PassThru

# Esperar a que inicie
Write-Host "Esperando a que inicie..."
Start-Sleep -Seconds 40

# Verificar si está respondiendo
Write-Host "Verificando si el servidor está activo..."
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5132/swagger/index.html" -UseBasicParsing -ErrorAction Stop
    Write-Host "✅ Servidor está activo (Status: $($response.StatusCode))"
    
    # Ahora ejecutar el test
    Write-Host ""
    Write-Host "Ejecutando test de autenticación JWT..."
    Write-Host ""
    & powershell -ExecutionPolicy Bypass -File "C:\Users\user\OneDrive\Desktop\ApexVision\test-jwt.ps1"
} catch {
    Write-Host "❌ Servidor no responde: $($_.Exception.Message)"
    Write-Host ""
    Write-Host "Intentando obtener logs del proceso..."
    Get-Process dotnet | ForEach-Object {
        Write-Host "Proceso: $($_.ProcessName) (PID: $($_.Id))"
    }
}

