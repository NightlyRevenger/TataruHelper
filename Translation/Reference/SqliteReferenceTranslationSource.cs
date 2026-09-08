using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Microsoft.Data.Sqlite;

using Microsoft.Extensions.Logging;

namespace Translation.Reference
{
    /// <summary>
    /// Looks lines up in the index built by <see cref="ReferenceIndexUpdater"/>.
    ///
    /// Held open read-only for the life of the app: the file is around sixty
    /// megabytes and every NPC line asks it a question, so opening per lookup
    /// would cost more than the lookup.
    /// </summary>
    public sealed class SqliteReferenceTranslationSource : IReferenceTranslationSource, IDisposable
    {
        private readonly ILogger _logger;
        private readonly object _sync = new object();

        /// <summary>Stands in for the character's name inside a stored pattern.</summary>
        private const string PlayerPlaceholder = "\u0001";

        /// <summary>Stands in for an item the game names, inside a stored pattern.</summary>
        private const string ItemPlaceholder = "\u0002";

        /// <summary>
        /// A line with one item's name punched out, already split at the hole.
        ///
        /// Split once at load rather than per lookup: this is tried on every
        /// line that misses, and a cutscene misses often.
        /// </summary>
        internal readonly struct ItemPattern
        {
            public ItemPattern(string prefix, string suffix, string translated)
            {
                Prefix = prefix;
                Suffix = suffix;
                Translated = translated;
            }

            public string Prefix { get; }

            public string Suffix { get; }

            /// <summary>Still carries the hole, for whatever the line had in it.</summary>
            public string Translated { get; }

            /// <summary>How much of the line the pattern actually pins down.</summary>
            public int FixedLength => Prefix.Length + Suffix.Length;
        }

        private ItemPattern[] _itemPatterns = Array.Empty<ItemPattern>();

        private string _playerName = string.Empty;

        private Dictionary<string, string> _addressedToPlayer;

        private bool? _playerIsFeminine;

        private Dictionary<string, string> _genderedLines;

        /// <summary>
        /// Lines that name the character and agree with them at once. Needs
        /// both facts, so it waits for whichever arrives second.
        /// </summary>
        private Dictionary<string, string> _addressedAndGendered;

        private SqliteCommand _speakerLookup;

        private SqliteParameter _speakerParameter;

        private SqliteConnection _connection;
        private SqliteCommand _lookup;
        private SqliteParameter _sentenceParameter;

        public SqliteReferenceTranslationSource(string databasePath, ILogger logger)
        {
            _logger = logger;
            LanguageCode = string.Empty;
            SourceLanguageCode = string.Empty;
            Revision = string.Empty;

            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return;
            }

            var resolvedPath = Resolve(databasePath);
            DatabasePath = resolvedPath;

            if (!File.Exists(resolvedPath))
            {
                _logger?.LogInformation(
                    "Reference translations are not installed at {Path}; every line will be translated.",
                    resolvedPath);
                return;
            }

            try
            {
                Open(resolvedPath);
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", Convert.ToString(ex));
                Dispose();
            }
        }

        public string LanguageCode { get; private set; }

        public string SourceLanguageCode { get; private set; }

        public string Revision { get; private set; }

        public int LineCount { get; private set; }

        public int RulesVersion { get; private set; }

        /// <summary>
        /// Where the index was looked for, whether or not one was found. The
        /// path is settled here rather than by the caller, and rebuilding the
        /// index has to write to the same file this reads.
        /// </summary>
        public string DatabasePath { get; private set; } = string.Empty;

        public bool IsAvailable => _lookup != null;

        /// <summary>
        /// A path from settings, made absolute.
        ///
        /// Environment variables are expanded first, so the settings can name
        /// the user's own folder - which is where the index lives - without
        /// this project having to know what that folder is called.
        /// </summary>
        public static string Resolve(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return string.Empty;
            }

            var expanded = Environment.ExpandEnvironmentVariables(databasePath);

