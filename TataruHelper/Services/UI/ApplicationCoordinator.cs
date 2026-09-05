using System;
using System.Threading;
using System.Threading.Tasks;

using FFXIVTataruHelper.FFHandlers;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.ViewModel;

using Translation;

namespace FFXIVTataruHelper.Services.UI
{
    public sealed class ApplicationCoordinator : IApplicationCoordinator
    {
        private static readonly TimeSpan SettingsShutdownTimeout = TimeSpan.FromSeconds(5);

        private readonly IFFMemoryReaderService _ffMemoryReader;
        private readonly ITranslationPipelineCoordinator _translationPipelineCoordinator;
        private readonly IChatWindowsEventCoordinator _chatWindowsEventCoordinator;
        private readonly ISettingsMigrationService _settingsMigrationService;
        private readonly ISettingsSyncService _settingsSyncService;
        private readonly IAppLogger _logger;

        public ApplicationCoordinator(
            IFFMemoryReaderService ffMemoryReader,
            ITranslationPipelineCoordinator translationPipelineCoordinator,
            IChatWindowsEventCoordinator chatWindowsEventCoordinator,
            ISettingsMigrationService settingsMigrationService,
            ISettingsSyncService settingsSyncService,
            IAppLogger logger)
        {
            _ffMemoryReader = ffMemoryReader;
            _translationPipelineCoordinator = translationPipelineCoordinator;
            _chatWindowsEventCoordinator = chatWindowsEventCoordinator;
            _settingsMigrationService = settingsMigrationService;
            _settingsSyncService = settingsSyncService;
            _logger = logger;
        }

        public async Task InitializeAsync(TataruModel tataruModel, MainWindow mainWindow, TataruUIModel uiModel,
            TataruViewModel viewModel)
        {
            _ffMemoryReader.IsRealtimeTranslationEnabled = uiModel.IsRealtimeTranslation;
            tataruModel.WebTranslator.UseReferenceTranslations = uiModel.IsLiteraryTranslation;

            // Read from the game rather than taken from a setting: the index is
            // keyed on the language the client draws its dialogue in, and this
            // application has no say in that.
            tataruModel.WebTranslator.GameLanguage = GameClientLanguage.Detect(_logger);
            _ffMemoryReader.GameLanguageResolved = language =>
            {
                if (!string.IsNullOrEmpty(language))
                {
                    tataruModel.WebTranslator.GameLanguage = language;
                }
            };
            tataruModel.ChatProcessor.MarkMachineTranslation = uiModel.IsMachineTranslationMarked;
            tataruModel.ChatProcessor.TranslateSpeakerNames = uiModel.IsSpeakerNameTranslated;
            tataruModel.ChatProcessor.TranslatePlayerNicknames = uiModel.IsPlayerNicknameTranslated;
            _ffMemoryReader.PlayerNameResolved = (name, isFeminine) =>
            {
                tataruModel.WebTranslator.PlayerName = name;
                tataruModel.WebTranslator.PlayerIsFeminine = isFeminine;
            };
            uiModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(TataruUIModel.IsRealtimeTranslation))
                {
                    _ffMemoryReader.IsRealtimeTranslationEnabled = uiModel.IsRealtimeTranslation;
                }
                else if (e.PropertyName == nameof(TataruUIModel.IsLiteraryTranslation))
                {
                    tataruModel.WebTranslator.UseReferenceTranslations = uiModel.IsLiteraryTranslation;
                }

                else if (e.PropertyName == nameof(TataruUIModel.IsMachineTranslationMarked))
                {
                    tataruModel.ChatProcessor.MarkMachineTranslation = uiModel.IsMachineTranslationMarked;
                }


                else if (e.PropertyName == nameof(TataruUIModel.IsSpeakerNameTranslated))
                {
                    tataruModel.ChatProcessor.TranslateSpeakerNames = uiModel.IsSpeakerNameTranslated;
                }
                else if (e.PropertyName == nameof(TataruUIModel.IsPlayerNicknameTranslated))
                {
                    tataruModel.ChatProcessor.TranslatePlayerNicknames = uiModel.IsPlayerNicknameTranslated;
                }
            };

            // LoadLanguages and FFMemoryReader.Start are independent; do them in parallel.
            var loadLanguagesTask = Task.Run(() => tataruModel.WebTranslator.LoadLanguages());
            var startReaderTask = Task.Run(() => _ffMemoryReader.Start());

            await Task.WhenAll(loadLanguagesTask, startReaderTask).ConfigureAwait(false);

            // Pipeline depends on both; chat windows must run on the UI thread, so marshal back.
            _translationPipelineCoordinator.Start(_ffMemoryReader, tataruModel.ChatProcessor);
            _chatWindowsEventCoordinator.Start(uiModel, viewModel, tataruModel, mainWindow);
        }

        // The synchronous Stop had no callers: the only shutdown path is StopAsync,
        // reached from the thread-pool cleanup task in Window_Closing.

        public async Task StopAsync(IChatWindowCoordinator chatWindowCoordinator)
        {
            StopBestEffort(_chatWindowsEventCoordinator.Stop, "chat windows events");
            StopBestEffort(chatWindowCoordinator.CloseAll, "chat windows");
            StopBestEffort(_translationPipelineCoordinator.Stop, "translation pipeline");

            try
            {
                await _ffMemoryReader.StopAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.WriteLog("ApplicationCoordinator.StopAsync ff memory reader failed.");
                _logger.WriteLog(ex);
            }

            try
            {
                using var cancellation = new CancellationTokenSource(SettingsShutdownTimeout);
                await _settingsSyncService.StopAsync(cancellation.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.WriteLog("ApplicationCoordinator.StopAsync settings sync timed out.");
            }
            catch (Exception ex)
            {
                _logger.WriteLog("ApplicationCoordinator.StopAsync settings sync failed.");
                _logger.WriteLog(ex);
            }
        }

        private void StopBestEffort(Action action, string componentName)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                _logger.WriteLog("ApplicationCoordinator.Stop canceled for " + componentName + ".");
            }
            catch (Exception ex)
            {
                _logger.WriteLog("ApplicationCoordinator.Stop failed for " + componentName + ".");
                _logger.WriteLog(ex);
            }
        }

        public void LoadSettings(TataruUIModel uiModel, string systemSettingFileName, ChatProcessor chatProcessor,
            WebTranslator webTranslator, Func<Task> persistSettingsAsync)
        {
            var userSettings = _settingsMigrationService.LoadUserSettings(systemSettingFileName,
                chatProcessor.AllChatCodes, webTranslator.TranslationEngines);
            uiModel.SetSettings(userSettings);
            _settingsSyncService.Start(uiModel, persistSettingsAsync);
        }
    }
}
