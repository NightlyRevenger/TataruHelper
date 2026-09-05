using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using FFXIVTataruHelper.Services.Diagnostics;
using FFXIVTataruHelper.Services.HotKeys;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.Update;
using FFXIVTataruHelper.Theme;
using FFXIVTataruHelper.Utils;

using Translation.Reference;

using Wpf.Ui.Controls;

namespace FFXIVTataruHelper.ViewModel.Shell;

public sealed class SettingsShellViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly TataruViewModel _settingsViewModel;
    private readonly TataruUIModel _uiModel;
    private readonly IHotkeyCaptureService _hotkeyCaptureService;
    private readonly Action _checkUpdatesAction;
    private readonly IReferenceIndexUpdateService _referenceIndexUpdateService;
    private readonly ISettingsResetService _settingsResetService;
    private readonly IDiagnosticsReporter _diagnosticsReporter;

    /// <summary>
    /// Where the daily check leaves a trace. Nobody watches it happen, so
    /// without this the only evidence it ran at all is a line of text in a
    /// window that may never have been opened.
    /// </summary>
    private readonly IAppLogger _logger;

    /// <summary>
    /// Reads an interface string as the window has it.
    ///
    /// Not from the application's own resources: the translated strings are put
    /// on the settings window, and the application keeps the English defaults.
    /// Reading the wrong one is how the game-attached indicator stayed red on
    /// every non-English interface.
    /// </summary>
    private readonly Func<string, string> _localize;

    /// <summary>
    /// Puts a question to the user and waits for the answer. False means no,
    /// and nothing is done.
    /// </summary>
    private readonly Func<string, bool> _confirm;

    /// <summary>Starts the copy that takes over once this one has gone.</summary>
    private readonly Action _restart;

    private CancellationTokenSource _referenceIndexUpdateCancellation;

    /// <summary>Cancels whatever the daily check is in the middle of, at shutdown.</summary>
    private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

    private readonly DispatcherTimer _referenceIndexCheckTimer;

    private string _referenceIndexStatus;
    private string _referenceIndexProgress;

    /// <summary>
    /// Starts true, and only a definite answer takes it away: the saved
    /// windows arrive on a background thread, and a section that vanishes and
    /// comes back as they land is worse than one that stays.
    /// </summary>
    private bool _isReferenceTranslationUsable = true;

    /// <summary>
    /// The window whose reading language is being watched. Only the selected
    /// one: it is the only one whose language anybody can be changing.
    /// </summary>
    private ChatWindowViewModel _watchedChatWindow;

    private bool _disposed;
    private SettingsSectionItem _selectedSection;
    private LanguageOption _selectedLanguageOption;
    private string _ffStatusText;

    private bool _ffStatusActive;
    private readonly string _appVersion;

    private string _diagnosticsStatus = string.Empty;

    public TranslationCredentialsViewModel TranslationCredentials { get; }

    public SettingsShellViewModel(
        TataruViewModel settingsViewModel,
        TataruUIModel uiModel,
        IHotkeyCaptureService hotkeyCaptureService,
        Action checkUpdatesAction,
        TranslationCredentialsViewModel translationCredentials,
        IReferenceIndexUpdateService referenceIndexUpdateService,
        ISettingsResetService settingsResetService,
        IAppLogger logger,
        Func<string, string> localize,
        Func<string, bool> confirm,
        Action restart,
        IDiagnosticsReporter diagnosticsReporter = null)
    {
        _diagnosticsReporter = diagnosticsReporter;
        _settingsViewModel = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
        _uiModel = uiModel ?? throw new ArgumentNullException(nameof(uiModel));
        _hotkeyCaptureService = hotkeyCaptureService ?? throw new ArgumentNullException(nameof(hotkeyCaptureService));
        _checkUpdatesAction = checkUpdatesAction ?? throw new ArgumentNullException(nameof(checkUpdatesAction));
        TranslationCredentials =
            translationCredentials ?? throw new ArgumentNullException(nameof(translationCredentials));
        _referenceIndexUpdateService = referenceIndexUpdateService
                                       ?? throw new ArgumentNullException(nameof(referenceIndexUpdateService));
        _settingsResetService = settingsResetService ?? throw new ArgumentNullException(nameof(settingsResetService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _localize = localize ?? (key => key);
        _confirm = confirm ?? (_ => true);
        _restart = restart ?? (() => { });

        Sections = new ObservableCollection<SettingsSectionItem>
        {
            new(SettingsSection.ChatWindows, "SidebarGroupChatWindows", "Chat Windows",
                "ChatWindowsTab", "Chat Windows", SymbolRegular.Chat24),
            new(SettingsSection.Appearance, "SidebarGroupPerWindow", "Per Window Settings",
                "SectionAppearance", "Appearance", SymbolRegular.ColorBackground24),
            new(SettingsSection.Hotkeys, "SidebarGroupPerWindow", "Per Window Settings",
                "ChatWindowHotkeys", "Hotkeys", SymbolRegular.Keyboard24),
            new(SettingsSection.Translation, "SidebarGroupPerWindow", "Per Window Settings",
                "SectionTranslation", "Translation", SymbolRegular.Translate24),
            new(SettingsSection.General, "SidebarGroupApplication", "Application",
                "SectionGeneral", "General", SymbolRegular.Settings24),
            new(SettingsSection.About, "SidebarGroupApplication", "Application",
                "DockAbout", "About", SymbolRegular.Info24)
        };

        ThemeOptions = new ObservableCollection<ThemeOption>
        {
            new(AppThemeMode.System, "ThemeSystem", "System"),
            new(AppThemeMode.Light, "ThemeLight", "Light"),
            new(AppThemeMode.Dark, "ThemeDark", "Dark")
        };

        Languages = new ObservableCollection<LanguageOption>
        {
            new("English", "English"),
            new("Russian", "Русский"),
            new("Spanish", "Español"),
            new("Polish", "Polski"),
            new("Korean", "한국어"),
            new("PortugueseBR", "Português brasileiro"),
            new("Catalan", "Català"),
            new("Italian", "Italiano"),
            new("Japanese", "日本語"),
            new("Ukrainian", "Українська"),
            new("Chinese", "汉语"),
            new("ChineseTR", "繁體中文")
        };

        SwitchLanguageCommand = new TataruUICommand(ExecuteSwitchLanguage);
        CheckUpdatesCommand = new TataruUICommand(() => _checkUpdatesAction());
        UpdateReferenceIndexCommand = new TataruUICommand(StartReferenceIndexUpdate);
        CancelReferenceIndexUpdateCommand = new TataruUICommand(CancelReferenceIndexUpdate);
        ResetSettingsCommand = new TataruUICommand(ResetSettings);
        CopyDiagnosticsCommand = new TataruUICommand(CopyDiagnostics);
        SelectChatWindowCommand = new TataruUICommand(SelectChatWindowByParameter);
        AddWindowCommand = new TataruUICommand(() => _settingsViewModel.AddNewChatWindowCommand.Execute(null));
        DeleteWindowCommand = new TataruUICommand(() => _settingsViewModel.DeleteChatWindowCommand.Execute(null));
        ShowHideWindowCommand = new TataruUICommand(ToggleCurrentWindowVisibility);
        ResetPositionCommand = new TataruUICommand(ResetCurrentWindowPosition);

        _selectedSection = Sections.First(x => x.Section == SettingsSection.ChatWindows);
        _selectedLanguageOption = ResolveLanguageOption(_uiModel.UiLanguage);
        _ffStatusText = string.Empty;
        _referenceIndexProgress = string.Empty;
        _appVersion = "v" + (Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "unknown");

        RefreshSectionTitles();
        RefreshReferenceIndexStatus();
        RefreshReferenceTranslationUse();

        _settingsViewModel.PropertyChanged += OnSettingsViewModelPropertyChanged;
        _uiModel.PropertyChanged += OnUiModelPropertyChanged;

        // A dispatcher timer, so the check arrives on the thread that reads
        // the current chat window and writes the status line - the same
        // thread, and the same code, as pressing the button.
        _referenceIndexCheckTimer = new DispatcherTimer { Interval = FirstCheckDelay };
        _referenceIndexCheckTimer.Tick += OnReferenceIndexCheckDue;
        _referenceIndexCheckTimer.Start();
    }

    /// <summary>
    /// How long after starting the first check happens.
    ///
    /// Not at once: the saved settings are still being read, the game may not
    /// be attached yet, and somebody who has just opened the application is
    /// about to play rather than to read about downloads.
    /// </summary>
    private static readonly TimeSpan FirstCheckDelay = TimeSpan.FromMinutes(1);

    public event PropertyChangedEventHandler PropertyChanged;

    public ObservableCollection<SettingsSectionItem> Sections { get; }

    public ObservableCollection<LanguageOption> Languages { get; }

    public ObservableCollection<ThemeOption> ThemeOptions { get; }

    public ThemeOption SelectedThemeOption
    {
        get => ThemeOptions.FirstOrDefault(o => o.Mode == AppThemeService.CurrentMode) ?? ThemeOptions[0];
        set
        {
            if (value == null || value.Mode == AppThemeService.CurrentMode)
            {
                return;
            }

            AppThemeService.Apply(value.Mode);
            OnPropertyChanged();
        }
    }

    public TataruViewModel SettingsViewModel => _settingsViewModel;

    public SettingsSectionItem SelectedSection
    {
        get => _selectedSection;
        set
        {
            if (ReferenceEquals(_selectedSection, value) || value == null)
            {
                return;
            }

            _selectedSection = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedSectionKey));
            OnPropertyChanged(nameof(ShowActiveWindowSelector));
        }
    }

    public SettingsSection SelectedSectionKey => SelectedSection.Section;

    public long SelectedChatWindowId
    {
        get => CurrentChatWindow?.WinId ?? -1;
        set
        {
            var index = _settingsViewModel.ChatWindows.ToList().FindIndex(x => x.WinId == value);
            if (index >= 0 && _settingsViewModel.SelectedTabIndex != index)
            {
                _settingsViewModel.SelectedTabIndex = index;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentChatWindow));
            }
        }
    }

    public ChatWindowViewModel CurrentChatWindow => _settingsViewModel.CurrentChatWindow;

    /// <summary>
    /// Translates NPC dialogue as it appears on screen instead of waiting for it
    /// to reach the chat log, and drops the chat-log copies so nothing is shown
    /// twice.
    /// </summary>
    public bool IsRealtimeTranslation
    {
        get => _uiModel.IsRealtimeTranslation;
        set
        {
            if (_uiModel.IsRealtimeTranslation == value)
            {
                return;
            }

            _uiModel.IsRealtimeTranslation = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Use the hand-made translation of a line when there is one.</summary>
    public bool IsLiteraryTranslation
    {
        get => _uiModel.IsLiteraryTranslation;
        set
        {
            if (_uiModel.IsLiteraryTranslation == value)
            {
                return;
            }

            _uiModel.IsLiteraryTranslation = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Mark lines an engine translated, leaving hand-made ones plain.</summary>
    public bool IsMachineTranslationMarked
    {
        get => _uiModel.IsMachineTranslationMarked;
        set
        {
            if (_uiModel.IsMachineTranslationMarked == value)
            {
                return;
            }

            _uiModel.IsMachineTranslationMarked = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Show the speaker's name in the reading language too.</summary>
    public bool IsSpeakerNameTranslated
    {
        get => _uiModel.IsSpeakerNameTranslated;
        set
        {
            if (_uiModel.IsSpeakerNameTranslated == value)
            {
                return;
            }

            _uiModel.IsSpeakerNameTranslated = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Show player-chat sender names in the reading language.</summary>
    public bool IsPlayerNicknameTranslated
    {
        get => _uiModel.IsPlayerNicknameTranslated;
        set
        {
            if (_uiModel.IsPlayerNicknameTranslated == value)
            {
                return;
            }

            _uiModel.IsPlayerNicknameTranslated = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether the daily check may act on what it finds, or only report it.
    ///
    /// Off unless asked for: the export is around a gigabyte, and taking that
    /// off somebody's connection unprompted is not a default.
    /// </summary>
    public bool IsReferenceIndexAutoInstall
    {
        get => _uiModel.IsReferenceIndexAutoInstall;
        set
        {
            if (_uiModel.IsReferenceIndexAutoInstall == value)
            {
                return;
            }

            _uiModel.IsReferenceIndexAutoInstall = value;
            OnPropertyChanged();
        }
    }

    /// <summary>What the installed index holds, in a line the user can read.</summary>
    public string ReferenceIndexStatus
    {
        get => _referenceIndexStatus;
        private set
        {
            if (string.Equals(_referenceIndexStatus, value, StringComparison.Ordinal))
            {
                return;
            }

            _referenceIndexStatus = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// How the update is getting on, and what became of it afterwards. Empty
    /// until the button is pressed for the first time.
    /// </summary>
    public string ReferenceIndexProgress
    {
        get => _referenceIndexProgress;
        private set
        {
            if (string.Equals(_referenceIndexProgress, value, StringComparison.Ordinal))
            {
                return;
            }

            _referenceIndexProgress = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasReferenceIndexProgress));
        }
    }

    public bool HasReferenceIndexProgress => !string.IsNullOrEmpty(_referenceIndexProgress);

    /// <summary>
    /// Whether the hand-made translations are worth a place on the page.
    ///
    /// They translate out of the language the game is played in, so a window
    /// reading that same language has nothing to look up. The whole section
    /// goes rather than sitting there greyed: a switch, a line about 202,080
    /// lines and a button offering a gigabyte are all answers to a question
    /// nobody in that position is asking.
    /// </summary>
    public bool IsReferenceTranslationUsable => _isReferenceTranslationUsable;

    public bool IsReferenceIndexUpdating => _referenceIndexUpdateCancellation != null;

    public bool CanUpdateReferenceIndex =>
        _referenceIndexUpdateService.IsSupported && !IsReferenceIndexUpdating;

    /// <summary>
    /// Whether a new chat window may be added yet. Not until the saved ones
    /// have been read in: one added before that takes a number they are about
    /// to use, and the saved window is dropped in its favour.
    /// </summary>
    public bool CanAddWindow => _uiModel.AreSettingsLoaded;

    public bool IsHideSettingsToTray
    {
        get => _uiModel.IsHideSettingsToTray;
        set
        {
            if (_uiModel.IsHideSettingsToTray == value)
            {
                return;
            }

            _uiModel.IsHideSettingsToTray = value;
            OnPropertyChanged();
        }
    }

    public string FfStatusText
    {
        get => _ffStatusText;
        set
        {
            if (string.Equals(_ffStatusText, value, StringComparison.Ordinal))
            {
                return;
            }

            _ffStatusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether the game process is currently attached.
    ///
    /// This used to be inferred by matching the status text against the
    /// "Process found:" resource, but the text is built from the window's
    /// localized dictionary while the comparison read the application's English
    /// defaults — so on any non-English UI the indicator stayed red even though
    /// the process was attached and translation was working.
    /// </summary>
    public bool FfStatusActive
    {
        get => _ffStatusActive;
        set
        {
            if (_ffStatusActive == value)
            {
                return;
            }

            _ffStatusActive = value;
            OnPropertyChanged();

            // Attaching to the game is when its language stops being a guess
            // from a configuration file and starts coming from the process.
            RefreshReferenceTranslationUse();
        }
    }

    public bool ShowActiveWindowSelector
    {
        get
        {
            var section = SelectedSection?.Section;
            return section == SettingsSection.Translation
                   || section == SettingsSection.Appearance
                   || section == SettingsSection.Hotkeys;
        }
    }

    public string AppVersion => _appVersion;

    public LanguageOption SelectedLanguageOption
    {
        get => _selectedLanguageOption;
        set
        {
            if (value == null || ReferenceEquals(_selectedLanguageOption, value))
            {
                return;
            }

            _selectedLanguageOption = value;
            OnPropertyChanged();
            SwitchLanguageCommand.Execute(value.Value);
        }
    }

    public TataruUICommand SwitchLanguageCommand { get; }

    public TataruUICommand CheckUpdatesCommand { get; }

    public TataruUICommand UpdateReferenceIndexCommand { get; }

    public TataruUICommand CancelReferenceIndexUpdateCommand { get; }

    public TataruUICommand ResetSettingsCommand { get; }

    public TataruUICommand CopyDiagnosticsCommand { get; }

    /// <summary>
    /// What came of the last press of the diagnostics button, shown beside it.
    /// A button that copies something invisibly and says nothing leaves the
    /// person wondering whether it worked.
    /// </summary>
    public string DiagnosticsStatus
    {
        get => _diagnosticsStatus;
        private set
        {
            if (string.Equals(_diagnosticsStatus, value, StringComparison.Ordinal))
            {
                return;
            }

            _diagnosticsStatus = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasDiagnosticsStatus));
        }
    }

    public bool HasDiagnosticsStatus => !string.IsNullOrEmpty(DiagnosticsStatus);

    public TataruUICommand SelectChatWindowCommand { get; }

    public TataruUICommand AddWindowCommand { get; }

    public TataruUICommand DeleteWindowCommand { get; }

    public TataruUICommand ShowHideWindowCommand { get; }

    public TataruUICommand ResetPositionCommand { get; }

    public void RegisterHotKeyDown(TatruHotkeyType type, KeyEventArgs args)
    {
        _hotkeyCaptureService.RegisterHotKeyDown(CurrentChatWindow, type, args);
    }

    public void RegisterHotKeyUp(TatruHotkeyType type, KeyEventArgs args)
    {
        _hotkeyCaptureService.RegisterHotKeyUp(CurrentChatWindow, type, args);
    }

    /// <summary>
    /// The button: build for the language this window is set to read.
    /// </summary>
    private void StartReferenceIndexUpdate()
    {
        BeginReferenceIndexUpdate(CurrentChatWindow?.CurrentTranslateToLanguage?.LanguageCode ?? string.Empty);
    }

    /// <param name="readingLanguage">
    /// Empty to keep reading whatever the installed index is in. That is what
    /// an update started by the daily check asks for: it decided on a pair
    /// before starting, and working it out a second time from a chat window
    /// could well produce a different one.
    /// </param>
    private void BeginReferenceIndexUpdate(string readingLanguage)
    {
        if (IsReferenceIndexUpdating || !_referenceIndexUpdateService.IsSupported)
        {
            return;
        }

        // The game may have been restarted in another language since this
        // started, so what would be built is worked out now - and if it is not
        // what is installed, it is asked about rather than done. Saying nothing
        // cost somebody a working index and a download to get it back.
        var (game, reading) = _referenceIndexUpdateService.ResolveLanguages(string.Empty, readingLanguage);

        var state = _referenceIndexUpdateService.ReadState();
        if (ReferenceIndexRebuild.ChangesLanguages(state, game, reading) &&
            !_confirm(ReferenceIndexRebuild.Question(state, game, reading, _localize)))
        {
            return;
        }

        _referenceIndexUpdateCancellation = new CancellationTokenSource();
        OnPropertyChanged(nameof(IsReferenceIndexUpdating));
        OnPropertyChanged(nameof(CanUpdateReferenceIndex));

        RunReferenceIndexUpdateAsync(readingLanguage, _referenceIndexUpdateCancellation.Token).Forget();
    }

    /// <summary>
    /// Asks the translation project whether it has moved, once a day.
    ///
    /// The question is a few kilobytes and the answer changes over weeks, so
    /// this is cheap enough to do unprompted and rare enough to be worth
    /// doing. Until it existed, an index only ever got newer by somebody
    /// wondering whether it might have - and a fresh installation with no
    /// translations at all said nothing about it either.
    /// </summary>
    private void OnReferenceIndexCheckDue(object sender, EventArgs e)
    {
        // The saved settings arrive on a background thread and may not have
        // landed yet, and none of what follows can be decided without them.
        // The interval is still the short one, so this comes round again in a
        // minute rather than tomorrow.
        if (!_uiModel.AreSettingsLoaded)
        {
            return;
        }

        if (_referenceIndexCheckTimer.Interval != ReferenceIndexAutoCheck.Interval)
        {
            _referenceIndexCheckTimer.Interval = ReferenceIndexAutoCheck.Interval;
        }

        // Nothing to say to somebody who has turned the hand-made translations
        // off, or who is reading in the language the game is already in: in
        // both cases the answer is news about a feature they are not using,
        // and in the second the page is not even showing it.
        if (!_uiModel.IsLiteraryTranslation ||
            !IsReferenceTranslationUsable ||
            !_referenceIndexUpdateService.IsSupported ||
            IsReferenceIndexUpdating)
        {
            return;
        }

        RunReferenceIndexCheckAsync(_lifetime.Token).Forget();
    }

    private async Task RunReferenceIndexCheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Neither language comes from a chat window here. The game's comes
            // from the game, as always; the reading language is left empty so
            // it falls back to the one the installed index is already in.
            //
            // Taking it from the current window instead is what the button
            // does, and it was wrong for this: the first check ever run
            // reported "your translations are for the wrong pair" against a
            // working en → ru index, because the selected window had no
            // language saved and its list starts at English. The button has a
            // human in front of it to disbelieve that. A timer does not, and
            // the question this asks is whether the index in place is current
            // - not whether a different one should be.
            var (game, reading) = _referenceIndexUpdateService.ResolveLanguages(string.Empty, string.Empty);

            var state = _referenceIndexUpdateService.ReadState();

            var latest = await _referenceIndexUpdateService
                .GetLatestRevisionAsync(cancellationToken)
                .ConfigureAwait(true);

            var outcome = ReferenceIndexAutoCheck.Decide(state, game, reading, latest);

            // In English, and with both revisions in it: whoever reads this
            // afterwards wants to know what was compared, not how it was
            // phrased in the interface language of the day.
            _logger.WriteLog("Reference index check: " + outcome +
                             " (installed " + Revision(state.Revision) + ", project " + Revision(latest) +
                             ", " + game + " → " + reading + ")");

            if (IsReferenceIndexAutoInstall && ReferenceIndexAutoCheck.MayInstall(outcome))
            {
                // Straight down the button's own path, so an update that
                // started by itself can still be watched and cancelled. It
                // asks the project a second time, which is the same few
                // kilobytes and keeps one place deciding what to fetch.
                BeginReferenceIndexUpdate(string.Empty);
                return;
            }

            if (ReferenceIndexAutoCheck.IsWorthSaying(outcome))
            {
                ReferenceIndexProgress =
                    ReferenceIndexTextMapper.Describe(outcome, state, game, reading, _localize);
            }
        }
        catch (OperationCanceledException)
        {
            // The application is closing. Nothing was being replaced.
        }
    }

    /// <summary>A commit as short as anybody quotes one, or a word saying there is none.</summary>
    private static string Revision(string revision)
    {
        return string.IsNullOrEmpty(revision) ? "none" : revision.Substring(0, Math.Min(7, revision.Length));
    }

    /// <summary>
    /// Takes the saved settings away and closes, so the next start reads what
    /// a fresh installation would.
    ///
    /// Closing is the point rather than a shortcut: chat windows, hotkeys and
    /// overlays are all built from the settings as the application starts, and
    /// putting them back in place while it runs is a great deal of surface for
    /// something to be left behind on.
    /// </summary>
    private void ResetSettings()
    {
        if (!_confirm(_localize("ResetSettingsQuestion")))
        {
            return;
        }

        ResetSettingsAsync().Forget();
    }

    private async Task ResetSettingsAsync()
    {
        await _settingsResetService.ResetAsync().ConfigureAwait(true);

        // Started before the shutdown, and told to wait for this process: only
        // one copy may run, so one started now would find the mutex taken and
        // exit without a word.
        _restart();

        _settingsViewModel.ShutDownRequestedCommand.Execute(null);
    }

    /// <summary>
    /// Puts a description of this session on the clipboard, and a copy of it on
    /// disk beside the log.
    ///
    /// The clipboard is the point: a report is going to be pasted into Discord,
    /// and asking somebody to find a file, open it, and copy its contents is
    /// three chances to end up with nothing. The file is there for when the
    /// paste has been and gone.
    /// </summary>
    private void CopyDiagnostics()
    {
        if (_diagnosticsReporter == null)
        {
            DiagnosticsStatus = _localize("DiagnosticsUnavailable");
            return;
        }

        try
        {
            var (report, savedTo) = _diagnosticsReporter.Collect();

            _logger.WriteLog("Diagnostics collected." + Environment.NewLine + report);

            var copied = TryCopyToClipboard(report);

            DiagnosticsStatus = copied
                ? _localize("DiagnosticsCopied")
                : _localize("DiagnosticsSavedOnly");

            if (!string.IsNullOrEmpty(savedTo))
            {
                DiagnosticsStatus += " " + savedTo;

                // Opened for them, because the archive is the half of this worth
                // sending and it lives under %APPDATA% - a path somebody has to
                // be talked through typing.
                RevealInExplorer(savedTo);
            }
        }
        catch (Exception ex)
        {
            _logger.WriteLog(ex);
            DiagnosticsStatus = _localize("DiagnosticsFailed");
        }
    }

    /// <summary>
    /// Shows the archive in Explorer with it already selected, so the next step
    /// is dragging it into a chat window.
    /// </summary>
    private void RevealInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                // Quoted, and no space after the comma: Explorer takes the rest
                // of the line as the path, so a space makes it open Documents
                // instead and say nothing about why.
                Arguments = "/select,\"" + path + "\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.WriteLog(ex);
        }
    }

    /// <summary>
    /// Another application can hold the clipboard open, and then setting it
    /// throws. Worth retrying once - and worth not losing the report over.
    /// </summary>
    private bool TryCopyToClipboard(string report)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                Clipboard.SetText(report);
                return true;
            }
            catch (Exception ex)
            {
                _logger.WriteLog(ex);
            }
        }

        return false;
    }

    private void CancelReferenceIndexUpdate()
    {
        // The archive is hundreds of megabytes; somebody who started this by
        // accident should not have to sit through it or kill the application.
        _referenceIndexUpdateCancellation?.Cancel();
    }

    private async Task RunReferenceIndexUpdateAsync(string readingLanguage, CancellationToken cancellationToken)
    {
        // Constructed here, on the interface thread, so its callbacks arrive
        // there too and the progress line can be set without marshalling.
        var progress = new Progress<ReferenceUpdateProgress>(
            report => ReferenceIndexProgress = ReferenceIndexTextMapper.Describe(report, _localize));

        try
        {
            ReferenceIndexProgress = _localize("ReferenceIndexChecking");

            // The language to key on comes from the game, not from here; the
            // caller only says what to read it in. Left empty, the service asks
            // the game and falls back to the index already in place.
            var result = await _referenceIndexUpdateService
                .UpdateAsync(
                    string.Empty,
                    readingLanguage,
                    progress,
                    cancellationToken)
                .ConfigureAwait(true);

            ReferenceIndexProgress = ReferenceIndexTextMapper.Describe(result, _localize);
        }
        catch (OperationCanceledException)
        {
            ReferenceIndexProgress = _localize("ReferenceIndexCancelled");
        }
        catch (Exception ex)
        {
            ReferenceIndexProgress = string.Format(CultureInfo.CurrentCulture,
                _localize("ReferenceIndexFailed"), ex.Message);
        }
        finally
        {
            _referenceIndexUpdateCancellation?.Dispose();
            _referenceIndexUpdateCancellation = null;
            OnPropertyChanged(nameof(IsReferenceIndexUpdating));
            OnPropertyChanged(nameof(CanUpdateReferenceIndex));

            // Whatever happened, the index the application is now reading is
            // the one to describe: a failed swap leaves the old one in place.
            RefreshReferenceIndexStatus();
        }
    }

    private void RefreshReferenceIndexStatus()
    {
        ReferenceIndexStatus =
            ReferenceIndexTextMapper.Describe(_referenceIndexUpdateService.ReadState(), _localize);
    }

    /// <summary>
    /// Works out again whether any window reads a language the game is not
    /// already in, and follows the selected window so a change of its reading
    /// language is answered as it is made rather than at the next restart.
    /// </summary>
    private void RefreshReferenceTranslationUse()
    {
        var current = CurrentChatWindow;
        if (!ReferenceEquals(_watchedChatWindow, current))
        {
            if (_watchedChatWindow != null)
            {
                _watchedChatWindow.PropertyChanged -= OnWatchedChatWindowPropertyChanged;
            }

            _watchedChatWindow = current;

            if (_watchedChatWindow != null)
            {
                _watchedChatWindow.PropertyChanged += OnWatchedChatWindowPropertyChanged;
            }
        }

        var reading = ReadingLanguages();
        var game = _referenceIndexUpdateService.GameLanguage;
        var usable = ReferenceTranslationUse.AnythingToLookUp(game, reading);

        if (_isReferenceTranslationUsable == usable)
        {
            return;
        }

        // A whole section appearing or disappearing, so it says what decided
        // that. Somebody whose translations are suddenly not on the page has
        // one line to look at instead of a guess.
        _logger.WriteLog("Hand-made translations " + (usable ? "shown" : "hidden") +
                         ": game '" + (game.Length > 0 ? game : "unknown") +
                         "', windows read '" + string.Join(", ", reading) + "'.");

        _isReferenceTranslationUsable = usable;
        OnPropertyChanged(nameof(IsReferenceTranslationUsable));
    }

    private List<string> ReadingLanguages()
    {
        return _settingsViewModel.ChatWindows
            .ToList()
            .Select(window => window?.CurrentTranslateToLanguage?.LanguageCode ?? string.Empty)
            .ToList();
    }

    private void OnWatchedChatWindowPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Raised by the window when the current item of its reading-language
        // list moves, which is what a change in the combo box amounts to.
        if (e.PropertyName == "TranslateToLanguages" || string.IsNullOrEmpty(e.PropertyName))
        {
            RefreshReferenceTranslationUse();
        }
    }

    private void ExecuteSwitchLanguage(object parameter)
    {
        _settingsViewModel.SwitchLanguageCommand.Execute(parameter);
    }

    private void SelectChatWindowByParameter(object parameter)
    {
        if (parameter == null)
        {
            return;
        }

        if (long.TryParse(parameter.ToString(), out var winId))
        {
            SelectedChatWindowId = winId;
        }
    }

    private void ToggleCurrentWindowVisibility()
    {
        if (CurrentChatWindow == null)
        {
            return;
        }

        CurrentChatWindow.ShowChatWindowCommand.Execute(null);
    }

    private void ResetCurrentWindowPosition()
    {
        if (CurrentChatWindow == null)
        {
            return;
        }

        CurrentChatWindow.RestChatWindowPositionCommand.Execute(null);
    }

    private LanguageOption ResolveLanguageOption(int languageId)
    {
        var language = (LanguageWrapper.Languages)languageId;
        var value = language switch
        {
            LanguageWrapper.Languages.Russian => "Russian",
            LanguageWrapper.Languages.Spanish => "Spanish",
            LanguageWrapper.Languages.Polish => "Polish",
            LanguageWrapper.Languages.Korean => "Korean",
            LanguageWrapper.Languages.PortugueseBR => "PortugueseBR",
            LanguageWrapper.Languages.Catalan => "Catalan",
            LanguageWrapper.Languages.Italian => "Italian",
            LanguageWrapper.Languages.Ukrainian => "Ukrainian",
            LanguageWrapper.Languages.Chinese => "Chinese",
            LanguageWrapper.Languages.ChineseTR => "ChineseTR",
            LanguageWrapper.Languages.Japanese => "Japanese",
            _ => "English"
        };

        return Languages.FirstOrDefault(x => x.Value == value) ?? Languages[0];
    }

    private void OnSettingsViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TataruViewModel.SelectedTabIndex) ||
            e.PropertyName == nameof(TataruViewModel.ChatWindows) ||
            e.PropertyName == nameof(TataruViewModel.CurrentChatWindow) ||
            string.IsNullOrEmpty(e.PropertyName))
        {
            OnPropertyChanged(nameof(CurrentChatWindow));
            OnPropertyChanged(nameof(SelectedChatWindowId));

            // A window may have been added, removed or selected, and each of
            // those changes the set of languages being read.
            RefreshReferenceTranslationUse();
        }
    }

    private void OnUiModelPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TataruUIModel.IsHideSettingsToTray))
        {
            OnPropertyChanged(nameof(IsHideSettingsToTray));
            return;
        }

        if (e.PropertyName == nameof(TataruUIModel.AreSettingsLoaded))
        {
            OnPropertyChanged(nameof(CanAddWindow));

            // The saved windows are in by now, so their languages can finally
            // be asked about.
            RefreshReferenceTranslationUse();
            return;
        }

        // The saved settings arrive on a background thread, and may well
        // arrive after this window has bound to them. Without this the switch
        // shows off to somebody who turned it on, and turning it off again
        // changes nothing, because it was already off in the model.
        if (e.PropertyName == nameof(TataruUIModel.IsReferenceIndexAutoInstall))
        {
            OnPropertyChanged(nameof(IsReferenceIndexAutoInstall));
            return;
        }

        if (e.PropertyName == nameof(TataruUIModel.IsPlayerNicknameTranslated))
        {
            OnPropertyChanged(nameof(IsPlayerNicknameTranslated));
            return;
        }

        if (e.PropertyName == nameof(TataruUIModel.UiLanguage))
        {
            _selectedLanguageOption = ResolveLanguageOption(_uiModel.UiLanguage);
            OnPropertyChanged(nameof(SelectedLanguageOption));
            RefreshSectionTitles();
            RefreshReferenceIndexStatus();
        }
    }

    private void RefreshSectionTitles()
    {
        foreach (var section in Sections)
        {
            var resourceValue = Application.Current?.Resources?[section.ResourceKey] as string;
            if (!string.IsNullOrWhiteSpace(resourceValue))
            {
                section.RefreshTitle(resourceValue);
            }

            var groupValue = Application.Current?.Resources?[section.GroupResourceKey] as string;
            if (!string.IsNullOrWhiteSpace(groupValue))
            {
                section.RefreshGroupName(groupValue);
            }
        }

        RegroupSections();

        foreach (var option in ThemeOptions)
        {
            option.RefreshTitleFromResources();
        }
    }

    private void RegroupSections()
    {
        var snapshot = Sections.ToList();
        var previousSelection = _selectedSection;
        Sections.Clear();
        foreach (var item in snapshot)
        {
            Sections.Add(item);
        }

        if (previousSelection != null && Sections.Contains(previousSelection))
        {
            _selectedSection = previousSelection;
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _settingsViewModel.PropertyChanged -= OnSettingsViewModelPropertyChanged;
        _uiModel.PropertyChanged -= OnUiModelPropertyChanged;

        if (_watchedChatWindow != null)
        {
            _watchedChatWindow.PropertyChanged -= OnWatchedChatWindowPropertyChanged;
            _watchedChatWindow = null;
        }

        _referenceIndexCheckTimer.Stop();
        _referenceIndexCheckTimer.Tick -= OnReferenceIndexCheckDue;

        // An update outlives this window otherwise, and it ends by moving a
        // file over the index the application is still reading as it shuts down.
        _referenceIndexUpdateCancellation?.Cancel();
        _lifetime.Cancel();

        _disposed = true;
    }
}