using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using FFXIVTataruHelper.Services.Logging;

using Lumina;
using Lumina.Data.Files;

namespace FFXIVTataruHelper.Services.GameMemory
{
    /// <summary>
    /// The pictures behind the icons in a line of dialogue, read out of the
    /// player's own installation of the game.
    ///
    /// Nothing is shipped. The game's art belongs to the game, and the player
    /// already has it: the folder is known from the process being read, and the
    /// two files wanted are <c>common/font/gfdata.gfd</c>, which says where each
    /// icon sits, and <c>common/font/fonticon_xinput.tex</c>, the sheet they sit
    /// on. Lumina reads both, and it is already here as a dependency of
    /// Sharlayan.
    ///
    /// Everything is optional. A game folder that cannot be found, a file that
    /// has moved, an id the sheet does not have - each of them means an icon is
    /// not drawn, which is where this application was before it drew any.
    /// </summary>
    public sealed class GameIconReader
    {
        private const string IconTablePath = "common/font/gfdata.gfd";

        /// <summary>
        /// The sheet drawn on a keyboard-and-mouse client. The game keeps one
        /// per controller, differing only in the button glyphs; those are not
        /// what turns up in dialogue.
        /// </summary>
        private const string IconSheetPath = "common/font/fonticon_xinput.tex";

        private readonly IAppLogger _logger;

        private readonly object _gate = new object();

        private readonly Dictionary<int, ImageSource> _drawn = new Dictionary<int, ImageSource>();

        private Dictionary<int, GameIconSheet.Place> _places;

        private byte[] _sheet;

        private int _sheetWidth;

        private int _sheetHeight;

        private string _openedFrom = string.Empty;

        private bool _tried;

        public GameIconReader(IAppLogger logger)
        {
            _logger = logger;
        }

        /// <summary>Whether an icon can be drawn at all.</summary>
        public bool IsOpen
        {
            get
            {
                lock (_gate)
                {
                    return _sheet != null && _places != null && _places.Count > 0;
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
                _places = null;
                _sheet = null;
                _drawn.Clear();
            }
        }

        /// <summary>
        /// The picture for an icon, or nothing when there is none to be had.
        /// Drawn once and kept: a line of dialogue asks for the same three
        /// crowns twenty times a second for as long as it is on screen.
        /// </summary>
        public ImageSource Icon(int iconId)
        {
            if (iconId <= 0)
            {
                return null;
            }

            lock (_gate)
            {
                if (_drawn.TryGetValue(iconId, out var already))
                {
                    return already;
                }

                Open();

                var drawn = Cut(iconId);
                _drawn[iconId] = drawn;
                return drawn;
            }
        }

        /// <summary>
        /// The game keeps its data one folder up from the executable, in
        /// <c>game/sqpack</c> - the executable itself lives in <c>game</c>.
        /// </summary>
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

                var table = game.GetFile(IconTablePath);
                _places = GameIconSheet.Read(table?.Data);

                var sheet = game.GetFile<TexFile>(IconSheetPath);
                if (sheet != null)
                {
                    _sheet = sheet.ImageData;
                    _sheetWidth = sheet.Header.Width;
                    _sheetHeight = sheet.Header.Height;
                }

                _logger?.WriteLog(FormattableString.Invariant(
                    $"The game's icons: {_places.Count} of them, on a {_sheetWidth}x{_sheetHeight} sheet, from {_openedFrom}."));
            }
            catch (Exception ex)
            {
                // Not worth stopping over: without the sheet a line simply
                // reads without its pictures, which is how it read before.
                _logger?.WriteLog("The game's icons could not be read from " + _openedFrom + ".");
                _logger?.WriteLog(ex);
                _places = null;
                _sheet = null;
            }
        }

        /// <summary>
        /// One icon off the sheet. The sheet is straight BGRA, a row at a time,
        /// which is what a WPF bitmap wants too - so this is a copy of a
        /// rectangle and nothing more.
        /// </summary>
        private ImageSource Cut(int iconId)
        {
            if (_sheet == null || _places == null || !_places.TryGetValue(iconId, out var place))
            {
                return null;
            }

            if (place.Left + place.Width > _sheetWidth || place.Top + place.Height > _sheetHeight)
            {
                return null;
            }

            const int bytesPerPixel = 4;
            var stride = place.Width * bytesPerPixel;
            var pixels = new byte[stride * place.Height];

            for (var row = 0; row < place.Height; row++)
            {
                var from = ((place.Top + row) * _sheetWidth + place.Left) * bytesPerPixel;
                if (from + stride > _sheet.Length)
                {
                    return null;
                }

                Buffer.BlockCopy(_sheet, from, pixels, row * stride, stride);
            }

            var bitmap = BitmapSource.Create(
                place.Width, place.Height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);

            // Frozen so the reader may hand the same picture to the drawing
            // thread as often as it likes.
            bitmap.Freeze();
            return bitmap;
        }
    }
}
