
using System.Windows;
using System.Windows.Threading;

namespace Gemcom.Klabr
{
    public partial class App
    {
        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            string message = string.Format("An unhandled exception has occurred \n\n{0}", e.Exception);
            MessageBox.Show(message);
            e.Handled = true;
        }
    }
}
