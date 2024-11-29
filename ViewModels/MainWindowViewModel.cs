using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using i18nTest.Models;

namespace i18nTest.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IObserver<LanguageChanged>
{
    private readonly IDisposable _cancellation;
    [ObservableProperty]
    private string _selectLanguageText = "";
    [ObservableProperty]
    private string _updateLanguagesToolTip = "";
    [ObservableProperty]
    private int _selectedLanguageIndex = -1;
    [ObservableProperty]
    private bool _notChangingLanguage = false;

    public ObservableCollection<Language> AvailableLanguages { get; private set; } = new();
    //public string SelectLanguageText { get; set; }
    //public int SelectedLanguageIndex { get; set; }
    //public bool NotChangingLanguage { get; set; }
    //public IAsyncRelayCommand UpdateLanguagesCommand { get; }

    public MainWindowViewModel()
    {
        _cancellation = LocalizationManager.Instance.Subscribe(this);
        Task.Run(async () =>
        {
            await UpdateAvailableLanguagesAsync();
            NotChangingLanguage = true;
        });
    }

    public virtual void OnCompleted() => _cancellation.Dispose();
    public virtual void OnError(Exception _) { }
    public virtual void OnNext(LanguageChanged _) => UpdateLocalized();

    private void UpdateLocalized()
    {
        SelectLanguageText = LocalizationManager.Instance.GetLocalizedString("SelectLanguage");
        UpdateLanguagesToolTip = LocalizationManager.Instance.GetLocalizedString("UpdateLanguagesToolTip");
    }

    private async Task UpdateAvailableLanguagesAsync(bool doSearch = false)
    {
        Debug.WriteLine("[DEBUG] updating `AvailableLanguages`");

        if (doSearch) { await LocalizationManager.Instance.SearchAvailableLanguageAsync(); }

        AvailableLanguages.Clear();
        foreach (var language in LocalizationManager.Instance.AvailableLanguages) {
            AvailableLanguages.Add(language);
        }

        SelectedLanguageIndex = AvailableLanguages.IndexOf(LocalizationManager.Instance.CurrentLanguage);
        Debug.WriteLine($"[DEBUG] change selected language index to {SelectedLanguageIndex}");
    }

    [RelayCommand]
    private async Task UpdateLanguagesAsync()
    {
        NotChangingLanguage = false;
        await UpdateAvailableLanguagesAsync(doSearch: true);
        NotChangingLanguage = true;
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        if (!NotChangingLanguage) { return; }

        if (value < 0 || value > AvailableLanguages.Count) {
            Debug.WriteLine($"[DEBUG] selected language index out of range: {value} not in [0,{AvailableLanguages.Count})");
            return;
        }

        NotChangingLanguage = false;

        var selectedLanguage = AvailableLanguages[value];
        Debug.WriteLine($"[DEBUG] selected language name: {selectedLanguage}");

        if (selectedLanguage == LocalizationManager.Instance.CurrentLanguage) {
            NotChangingLanguage = true;
            return;
        }

        Task.Run(async () =>
        {
            if (!await LocalizationManager.Instance.TrySetLanguageAsync(selectedLanguage)) {
                await UpdateAvailableLanguagesAsync(doSearch: true);
            }
            NotChangingLanguage = true;
        });
    }
}