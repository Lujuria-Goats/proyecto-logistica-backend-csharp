using System.Diagnostics;

namespace ApexVision.Backend
{
    public class CommandExecutor
    {
        public async Task ExecuteCommandAsync(string command)
        {
            try
            {
                Console.WriteLine($"[EXECUTOR] Ejecutando: {command}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash", // Usa "cmd.exe" si pruebas en Windows
                    Arguments = $"-c \"{command}\"", // Usa "/c" si es Windows
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processInfo };
                process.Start();

                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync();

                if (!string.IsNullOrEmpty(output)) Console.WriteLine($"OUT: {output}");
                if (!string.IsNullOrEmpty(error)) Console.WriteLine($"ERR: {error}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Falló ejecución: {ex.Message}");
            }
        }
    }
}