using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace i18nTest.Models;

public class LocalizationManager : IObservable<LanguageChanged>
{
    private readonly HashSet<Language> _availableLanguages = new();
    private readonly HashSet<IObserver<LanguageChanged>> _observers = new();
    private readonly LanguageChanged _onChanged = new();

    public IEnumerable<Language> AvailableLanguages => _availableLanguages;
    public Language CurrentLanguage { get; private set; } = s_builtinLanguage;
    public FrozenDictionary<string, string> CurrentLocalization { get; private set; } = s_builtinLocalization;

    private LocalizationManager() { }

    private void SendOnChangedToObservers()
    {
        Debug.WriteLine($"[DEBUG] language changed to \"{CurrentLanguage}\"");
        foreach (var observer in _observers) { observer.OnNext(_onChanged); }
    }

    private async Task<bool> TrySetLocalizationFromStreamAsync(Stream stream)
    {
        try {
            XElement ele = await XElement.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
            var localization = ele.Elements().ToDictionary(k => k.Name.ToString(), v => v.Value.ToString());
            CurrentLocalization = localization.ToFrozenDictionary();
            return true;
        }
        catch { return false; }
    }

    public async Task<IEnumerable<Language>> SearchAvailableLanguageAsync()
    {
        _availableLanguages.Clear();
        _availableLanguages.Add(s_builtinLanguage);

        if (!Directory.Exists(LanguageFolderPath)) { return AvailableLanguages; }

        foreach (var file in Directory.EnumerateFiles(LanguageFolderPath)) {
            if (Path.GetExtension(file) != LanguageFileExtension ||
                Path.GetFileNameWithoutExtension(file) == BuiltinLanguageFileName) { continue; }

            try {
                await using var fs = File.OpenRead(file);
                var languageName = await LoadLanguageNameFromStreamAsync(fs);
                _availableLanguages.Add(new Language(languageName, file));
            }
            catch {}
        }
        return AvailableLanguages;
    }

    public async Task<bool> TrySetCurrentLocalization()
    {
        var cultrueName = CultureInfo.CurrentCulture.Name;
        Debug.WriteLine($"[DEBUG] current culture is {cultrueName}");
        var language = AvailableLanguages.FirstOrDefault(x => x.ToString().Contains(cultrueName, StringComparison.OrdinalIgnoreCase));
        if (language is null) { return false; }
        return await TrySetLanguageAsync(language);
    }

    public async Task<bool> TrySetLanguageAsync(Language language)
    {
        //if (languageName == CurrentLanguageName) { return true; }
        if (language.FileName == s_builtinLanguage.FileName) {
            CurrentLanguage = s_builtinLanguage;
            CurrentLocalization = s_builtinLocalization;
            SendOnChangedToObservers();
            return true;
        }

        if (!File.Exists(language.FilePath)) { return false; }
        await using (var fs = File.OpenRead(language.FilePath)) {

            if (!await TrySetLocalizationFromStreamAsync(fs)) { return false; }
        }
        CurrentLanguage = language;
        SendOnChangedToObservers();
        return true;
    }

    public async Task<bool> TrySetLanguageFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) { return false; }
        await using (var fs = File.OpenRead(filePath)) {

            if (!await TrySetLocalizationFromStreamAsync(fs)) { return false; }
        }
        var languageName = CurrentLocalization.TryGetValue("LanguageName", out var v) ? v : "";
        CurrentLanguage = new Language(languageName, filePath);
        SendOnChangedToObservers();
        return true;
    }

    public string GetLocalizedString(string key)
    {
        if (CurrentLocalization.TryGetValue(key, out var v)) { return v; }
        if (s_builtinLocalization.TryGetValue(key, out v)) { return v; }
#if DEBUG
        return $"|\"{CurrentLanguage.FilePath}\":{key}|";
#else
        return key;
#endif
    }

    public IDisposable Subscribe(IObserver<LanguageChanged> observer)
    {
        if (_observers.Add(observer)) { observer.OnNext(_onChanged); }
        return new Unsubscriber(_observers, observer);
    }


    private static readonly Language s_builtinLanguage = new("ɥsᴉlƃuƎ", "uᴉ-ʇlᴉnq");
    private static readonly FrozenDictionary<string, string> s_builtinLocalization = new Dictionary<string, string> {
        { "LanguageName", s_builtinLanguage.Name },
        { "SelectLanguage", "ǝƃɐnƃuɐ˥ ʇɔǝlǝS: " },
        { "UpdateLanguagesToolTip", "sǝƃɐnƃuɐl ǝlqɐlᴉɐʌɐ ǝʇɐpd∩" },
    }.ToFrozenDictionary();

    public static string LanguageFolderPath { get; } = Path.Join(AppContext.BaseDirectory, "Lang");
    public static string LanguageFileExtension { get; } = ".xaml";
    public static string BuiltinLanguageFileName { get; } = ".Builtin";
    public static LocalizationManager Instance { get; } = new();

    private static async Task<string> LoadLanguageNameFromStreamAsync(Stream stream)
    {
        var root = await XElement.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
        var eles = root.Elements("LanguageName");
        return eles.Any() ? eles.First().Value : "";
    }

    public static async Task SaveBuiltinLocalization()
    {
        var root = new XElement("Root", s_builtinLocalization.Select(kv => new XElement(kv.Key, kv.Value)));
        if (!Directory.Exists(LanguageFolderPath)) { Directory.CreateDirectory(LanguageFolderPath); }
        await using var fs = File.OpenWrite(Path.Join(LanguageFolderPath, BuiltinLanguageFileName + LanguageFileExtension));
        await root.SaveAsync(fs, SaveOptions.OmitDuplicateNamespaces, CancellationToken.None);
    }
}

public sealed class Language(string name, string filePath) : IEquatable<Language>
{
    public string Name { get; } = name;
    public string FilePath { get; } = filePath;
    public string FileName { get; } = Path.GetFileNameWithoutExtension(filePath);

    public override string ToString() => string.IsNullOrWhiteSpace(Name) ? $"({FileName})" : $"{Name} ({FileName})";

    public override bool Equals(object? obj) => Equals(obj as Language);
    public bool Equals(Language? obj) => obj is Language other && Path.Equals(FilePath, other.FilePath);
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(FilePath);
}

public sealed class LanguageChanged { }

internal sealed class Unsubscriber(ISet<IObserver<LanguageChanged>> observers, IObserver<LanguageChanged> observer) : IDisposable
{
    private readonly ISet<IObserver<LanguageChanged>> _observers = observers;
    private readonly IObserver<LanguageChanged> _observer = observer;

    public void Dispose() => _observers.Remove(_observer);
}