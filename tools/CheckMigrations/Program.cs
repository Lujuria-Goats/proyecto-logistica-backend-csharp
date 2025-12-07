using System;
using System.IO;
using System.Linq;
using Npgsql;

namespace CheckMigrationsTool
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                string conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                    ?? Environment.GetEnvironmentVariable("ConnectionStrings:DefaultConnection");

                // Rutas relativas comunes: ../../ApexVision.Backend/.env o ../../.env
                if (string.IsNullOrWhiteSpace(conn))
                {
                    var candidates = new[]
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "ApexVision.Backend", ".env"),
                        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
                        Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")
                    };

                    foreach (var path in candidates)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                var lines = File.ReadAllLines(path);
                                var kv = lines.Select(l => l.Trim())
                                              .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#"))
                                              .Select(l => l.Split('=', 2))
                                              .Where(parts => parts.Length == 2)
                                              .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());

                                if (kv.TryGetValue("ConnectionStrings__DefaultConnection", out var v))
                                {
                                    conn = v.Trim('"', '\'');
                                    break;
                                }
                                // try key with ConnectionStrings:DefaultConnection
                                if (kv.TryGetValue("ConnectionStrings:DefaultConnection", out var v2))
                                {
                                    conn = v2.Trim('"', '\'');
                                    break;
                                }
                            }
                        }
                        catch { /* ignore and continue */ }
                    }
                }

                if (string.IsNullOrWhiteSpace(conn))
                {
                    Console.Error.WriteLine("No se encontró la cadena de conexión. Define ConnectionStrings__DefaultConnection en las variables de entorno o en un .env accesible.");
                    return 2;
                }

                Console.WriteLine("Usando connection string: " + conn);

                using var connPg = new NpgsqlConnection(conn);
                connPg.Open();

                // CONSULTA GENERICA: no asumimos nombres de columnas (evita errores por diferentes casing o nombres)
                using var cmd = new NpgsqlCommand("SELECT * FROM \"__EFMigrationsHistory\" ORDER BY 1;", connPg);
                using var rdr = cmd.ExecuteReader();

                if (!rdr.HasRows)
                {
                    Console.WriteLine("No se encontraron filas en __EFMigrationsHistory (la tabla puede no existir o la DB está vacía de migraciones).");
                    return 0;
                }

                // Imprimir cabecera con nombres de columnas
                var fieldCount = rdr.FieldCount;
                for (int i = 0; i < fieldCount; i++)
                {
                    Console.Write(rdr.GetName(i) + (i == fieldCount - 1 ? "\n" : "\t"));
                }

                // Imprimir filas
                while (rdr.Read())
                {
                    for (int i = 0; i < fieldCount; i++)
                    {
                        var val = rdr.IsDBNull(i) ? "NULL" : rdr.GetValue(i).ToString();
                        Console.Write(val + (i == fieldCount - 1 ? "\n" : "\t"));
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                if (ex.InnerException != null) Console.Error.WriteLine(ex.InnerException.Message);
                return 1;
            }
        }
    }
}
