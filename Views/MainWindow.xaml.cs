using System.Windows;
using IglesiaAsistencia.ViewModels;

namespace IglesiaAsistencia.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Mostramos un aviso rápido
                var result = MessageBox.Show("Cerrando el sistema y realizando copia de seguridad automática. ¿Desea continuar?", "Saliendo", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }

                vm.EjecutarBackupAutomatico();
                MessageBox.Show("Copia de seguridad completada con éxito. El programa se cerrará ahora.", "Backup Finalizado", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            base.OnClosing(e);
        }
    }
}
