using Avalonia.Controls;
using i18nTest.ViewModels;

namespace i18nTest.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Closing += (s, e) =>
        {
            // release references from `LocalizationManager.Instance`
            if (DataContext is MainWindowViewModel vm) { vm.OnCompleted(); }
        };
    }
}