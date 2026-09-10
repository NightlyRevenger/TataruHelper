using System;
using System.Collections.Generic;
using System.IO;

using FFXIVTataruHelper.Services.Logging;

using Lumina;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// The names of the game's worlds, out of the player's own installation.
    ///
    /// Nothing is shipped and no list is kept here: the game gains worlds, and
    /// a list would be wrong the week it did. The same two-file trick as the
    /// icons - Lumina unpacks the archive, the parsing is this application's -
    /// and the same manners: no game folder, a file that has moved, a sheet in
    /// a shape this does not know, and there are simply no worlds, which is
    /// where this application was before it knew any.
    /// </summary>
    public sealed class GameWorldReader
    {
        private const string SheetHeaderPath = "exd/world.exh";

        private const string SheetRowsPath = "exd/world_0.exd";

        private readonly IAppLogger _logger;

        private readonly object _gate = new object();

        private HashSet<string> _worlds = new HashSet<string>(StringComparer.Ordinal);

        private string _openedFrom = string.Empty;

        private bool _tried;

        public GameWorldReader(IAppLogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Every public world the game knows, or none when it could not be
        /// asked.
        /// </summary>
        public IReadOnlyCollection<string> Worlds
        {
            get
            {
                lock (_gate)
                {
                    Open();
                    return _worlds;
                }
            }
        }

        /// <summary>
        /// Points this at the game the reader is attached to, given the path of
        /// its executable. Doing nothing when it is the same game as last time,
        /// so this may be called as often as is convenient.
        /// </summary>
        public void Follow(string gameExecutablePath)
        {
            var sqpack = SqpackBeside(gameExecutablePath);

            lock (_gate)
            {
                if (sqpack.Length == 0 || string.Equals(sqpack, _openedFrom, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _openedFrom = sqpack;
                _tried = false;
                _worlds = new HashSet<string>(StringComparer.Ordinal);
            }
        }

        private static string SqpackBeside(string gameExecutablePath)
        {
            if (string.IsNullOrEmpty(gameExecutablePath))
            {
                return string.Empty;
            }

            try
            {
                var folder = Path.GetDirectoryName(gameExecutablePath);
                if (string.IsNullOrEmpty(folder))
                {
                    return string.Empty;
                }

                var sqpack = Path.Combine(folder, "sqpack");
                return Directory.Exists(sqpack) ? sqpack : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void Open()
        {
            if (_tried || _openedFrom.Length == 0)
            {
                return;
            }

            _tried = true;

            try
            {
                var game = new GameData(_openedFrom);
                _worlds = GameWorldSheet.Read(
                    game.GetFile(SheetHeaderPath)?.Data,
                    game.GetFile(SheetRowsPath)?.Data);

                _logger?.WriteLog(FormattableString.Invariant(
                    $"The game names {_worlds.Count} worlds, read from {_openedFrom}."));
            }
            catch (Exception ex)
            {
                // Not worth stopping over: without them a name from another
                // world reads as it did before, with the world stuck to it.
                _logger?.WriteLog("The game's worlds could not be read from " + _openedFrom + ".");
                _logger?.WriteLog(ex);
                _worlds = new HashSet<string>(StringComparer.Ordinal);
            }
        }
    }
}
