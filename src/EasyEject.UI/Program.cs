using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace EasyEject.UI;

/// <summary>
/// WinUI 3 entry point. The XAML-generated main is disabled in the project
/// file (DISABLE_XAML_GENERATED_MAIN) so the app can build its own host.
/// </summary>
public static class Program
{
    /// <summary>
    /// The application entry point.
    /// </summary>
    [STAThread]
    private static void Main(string[] args)
    {
        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }
}
