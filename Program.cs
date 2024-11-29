using Avalonia;
using i18nTest.Models;
using System;
using System.Threading.Tasks;

namespace i18nTest;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        Task.Run(async () =>
        {
            await LocalizationManager.Instance.SearchAvailableLanguageAsync();
            await LocalizationManager.Instance.TrySetCurrentLocalization();
        });
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
                     .UsePlatformDetect()
                     .WithInterFont()
                     .LogToTrace();
}
