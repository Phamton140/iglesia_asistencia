using System;
using System.Windows;

namespace IglesiaAsistencia
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Detector de errores globales
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                MessageBox.Show($"Error crítico: {ex.ExceptionObject}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            try 
            {
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar: {ex.Message}", "Error de Carga", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
