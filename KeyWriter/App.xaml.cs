using System.Threading;
using System.Windows;

namespace KeyWriter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _runLock;

        public App()
        {
            bool createNew;
            _runLock = new Mutex(true, "KeyWriter", out createNew);

            if (!createNew)
            {
                Shutdown();
            }
        }
    }
}
