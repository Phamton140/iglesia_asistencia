using System;
using System.IO;

namespace IglesiaAsistencia.Data
{
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
        public static string ConfigPath => Path.Combine(BasePath, "config.txt");
    }
}
