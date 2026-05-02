using System;
using System.IO;
using IglesiaAsistencia.Data;

namespace IglesiaAsistencia.Services
{
    public class BackupService
    {
        public void RealizarBackup(string destinationPath)
        {
            string dbPath = AppConfig.DatabasePath;
            
            if (File.Exists(dbPath))
            {
                File.Copy(dbPath, destinationPath, true);
            }
            else
            {
                throw new FileNotFoundException("La base de datos no fue encontrada.");
            }
        }

        public void RestaurarBackup(string sourcePath)
        {
            string dbPath = AppConfig.DatabasePath;
            
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, dbPath, true);
            }
        }
    }
}