            return Path.IsPathRooted(expanded)
                ? Path.GetFullPath(expanded)
                : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, expanded));
        }

        /// <summary>
        /// Setting this fills the name into every stored pattern at once.
        ///
        /// Matching them one at a time would mean testing a line against three
        /// thousand patterns; the name does not change while the game runs, so
        /// filling it in turns them into ordinary lines that are found the same
        /// way as the rest.
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set
            {
                var name = (value ?? string.Empty).Trim();
                lock (_sync)
                {
                    if (string.Equals(_playerName, name, StringComparison.Ordinal))
                    {
                        return;
                    }

                    _playerName = name;
                    _addressedToPlayer = name.Length > 0 ? BuildAddressedLines(name) : null;
                    _addressedAndGendered = BuildAddressedGenderedLines();
                }
            }
        }

        /// <summary>
        /// Setting this picks the wording the Russian agrees with, for the five
        /// thousand lines that have one. English usually needs no such choice -
        /// "adventurer" has no gender - so these are lines the index would
        /// otherwise have had to leave to an engine.
        /// </summary>
        public bool? PlayerIsFeminine
        {
            get => _playerIsFeminine;
            set
            {
                lock (_sync)
                {
                    if (_playerIsFeminine == value)
                    {
                        return;
                    }

                    _playerIsFeminine = value;
                    _genderedLines = value.HasValue ? BuildGenderedLines(value.Value) : null;
                    _addressedAndGendered = BuildAddressedGenderedLines();
                }
            }
        }

        public bool TryGetSpeakerName(string speaker, out string translated)
        {
            translated = string.Empty;

            var key = FoldApostrophes(Normalize(speaker));
            if (key.Length == 0 || _connection == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_speakerLookup == null)
                {
                    return false;
                }

                try
                {
                    _speakerParameter.Value = key;
                    var found = _speakerLookup.ExecuteScalar() as string;
                    if (string.IsNullOrEmpty(found))
                    {
                        return false;
                    }

                    translated = found;
                    return true;
                }
                catch (Exception ex)
                {
                    _logger?.LogInformation("{Message}", Convert.ToString(ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// The game writes a typographic apostrophe in names - Y’shtola - and a
        /// plain one elsewhere for the same character, so the two have to look
        /// alike before anything is compared.
        /// </summary>
        internal static string FoldApostrophes(string text)
        {
            return text
                .Replace('’', '\'')
                .Replace('ʼ', '\'')
                .Replace('‘', '\'');
        }

        public bool TryGetTranslation(string sentence, out string translation)
        {
            translation = string.Empty;

            var key = Normalize(sentence);
            if (key.Length == 0 || _lookup == null)
            {
                return false;
            }

            lock (_sync)
            {
                if (_lookup == null)
                {
                    return false;
                }

                // The most particular first: a line that names this character
                // and is worded for them.
                if (_addressedAndGendered != null &&
                    _addressedAndGendered.TryGetValue(key, out var both) &&
                    !CarriesUnresolvedMarkup(both))
                {
                    translation = both;
                    return true;
                }

                if (_addressedToPlayer != null &&
                    _addressedToPlayer.TryGetValue(key, out var addressed) &&
                    !CarriesUnresolvedMarkup(addressed))
                {
                    translation = addressed;
                    return true;
                }

                if (_genderedLines != null &&
                    _genderedLines.TryGetValue(key, out var gendered) &&
                    !CarriesUnresolvedMarkup(gendered))
                {
                    translation = gendered;
                    return true;
                }

                try
                {
                    _sentenceParameter.Value = key;
                    var found = _lookup.ExecuteScalar() as string;
                    if (!string.IsNullOrEmpty(found) && !CarriesUnresolvedMarkup(found))
                    {
                        translation = found;
                        return true;
                    }

                    // Last, and only when nothing was written for this line as
                    // it stands: a line that differs from a stored one only in
                    // the item it names. An exact line is always the better
                    // answer, so this never gets to override one.
                    if (TryMatchItemPattern(_itemPatterns, key, out var named) &&
                        !CarriesUnresolvedMarkup(named))
                    {
                        translation = named;
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger?.LogInformation("{Message}", Convert.ToString(ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// Whether a stored line still carries markup the game would have
        /// resolved as it drew - gender agreement, the player's name.
        ///
        /// The builder leaves these out, and a stricter builder is the real
        /// answer. This is the net under it: one such line reached the chat
        /// window reading "когда ты с ним &lt;var 08 E905 ((схлестнулась))
        /// ((схлестнулся)) /var&gt;", and showing that is worse than paying a
        /// translator for the line.
        ///
        /// Only &lt;var&gt; counts. Sound cues - &lt;sigh&gt;, &lt;click&gt;,
        /// &lt;gasp&gt; - look like markup and are not: the game draws them as
        /// the text they appear to be, so they belong in the line.
        /// </summary>
        private static bool CarriesUnresolvedMarkup(string translation)
        {
            var opening = translation.IndexOf("<var ", StringComparison.Ordinal);
            return opening >= 0 && translation.IndexOf('>', opening) > opening;
        }

        /// <summary>
        /// Reduces a line to the shape the index was built in: the game wraps
        /// dialogue across lines and we read it back joined, so runs of
        /// whitespace cannot be part of the key.
        /// </summary>
        /// <summary>
        /// Finds a line that differs from a stored one only in the item it
        /// names, and writes what this line had into the translation.
        ///
        /// The most particular match wins: two patterns can both fit, and the
        /// one that pins down more of the line is the one that meant it. A hole
        /// has to swallow something - a line identical to the fixed parts with
        /// nothing between them is a different line, not this one.
        /// </summary>
        internal static bool TryMatchItemPattern(
            IReadOnlyList<ItemPattern> patterns, string key, out string translation)
        {
            translation = string.Empty;

            if (patterns == null || key == null)
            {
                return false;
            }

            var pinned = -1;

            for (var i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];

                if (pattern.FixedLength >= key.Length || pattern.FixedLength <= pinned)
                {
                    continue;
                }

                if (!key.StartsWith(pattern.Prefix, StringComparison.Ordinal) ||
                    !key.EndsWith(pattern.Suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                var item = key.Substring(pattern.Prefix.Length, key.Length - pattern.FixedLength);

                if (!LooksLikeAnItemName(item))
                {
                    continue;
                }

                // The hole must not outweigh the sentence around it. A pattern
                // is worth using when it translates the line and writes a name
                // into it; when the name is longer than everything the pattern
                // knows how to say, it is barely translating at all - "Defeat
                // ⟨…⟩" carries one word and hands the rest back in English,
                // and it caught seven hundred story lines doing so. Refusing
                // those costs nothing: the line goes to an engine instead,
                // which is where it would have gone anyway.
                if (item.Length > pattern.FixedLength)
                {
                    continue;
                }

                pinned = pattern.FixedLength;
                translation = pattern.Translated.Replace(ItemPlaceholder, item);
            }

            return pinned >= 0;
        }

        /// <summary>
        /// Whether what fell into the hole could be the name of a thing.
        ///
        /// The hole was allowed to swallow anything at all, and a pattern as
        /// short as "Welcome to ⟨item⟩." will happily swallow a paragraph: a
        /// guildmaster's whole speech about the Fishermen's Guild came out as
        /// "Добро пожаловать в " followed by the untouched English, because it
        /// began with those words and ended with a full stop.
        ///
        /// A name is short, is not many words, and carries no punctuation
        /// that joins or ends a sentence. Each test catches what the others
        /// miss: length alone lets a short sentence through, the word count
        /// alone lets "the Endeavor, pride and joy of the guild" through at
        /// exactly eight, and the punctuation alone lets a long plain clause
        /// through.
        ///
        /// Checking the hole against the game's own list of item names was
        /// tried on paper and does not work: the markup carries the number and
        /// the article, so what is drawn is "Allagan tomestones of poetics"
        /// while the list holds "Allagan tomestone of poetics". The very line
        /// this feature was built for would fail such a check.
        /// </summary>
        internal static bool LooksLikeAnItemName(string item)
        {
            // Measured against the game's own names: the longest run past
            // forty characters, and "Extreme Survival Kit of the Namazu" is
            // thirty-four, so sixty leaves room without leaving a sentence in.
            const int longestName = 60;

            // The longest names run to seven words - "Extreme Survival Kit of
            // the Namazu" is six - so eight leaves room and still refuses a
            // clause.
            const int mostWords = 8;

            if (item.Length == 0 || item.Length > longestName)
            {
                return false;
            }

            var words = 1;

            for (var i = 0; i < item.Length; i++)
            {
                var c = item[i];

                // A comma joins clauses. Names do not have any to join:
                // "the Endeavor, pride and joy of the guild" is eight words
                // and fits in sixty characters, and only the comma gives it
                // away as a sentence rather than a thing.
                if (c == '!' || c == '?' || c == ';' || c == ',' ||
                    c == (char)10 || c == (char)13)
                {
                    return false;
                }

                // A full stop with a space after it ends a sentence. A name
                // may carry one - "Mk. II" - but never one that closes.
                if (c == '.' && i + 1 < item.Length && item[i + 1] == ' ')
                {
                    return false;
                }

                if (c == ' ')
                {
                    words++;

                    if (words > mostWords)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Reads the item patterns and splits each at its hole.
        ///
        /// An index built before these existed has no such table, and that has
        /// to cost only these lines rather than the whole index.
        /// </summary>
        private ItemPattern[] LoadItemPatterns()
        {
            if (_connection == null)
            {
                return Array.Empty<ItemPattern>();
            }

            var patterns = new List<ItemPattern>();

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT source, translated FROM item_pattern";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var source = Normalize(reader.GetString(0));
                            var translated = reader.GetString(1);

                            var hole = source.IndexOf(ItemPlaceholder, StringComparison.Ordinal);
                            if (hole < 0 || !translated.Contains(ItemPlaceholder))
                            {
                                continue;
                            }

                            patterns.Add(new ItemPattern(
                                source.Substring(0, hole),
                                source.Substring(hole + ItemPlaceholder.Length),
                                translated));
                        }
                    }
                }

                _logger?.LogInformation("Lines naming an item: {Count}.", patterns.Count);
            }
            catch (SqliteException)
            {
                return Array.Empty<ItemPattern>();
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", Convert.ToString(ex));
                return Array.Empty<ItemPattern>();
            }

            return patterns.ToArray();
        }

        internal static string Normalize(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(sentence.Length);
            var pendingSpace = false;

            foreach (var character in sentence)
            {
                // The game's own icons, carried through the application as
                // characters out of the private-use area. Nothing written by
                // hand has them, so a line matches whether it carries them or
                // not - which is how it was before they were carried at all.
                if (character >= '' && character <= '')
                {
                    continue;
                }

                if (char.IsWhiteSpace(character))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(character);
            }

            return builder.ToString();
        }

        /// <summary>
        /// The ways the game may write a character called "D'ark One": in full,
        /// or by either half of the name.
        ///
        /// Which one appears is the line's own choice, and guessing wrong costs
        /// the match: every line addressed to the player went to a translator
        /// while only the full name was tried, because what the game had
        /// written was "Go swiftly, D'ark."
        /// </summary>
        /// <summary>
        /// The name as somebody speaking to the character would use it.
        ///
        /// The forename, on the evidence of what the game draws: a line stored
        /// as "Go swiftly, &lt;name&gt;." reaches the screen as "Go swiftly,
        /// D'ark." for a character called D'ark One.
        /// </summary>
        private static string AddressForm(string playerName)
        {
            var separator = playerName.IndexOf(' ');
            return separator > 0 ? playerName.Substring(0, separator) : playerName;
        }

        private static IEnumerable<string> NameForms(string playerName)
        {
            yield return playerName;

            var separator = playerName.IndexOf(' ');
            if (separator <= 0)
            {
                yield break;
            }

            yield return playerName.Substring(0, separator);

            var surname = playerName.Substring(separator + 1).Trim();
            if (surname.Length > 0)
            {
                yield return surname;
            }
        }

        /// <summary>
        /// Reads the lines the Russian phrases differently for a man and a
        /// woman, keeping the wording that fits this character.
        /// </summary>
        private Dictionary<string, string> BuildGenderedLines(bool isFeminine)
        {
            var lines = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_connection == null)
            {
                return lines;
            }

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    // Both the line and its translation are kept per gender:
                    // English says "this woman" against "this man" as readily
                    // as Russian declines around it, so even the key differs.
                    command.CommandText = "SELECT source, translated FROM gendered WHERE feminine = $feminine";
                    var feminine = command.CreateParameter();
                    feminine.ParameterName = "$feminine";
                    feminine.Value = isFeminine ? 1 : 0;
                    command.Parameters.Add(feminine);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var source = Normalize(reader.GetString(0));
                            if (source.Length > 0)
                            {
                                lines[source] = reader.GetString(1);
                            }
                        }
                    }
                }

                _logger?.LogInformation("Lines phrased for a {Gender} character: {Count}.",
                    isFeminine ? "female" : "male", lines.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", Convert.ToString(ex));
            }

            return lines;
        }

        /// <summary>
        /// Reads the lines that both name the character and agree with them,
        /// keeping this character's gender and writing this character's name in.
        ///
        /// Needs both facts, and they arrive separately - the name from the
        /// character, the gender from the same read - so this runs again
        /// whenever either changes and does nothing until both are known.
        /// </summary>
        private Dictionary<string, string> BuildAddressedGenderedLines()
        {
            if (_connection == null || _playerName.Length == 0 || !_playerIsFeminine.HasValue)
            {
                return null;
            }

            var lines = new Dictionary<string, string>(StringComparer.Ordinal);

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText =
                        "SELECT source, translated FROM gendered_pattern WHERE feminine = $feminine";
                    var feminine = command.CreateParameter();
                    feminine.ParameterName = "$feminine";
                    feminine.Value = _playerIsFeminine.Value ? 1 : 0;
                    command.Parameters.Add(feminine);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FillName(lines, reader.GetString(0), reader.GetString(1), _playerName);
                        }
                    }
                }

                _logger?.LogInformation("Lines both naming and agreeing with {Player}: {Count}.",
                    _playerName, lines.Count);
            }
            catch (SqliteException)
            {
                // An index built before these were collected has no such table,
                // and that has to cost only these lines.
                _logger?.LogInformation("This index carries no lines that both name and agree.");
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", Convert.ToString(ex));
            }

            return lines;
        }

        /// <summary>
        /// Writes a character's name into one stored pattern, under every form
        /// of the name the line might use.
        /// </summary>
        private static void FillName(
            Dictionary<string, string> lines, string sourcePattern, string translatedPattern, string playerName)
        {
            if (sourcePattern.IndexOf(PlayerPlaceholder, StringComparison.Ordinal) < 0)
            {
                // The line does not name the character but its translation
                // does - German rarely addresses the player where the Russian
                // for the same row does. Nothing on screen says which form to
                // use, so use the one the game uses to address somebody;
                // trying each form here would only give the same key three
                // different endings.
                var fixedSource = Normalize(sourcePattern);
                if (fixedSource.Length > 0)
                {
                    lines[fixedSource] = translatedPattern.Replace(PlayerPlaceholder, AddressForm(playerName));
                }

                return;
            }

            foreach (var form in NameForms(playerName))
            {
                var source = Normalize(sourcePattern.Replace(PlayerPlaceholder, form));
                if (source.Length > 0)
                {
                    // Stored under each form the line might use, but the
                    // translation reads the way the line did.
                    lines[source] = translatedPattern.Replace(PlayerPlaceholder, form);
                }
            }
        }

        /// <summary>
        /// Reads the patterns and writes the name into each, giving the lines
        /// as this particular character hears them.
        /// </summary>
        private Dictionary<string, string> BuildAddressedLines(string playerName)
        {
            var lines = new Dictionary<string, string>(StringComparer.Ordinal);
            if (_connection == null)
            {
                return lines;
            }

            try
            {
                using (var command = _connection.CreateCommand())
                {
                    command.CommandText = "SELECT source, translated FROM pattern";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            FillName(lines, reader.GetString(0), reader.GetString(1), playerName);
                        }
                    }
                }

                _logger?.LogInformation("Lines addressed to {Player}: {Count}.", playerName, lines.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogInformation("{Message}", Convert.ToString(ex));
            }

            return lines;
        }

        private void Open(string resolvedPath)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = resolvedPath,
                Mode = SqliteOpenMode.ReadOnly,
                Cache = SqliteCacheMode.Shared,

                // A pooled connection keeps the file open after it is closed,
                // and rebuilding the index has to move a new file over this
                // one. Nothing is gained by pooling here anyway: this is one
                // connection held open for as long as the application runs.
                Pooling = false
            }.ToString();

            _connection = new SqliteConnection(connectionString);
            _connection.Open();

            LanguageCode = ReadMeta("language");

            // Every index built before the game's language could be anything
            // else was built from the English column, and says nothing.
            var source = ReadMeta("sourceLanguage");
            SourceLanguageCode = source.Length > 0 ? source : "en";

            // Written only by an index this application built. One built from a
            // folder cannot say which commit it holds, and then every update
            // downloads rather than trusting a revision nobody recorded.
            Revision = ReadMeta("revision");
            LineCount = int.TryParse(ReadMeta("lines"), out var lines) ? lines : 0;
            RulesVersion = int.TryParse(ReadMeta("rules"), out var rules) ? rules : 0;

            _lookup = _connection.CreateCommand();
            _lookup.CommandText = "SELECT translated FROM line WHERE source = $sentence";
            _sentenceParameter = _lookup.CreateParameter();
            _sentenceParameter.ParameterName = "$sentence";
            _lookup.Parameters.Add(_sentenceParameter);
            _lookup.Prepare();

            _itemPatterns = LoadItemPatterns();


            // An index built before names were collected has no such table, and
            // that has to cost only the names rather than the whole index.
            try
            {
                _speakerLookup = _connection.CreateCommand();
                // Case-insensitively: the game draws an NPC's name with every
                // word capitalised, while the sheet it comes from writes it as
                // it would sit in a sentence. "Sahjattra Concern representative"
                // is stored, "Sahjattra Concern Representative" is on screen,
                // and 1 847 of the 4 250 names differ exactly that way.
                _speakerLookup.CommandText =
                    "SELECT translated FROM speaker WHERE source = $speaker COLLATE NOCASE";
                _speakerParameter = _speakerLookup.CreateParameter();
                _speakerParameter.ParameterName = "$speaker";
                _speakerLookup.Parameters.Add(_speakerParameter);
                _speakerLookup.Prepare();
            }
            catch (SqliteException)
            {
                _speakerLookup?.Dispose();
                _speakerLookup = null;
                _logger?.LogInformation("This index carries no speaker names.");
            }

            // The path is part of the message on purpose: which file answered a
            // line is otherwise a guess, and guessing has been expensive here.
            _logger?.LogInformation(
                "Reference translations loaded: {Lines} lines, {Source} to {Language}, from {Path}.",
                LineCount, SourceLanguageCode, LanguageCode, resolvedPath);
        }

        private string ReadMeta(string key)
        {
            try
            {
                using var meta = _connection.CreateCommand();
                meta.CommandText = "SELECT value FROM meta WHERE key = $key";
                var parameter = meta.CreateParameter();
                parameter.ParameterName = "$key";
                parameter.Value = key;
                meta.Parameters.Add(parameter);
                return meta.ExecuteScalar() as string ?? string.Empty;
            }
            catch (SqliteException)
            {
                return string.Empty;
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                _speakerLookup?.Dispose();
                _speakerLookup = null;
                _lookup?.Dispose();
                _lookup = null;
                _connection?.Dispose();
                _connection = null;
            }
        }
    }
}
