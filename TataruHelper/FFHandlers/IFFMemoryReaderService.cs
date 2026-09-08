using System;
using System.Threading.Tasks;
using System.Windows;

using FFXIVTataruHelper.EventArguments;
using FFXIVTataruHelper.Services.GameMemory;
using FFXIVTataruHelper.TataruComponentModel;

namespace FFXIVTataruHelper.FFHandlers
{
    public interface IFFMemoryReaderService : IDisposable, INotifyPropertyChangedAsync
    {
        event AsyncEventHandler<WindowStateChangeEventArgs> FFWindowStateChanged;

        event AsyncEventHandler<ChatMessageArrivedEventArgs> FFChatMessageArrived;

        WindowState FFWindowState { get; }

        bool IsGameWindowForeground { get; }

        /// <summary>
        /// Where the game is drawing the line it is showing, or unknown when it
        /// is showing none - so a translation can be put over that line rather
        /// than beside it.
        /// </summary>
        Services.GameMemory.AddonBounds DialogueBounds { get; }

        /// <summary>Whether that line is a cutscene subtitle, drawn without a window.</summary>
        DialogueSurface DialogueSurface { get; }

        GameChoice CurrentChoice { get; }

        AddonBounds ChoiceBounds { get; }

        /// <summary>
        /// The line the game is drawing in its dialogue window right now, in the
        /// form it reaches the translation pipeline - or empty when it is
        /// drawing none, which is how a copy of a translated line knows the
        /// game has moved on to another while its own is still on its way.
        /// </summary>
        string CurrentDialogueLine { get; }

        string CurrentDialogueSpeaker { get; }

        /// <summary>
        /// Where the game being read is installed, by its executable, or empty
        /// when nothing is attached. Wanted so the game's own art can be read
        /// out of its folder rather than shipped alongside this application.
        /// </summary>
        string GameExecutablePath { get; }

        /// <summary>The game's own window, for putting something over it.</summary>
        IntPtr GameWindowHandle { get; }

        /// <summary>
        /// Whether the game process is attached right now.
        ///
        /// FFWindowStateChanged only fires on transitions, and the reader starts
        /// before the settings window exists, so a UI created afterwards has no way
        /// to learn the state from the event alone.
        /// </summary>
        bool IsGameRunning { get; }

        /// <summary>Process name and PID of the attached game, empty when detached.</summary>
        string GameProcessDescription { get; }

        /// <summary>
        /// The game side of a bug report: what was attached, what was read, and
        /// under which dialogue codes.
        /// </summary>
        GameReadingDiagnostics Reading { get; }

        /// <summary>
        /// Reads dialogue from the game's UI as it appears rather than waiting for
        /// the chat log. Off falls back to chat-log-only behaviour.
        /// </summary>
        bool IsRealtimeTranslationEnabled { get; set; }

        /// <summary>Called once, when the character's name becomes readable.</summary>
        Action<string, bool?> PlayerNameResolved { get; set; }

        /// <summary>
        /// Told the language the game is set to, each time one is attached to.
        /// The game may be restarted in another language while this keeps
        /// running, and what was read at startup would then be wrong.
        /// </summary>
        Action<string> GameLanguageResolved { get; set; }

        void Start();

        void Stop();

        Task StopAsync(TimeSpan timeout);

        void AddExclusionWindowHandler(IntPtr handler);
    }
}