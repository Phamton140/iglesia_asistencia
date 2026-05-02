using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace IglesiaAsistencia.Data
{
    public class ConfigData
    {
        public string NombreIglesia { get; set; } = "Comunidad Del Reino";
        public string? LogoPath { get; set; }
        public string RutaBackup { get; set; } = string.Empty;
        public string RutaReportes { get; set; } = string.Empty;
        
        // Categoría (string) -> [Dom, Lun, Mar, Mie, Jue, Vie, Sab]
        // Índice: 0=Dom, 1=Lun, 2=Mar, 3=Mie, 4=Jue, 5=Vie, 6=Sab
        public Dictionary<string, bool[]> DefaultCommitments { get; set; } = new();

        public ConfigData()
        {
            // Inicializar con valores base
            DefaultCommitments["Pastor"] = new[] { true, true, true, true, true, true, true };
            DefaultCommitments["Diacono"] = new[] { true, false, false, true, true, false, true };
            DefaultCommitments["Miembro"] = new[] { true, false, false, true, true, false, false };
            DefaultCommitments["Visita"] = new[] { false, false, false, false, false, false, false };
            DefaultCommitments["Seguimiento"] = new[] { true, false, false, false, false, false, false };
        }
    }

    public static class AppConfig
    {
        private static string? _basePath;

        public static string BasePath
        {
            get
            {
                if (_basePath == null)
                {
                    string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    _basePath = Path.Combine(folder, "IglesiaAsistencia");
                    if (!Directory.Exists(_basePath))
                    {
                        Directory.CreateDirectory(_basePath);
                    }
                }
                return _basePath;
            }
        }

        public static string DatabasePath => Path.Combine(BasePath, "iglesia.db");
        public static string ConfigPath => Path.Combine(BasePath, "config.json");

        public static ConfigData Load()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
                }
                catch { return new ConfigData(); }
            }
            return new ConfigData();
        }

        public static void Save(ConfigData data)
        {
            try
            {
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}
