using Sharlayan.Core;
using Sharlayan.Models;
using Sharlayan.Models.ReadResults;

namespace FFXIVTataruHelper.Services.GameMemory
{
    public interface IGameMemoryGateway
    {
        void SetProcess(ProcessModel processModel, string gameLanguage, string patchVersion, bool useLocalCache, bool scanAllMemoryRegions);

        void UnsetProcess();

        ChatLogResult GetChatLog(int previousArrayIndex, int previousOffset);

        ChatLogResult GetDirectDialog();

        /// <summary>
        /// Where the game is drawing the line it is showing, or unknown when it
        /// is showing none. Taken on the sweep that reads the line itself, so
        /// asking costs nothing beyond what is already being read.
        /// </summary>
        AddonBounds DialogueBounds { get; }

        /// <summary>Whether that line is a cutscene subtitle, drawn without a window.</summary>
        DialogueSurface DialogueSurface { get; }

        GameChoice CurrentChoice { get; }

        AddonBounds ChoiceBounds { get; }

        /// <summary>
        /// The line the game is drawing in its dialogue window right now, in the
        /// "speaker:text" form it reaches the translation pipeline - or empty
        /// when it is drawing none, which is how a copy of a translated line
        /// knows the ground has moved under it while its translation is in
        /// flight.
        ///
        /// Taken on the sweep that reads the line itself, so asking costs
        /// nothing beyond what is already being read.
        /// </summary>
        string CurrentDialogueLine { get; }

        string CurrentDialogueSpeaker { get; }

        /// <summary>
        /// Whether dialogue under this code has been read off the screen at least
        /// once since attaching, and so whether the chat log's later copy of it
        /// would be a repeat rather than the only copy there is.
        /// </summary>
        bool HasReadCodeLive(string chatCode);

        /// <summary>
        /// What has been read off the screen since attaching, for reporting when
        /// somebody says translation is not working.
        /// </summary>
        LiveReadingStats LiveReading { get; }

        bool CheckChatEquality(ChatLogItem item1, ChatLogItem item2);



        /// <summary>


        /// The character's own name, empty until they are loaded. The game writes it


        /// into lines addressed to them, so a hand-made translation of such a line


        /// cannot be recognised without it.


        /// </summary>


        string GetPlayerName();




        /// <summary>The character's gender, which the Russian agrees with. Null until known.</summary>



        bool? GetPlayerIsFeminine();
    }
}
