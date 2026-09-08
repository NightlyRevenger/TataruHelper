using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.Services.Logging;
using FFXIVTataruHelper.Services.Settings;
using FFXIVTataruHelper.Services.UI;
using FFXIVTataruHelper.ViewModel;

namespace FFXIVTataruHelper.Factories
{
    public sealed class ChatWindowFactory : IChatWindowFactory
    {
        private readonly IAppLogger _logger;
        private readonly ISettingsStore _settingsStore;
        private readonly IUiDispatcher _uiDispatcher;
        private readonly DialogueOverlayHost _dialogueOverlayHost;
        private readonly GameIconReader _gameIcons;

        public ChatWindowFactory(IAppLogger logger, ISettingsStore settingsStore, IUiDispatcher uiDispatcher,
            DialogueOverlayHost dialogueOverlayHost, GameIconReader gameIcons)
        {
            _logger = logger;
            _settingsStore = settingsStore;
            _uiDispatcher = uiDispatcher;
            _dialogueOverlayHost = dialogueOverlayHost;
            _gameIcons = gameIcons;
        }

        public ChatWindow Create(TataruModel tataruModel, ChatWindowViewModel chatWindowViewModel, MainWindow mainWindow)
        {
            return new ChatWindow(tataruModel, chatWindowViewModel, mainWindow, _logger, _settingsStore, _uiDispatcher,
                _dialogueOverlayHost, _gameIcons);
        }
    }
}
