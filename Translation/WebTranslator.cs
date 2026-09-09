using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Translation.Credentials;
using Translation.Exceptions;
using Translation.Http;
using Translation.Reference;
using Translation.Models;
using Translation.Providers;
using Translation.Settings;
using Translation.Utils;

namespace Translation
{
    public class WebTranslator
    {
        public ReadOnlyCollection<TranslationEngine> TranslationEngines
        {
            get { return _translationEngines; }
        }

        private ReadOnlyCollection<TranslationEngine> _translationEngines;

        private readonly List<KeyValuePair<TranslationRequest, string>> _translationCache;
        private readonly object _cacheSync = new object();

        private readonly KeyValuePair<TranslationRequest, string> defaultCachedResult =
            default(KeyValuePair<TranslationRequest, string>);

        private readonly IReadOnlyDictionary<TranslationEngineName, ITranslationProvider> _TranslationProviders;

        private readonly LanguageDetector _LanguageDetector;
        private readonly Func<string, string> _detectLanguage;

        private readonly ILogger _Logger;
        private readonly TranslationSettings _settings;
        private readonly ITranslationCredentialStore _credentials;

        private readonly string _translationSettingsPath = "TranslationSysSettings.json";

        public WebTranslator(ILogger logger)
            : this(logger, null, null, null, null)
        {
        }

        public WebTranslator(ILogger logger, ITranslationCredentialStore credentials)
            : this(logger, null, null, null, credentials)
        {
        }

        public WebTranslator(ILogger logger, IEnumerable<ITranslationProvider> translationProviders)
            : this(logger, translationProviders, null, null, null)
        {
        }

        internal WebTranslator(
            ILogger logger,
            IEnumerable<ITranslationProvider> translationProviders,
            TranslationSettings settings,
            Func<string, string> detectLanguage = null,
            ITranslationCredentialStore credentials = null,
            IReferenceTranslationSource referenceTranslations = null)
        {
            _Logger = logger;

            if (settings == null)
            {
                settings = TranslationSettingsStorage.Load(_translationSettingsPath, _Logger);
                if (settings == null)
                {
                    settings = new TranslationSettings();
                    TranslationSettingsStorage.Save(settings, _translationSettingsPath, _Logger);
                }
            }

            _settings = settings;
            ApiHttpClient.Configure(_settings.HttpRequestTimeoutMilliseconds,
                _settings.HttpReadWriteTimeoutMilliseconds);

            _translationCache =
                new List<KeyValuePair<TranslationRequest, string>>(_settings.TranslationCacheSize);

            _credentials = credentials ?? NullCredentialStore.Instance;

            _TranslationProviders = translationProviders != null
                ? translationProviders.ToDictionary(x => x.EngineName, x => x)
                : TranslationProviderFactory.CreateDefaultProviders(_Logger, _credentials, _settings);

            _LanguageDetector = new LanguageDetector(_settings.MaxSameLanguagePercent,
                _settings.NTextCatLanguageModelsPath, _Logger);
            _detectLanguage = detectLanguage ?? _LanguageDetector.TryDetectLanguage;

            if (referenceTranslations != null)
            {
                _referenceTranslations = referenceTranslations;
            }
            else
            {
                // Kept so the index can be rebuilt into the same file it is read
                // from. A supplied source has no file behind it, and then there
                // is nothing to update.
                _referenceIndexPath = SqliteReferenceTranslationSource.Resolve(_settings.ReferenceTranslationsPath);
                _referenceTranslations = new SqliteReferenceTranslationSource(ChooseReferenceIndex(), _Logger);
            }
        }

        private IReferenceTranslationSource _referenceTranslations;

        private readonly string _referenceIndexPath = string.Empty;

        /// <summary>
        /// Where an update writes the index. Not always the file being read:
        /// the application ships one too, and that one is left alone.
        /// </summary>
        public string ReferenceIndexPath => _referenceIndexPath;

        private string ChooseReferenceIndex()
        {
            return ReferenceIndexLocation.Choose(
                _settings.ReferenceTranslationsPath,
                _settings.ShippedReferenceTranslationsPath,
                _Logger);
        }

        /// <summary>The language the index was built in, empty when no index is loaded.</summary>
        public string ReferenceIndexLanguage => _referenceTranslations?.LanguageCode ?? string.Empty;

        /// <summary>The language the index is keyed on: the one the game is played in.</summary>
        public string ReferenceIndexSourceLanguage => _referenceTranslations?.SourceLanguageCode ?? string.Empty;

        /// <summary>The commit of the translation project the index was built from.</summary>
        public string ReferenceIndexRevision => _referenceTranslations?.Revision ?? string.Empty;

        /// <summary>
        /// The language a rebuilt index should be built in: the one the current
        /// index is in, since rebuilding it in another would quietly replace the
        /// translation the user has been reading.
        /// </summary>
        public string ReferenceIndexTargetLanguage
        {
            get
            {
                var current = ReferenceIndexLanguage;
                return current.Length > 0 ? current : _settings.ReferenceTranslationsLanguage;
            }
        }

        /// <summary>
        /// The language a rebuilt index should be keyed on, when nobody says
        /// otherwise: the one the current index uses.
        /// </summary>
        public string ReferenceIndexGameLanguage
        {
            get
            {
                return ReferenceIndexUpdater.ResolveGameLanguage(
                    GameLanguage,
                    ReferenceIndexSourceLanguage.Length > 0
                        ? ReferenceIndexSourceLanguage
                        : _settings.ReferenceTranslationsGameLanguage);
            }
        }

        /// <summary>How many lines the index holds.</summary>
        public int ReferenceIndexLines => _referenceTranslations?.LineCount ?? 0;

        /// <summary>The parsing rules the index was built by.</summary>
        public int ReferenceIndexRulesVersion => _referenceTranslations?.RulesVersion ?? 0;

        /// <summary>
        /// Lets go of the index file so a rebuilt one can be moved over it.
        ///
        /// A lookup already under way answers from the old source, which by then
        /// says it knows nothing, and the line goes to an engine as it would
        /// have before the index existed. That is the whole cost of a swap, and
        /// it lasts as long as a rename.
        /// </summary>
        public void CloseReferenceIndex()
        {
            (_referenceTranslations as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Opens the index again, as this character: the name and gender were
        /// read from the game once and nothing will announce them a second time.
        /// </summary>
        public void ReopenReferenceIndex()
        {
            if (_referenceIndexPath.Length == 0)
            {
                return;
            }

            var playerName = _referenceTranslations?.PlayerName ?? string.Empty;
            var playerIsFeminine = _referenceTranslations?.PlayerIsFeminine;

            var reopened = new SqliteReferenceTranslationSource(ChooseReferenceIndex(), _Logger)
            {
                PlayerName = playerName,
                PlayerIsFeminine = playerIsFeminine
            };

            var previous = _referenceTranslations;
            _referenceTranslations = reopened;
            (previous as IDisposable)?.Dispose();
        }

        /// <summary>
        /// Whether a line the game's translators have already rendered by hand
        /// should be used in place of asking a service to render it again.
        /// </summary>
        public bool UseReferenceTranslations { get; set; }

        /// <summary>
        /// The language the game is being played in, as read from the game
        /// itself. Empty until something has read it.
        ///
        /// This, and not the window's setting, is what the index has to agree
        /// with: a window may be set to work the language out line by line, and
        /// one line of German typed into chat says nothing about the language
        /// the game is drawing its dialogue in.
        /// </summary>
        public string GameLanguage { get; set; } = string.Empty;

        /// <summary>
        /// The character's name, so lines the game addresses to them can be
        /// recognised: what is stored has the name punched out.
        /// </summary>
        public string PlayerName
        {
            get => _referenceTranslations?.PlayerName ?? string.Empty;
            set
            {
                if (_referenceTranslations != null)
                {
                    _referenceTranslations.PlayerName = value;
                }
            }
        }

        /// <summary>The character's gender, which the Russian agrees with.</summary>
        public bool? PlayerIsFeminine
        {
            get => _referenceTranslations?.PlayerIsFeminine;
            set
            {
                if (_referenceTranslations != null)
                {
                    _referenceTranslations.PlayerIsFeminine = value;
                }
            }
        }

        /// <summary>A character's name as the translators render it.</summary>
        public bool TryGetReferenceSpeakerName(string speaker, TranslatorLanguage fromLang,
            TranslatorLanguage toLang, out string translated)
        {
            translated = string.Empty;

            // Taken once: the index is swapped out from under this when it is
            // rebuilt, and asking twice could ask two different sources.
            var reference = _referenceTranslations;

            if (!UseReferenceTranslations || !Speaks(reference, fromLang, toLang))
            {
                return false;
            }

            return reference.TryGetSpeakerName(speaker, out translated);
        }

        public void LoadLanguages()
        {
            try
            {
                var engines = new List<TranslationEngine>();

                foreach (var source in EngineLanguageCatalog.From(_settings))
                {
                    var languages =
                        JsonDataLoader.LoadJsonData<List<TranslatorLanguage>>(source.LanguagesPath, _Logger);
                    engines.Add(new TranslationEngine(source.Engine, languages, source.Quality));
                }

                _translationEngines = new ReadOnlyCollection<TranslationEngine>(
                    engines.OrderByDescending(x => x.Quality).ToList());
            }
            catch (Exception e)
            {
                _Logger.LogInformation("{Message}", Convert.ToString(e));
            }
        }

        public Task<TranslationResult> TranslateAsync(string inSentence, TranslationEngine translationEngine,
            TranslatorLanguage fromLang, TranslatorLanguage toLang)
        {
            return TranslateAsync(inSentence, translationEngine, fromLang, toLang, CancellationToken.None);
        }

        // Virtual so a test can stand in for the engine and script the faults
        // and the tokens instead of paying for real translations.
        public virtual Task<TranslationResult> TranslateAsync(
            string inSentence,
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            TranslatorLanguage toLang,
            CancellationToken cancellationToken)
        {
            return TranslateCoreAsync(inSentence, translationEngine, fromLang, toLang, cancellationToken);
        }

        /// <summary>
        /// Matched on the sentence as it came off the screen rather than after
        /// preprocessing: the index holds the game's own text, which is what we
        /// read, and preprocessing exists to help a machine translator rather
        /// than to help find an exact line.
        /// </summary>
        /// <summary>
        /// Whether the index answers this pair of languages.
        ///
        /// Both halves have to match. A line is read off the screen in the
        /// language the game is played in, so an index keyed on another finds
        /// nothing - and one built for another reading language would answer in
        /// a language nobody asked for.
        /// </summary>
        private bool Speaks(IReferenceTranslationSource reference,
            TranslatorLanguage fromLang, TranslatorLanguage toLang)
        {
            if (reference == null)
            {
                return false;
            }

            var indexed = reference.LanguageCode;
            if (string.IsNullOrEmpty(indexed) ||
                !string.Equals(indexed, toLang?.LanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var keyedOn = reference.SourceLanguageCode;
            if (string.IsNullOrEmpty(keyedOn))
            {
                return false;
            }

            // What the game itself says it is set to, and only failing that
            // what the window was told.
            //
            // When neither knows, let the index try: the lookup is by the exact
            // text, so a line in another language simply is not in there.
            // Refusing here instead would take the translations away from
            // everyone whose game could not be read, to guard against a match
            // that cannot happen.
            var declared = GameLanguage.Length > 0 ? GameLanguage : fromLang?.LanguageCode ?? string.Empty;
            if (declared.Length == 0 || string.Equals(declared, "auto", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return string.Equals(keyedOn, declared, StringComparison.OrdinalIgnoreCase);
        }

        private bool TryTranslateFromReference(string sentence, TranslatorLanguage fromLang,
            TranslatorLanguage toLang, out string translation)
        {
            translation = string.Empty;

            var reference = _referenceTranslations;

            if (!UseReferenceTranslations || !Speaks(reference, fromLang, toLang))
            {
                return false;
            }

            return reference.TryGetTranslation(sentence, out translation);
        }

        /// <summary>
        /// The hand-made translation of every line of a piece of text, when
        /// there is one for every line of it.
        ///
        /// All or none. A piece half by hand and half by a service reads as two
        /// different translations stacked on top of each other, and there is no
        /// telling from the outside which half is which - so where any line is
        /// unknown the whole piece goes to a service, as it did before.
        ///
        /// A line may carry a list marker - "1. " - which this application puts
        /// there itself so an answer can be found again after a service has
        /// rewritten the words around it. The marker is not part of what the
        /// game said, so it is set aside for the lookup and put back after.
        /// </summary>
        private bool TryTranslateLinesFromReference(string sentence, TranslatorLanguage fromLang,
            TranslatorLanguage toLang, out string translation)
        {
            translation = string.Empty;

            if (string.IsNullOrEmpty(sentence))
            {
                return false;
            }

            var lines = sentence.Split('\n');
            if (lines.Length < 2)
            {
                return false;
            }

            var translated = new string[lines.Length];

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0)
                {
                    translated[i] = string.Empty;
                    continue;
                }

                var marker = ListMarker(line);
                if (!TryTranslateFromReference(line.Substring(marker.Length), fromLang, toLang, out var known))
                {
                    return false;
                }

                translated[i] = marker + known;
            }

            translation = string.Join("\n", translated);
            return true;
        }

        /// <summary>
        /// The "1. " at the front of a line, or nothing when there is none.
        /// </summary>
        private static string ListMarker(string line)
        {
            var at = 0;
            while (at < line.Length && char.IsDigit(line[at]))
            {
                at++;
            }

            if (at == 0 || at > 2 || at >= line.Length)
            {
                return string.Empty;
            }

            // A dot or a bracket after the number, and nothing else will do.
            // The game writes lines that begin with a number and a space - "10
            // gil for that?" - and reading that as a marker looks the line up
            // without its first two words, which finds nothing.
            if (line[at] != '.' && line[at] != ')')
            {
                return string.Empty;
            }

            var after = at + 1;
            while (after < line.Length && line[after] == ' ')
            {
                after++;
            }

            return after < line.Length ? line.Substring(0, after) : string.Empty;
        }

        private async Task<TranslationResult> TranslateCoreAsync(string inSentence,
            TranslationEngine translationEngine, TranslatorLanguage fromLang, TranslatorLanguage toLang,
            CancellationToken cancellationToken)
        {
            if (translationEngine == null || fromLang == null || toLang == null)
            {
                return TranslationResult.Failure(
                    translationEngine?.EngineName ?? default,
                    TranslationFailureKind.ProviderUnavailable,
                    "Engine or language not specified.");
            }

            // What the window says the game is in, before any guessing. The
            // index is keyed on the language of the client, which is a setting
            // and not a property of the line: guessing it per line means a
            // German sentence typed into chat decides that the next line of
            // dialogue is German too.
            var declaredFrom = fromLang;

            fromLang = ResolveSourceLanguage(translationEngine, fromLang, inSentence);

            if (fromLang.SystemName == toLang.SystemName)
                return TranslationResult.Success(translationEngine.EngineName, inSentence);

            if ((inSentence ?? string.Empty).All(x => !char.IsLetter(x)))
                return TranslationResult.Success(translationEngine.EngineName, inSentence);

            switch (toLang.SystemName)
            {
                case "Korean":
                    if (_LanguageDetector.HasKorean(inSentence))
                        return TranslationResult.Success(translationEngine.EngineName, inSentence);
                    break;
                case "Japanese":
                    if (_LanguageDetector.HasJapanese(inSentence))
                        return TranslationResult.Success(translationEngine.EngineName, inSentence);
                    break;
            }

            // Somebody has already translated most of the game's dialogue by
            // hand. Asking a service to have another go at a line that is in
            // there is slower, costs a request, and reads worse.
            if (TryTranslateFromReference(inSentence, declaredFrom, toLang, out var referenceText))
            {
                return TranslationResult.Literary(translationEngine.EngineName, referenceText);
            }

            // And a second time, a line at a time, for text that arrives as
            // several lines at once. The question a cutscene asks comes that
            // way - the question and every answer under it, in one piece so
            // they stay together - and the index knows each of those lines
            // while knowing nothing of the piece they arrived in. Without this
            // a choice went to a service in full, and every answer to it read
            // as a machine had put it.
            if (TryTranslateLinesFromReference(inSentence, declaredFrom, toLang, out var lineByLine))
            {
                return TranslationResult.Literary(translationEngine.EngineName, lineByLine);
            }

            var normalizedSentence = HidePlayerName(PreprocessSentence(inSentence));
            var fromLangCode = fromLang.LanguageCode;
            var toLangCode = toLang.LanguageCode;

            var translationRequest =
                new TranslationRequest(normalizedSentence, translationEngine.EngineName, fromLangCode, toLangCode);
            KeyValuePair<TranslationRequest, string> cachedResult;
            lock (_cacheSync)
            {
                cachedResult = _translationCache.FirstOrDefault(x => x.Key == translationRequest);
            }

            if (!cachedResult.Equals(defaultCachedResult))
            {
                return TranslationResult.Success(
                    translationEngine.EngineName, ShowPlayerName(cachedResult.Value));
            }

            var result = await InvokeSelectedProviderAsync(translationEngine.EngineName, normalizedSentence,
                fromLangCode, toLangCode, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                result = await TryFallbackProvidersAsync(translationEngine, normalizedSentence, toLang,
                    fromLangCode, toLangCode, result, cancellationToken).ConfigureAwait(false);
            }

            if (result.IsSuccess && !string.IsNullOrEmpty(result.Text))
            {
                lock (_cacheSync)
                {
                    cachedResult = _translationCache.FirstOrDefault(x => x.Key == translationRequest);
                    if (cachedResult.Equals(defaultCachedResult))
                    {
                        _translationCache.Add(
                            new KeyValuePair<TranslationRequest, string>(translationRequest, result.Text));
                    }

                    if (_translationCache.Count > _settings.TranslationCacheSize - 10)
                        _translationCache.RemoveRange(0, _settings.TranslationCacheSize / 2);
                }
            }

            // Kept in the cache as it came back, with the name still standing
            // aside: what is cached is what the service said, and whose name
            // goes back into it is decided when it is read out.
            return result.IsSuccess ? result.WithText(ShowPlayerName(result.Text)) : result;
        }

        /// <summary>
        /// The character's own name is nobody's to translate.
        ///
        /// A service asked to translate "D'ark One..." hands back "Д'Арк
        /// Один..." - it has no way of knowing that those are not two English
        /// words. The hand-made translations know: they keep the name as a hole
        /// in the line and fill it afterwards. A service cannot be told that,
        /// so the name is taken out before it is asked and put back after.
        ///
        /// Taken out as a single character from the private-use area, which is
        /// what a service is likeliest to hand back untouched and in place -
        /// measured on the icons the game puts in its own lines, which come
        /// through Yandex unharmed.
        ///
        /// Both the name and the half of it people are called by: the game
        /// writes "Go swiftly, D'ark." as readily as it writes the whole thing.
        /// </summary>
        private const char PlayerNameMark = '\uE800';

        private const char PlayerForenameMark = '\uE801';

        private string HidePlayerName(string sentence)
        {
            var name = PlayerName;
            if (string.IsNullOrEmpty(sentence) || string.IsNullOrEmpty(name))
            {
                return sentence;
            }

            var hidden = ReplaceWholeWord(sentence, name, PlayerNameMark.ToString());

            var forename = Forename(name);
            if (forename.Length > 0 && forename.Length != name.Length)
            {
                hidden = ReplaceWholeWord(hidden, forename, PlayerForenameMark.ToString());
            }

            return hidden;
        }

        private string ShowPlayerName(string text)
        {
            var name = PlayerName;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(name))
            {
                return text;
            }

            return text
                .Replace(PlayerNameMark.ToString(), name)
                .Replace(PlayerForenameMark.ToString(), Forename(name));
        }

        private static string Forename(string playerName)
        {
            var space = playerName.IndexOf(' ');
            return space > 0 ? playerName.Substring(0, space) : playerName;
        }

        /// <summary>
        /// Only where the word stands on its own. A forename can be a few
        /// letters long and live inside other words - a character called Al
        /// would otherwise turn every "Also" in the game into a piece of
        /// somebody's name.
        /// </summary>
        private static string ReplaceWholeWord(string sentence, string word, string with)
        {
            if (word.Length == 0)
            {
                return sentence;
            }

            var built = new StringBuilder(sentence.Length);
            var at = 0;

            while (at < sentence.Length)
            {
                var found = sentence.IndexOf(word, at, StringComparison.Ordinal);
                if (found < 0)
                {
                    built.Append(sentence, at, sentence.Length - at);
                    break;
                }

                var before = found == 0 || !char.IsLetterOrDigit(sentence[found - 1]);
                var afterAt = found + word.Length;
                var after = afterAt >= sentence.Length || !char.IsLetterOrDigit(sentence[afterAt]);

                built.Append(sentence, at, found - at);
                built.Append(before && after ? with : word);
                at = afterAt;
            }

            return built.ToString();
        }

        /// <summary>
        /// Falls back to the other engines when the selected one fails.
        ///
        /// Without this a dead engine simply reported "Translation failed" for the
        /// rest of the session, and the only way out was to pick another engine by
        /// hand mid-conversation.
        ///
        /// Engines are tried best-quality first, and only those the user left
        /// switched on and that offer the target language. A missing API key is
        /// skipped silently rather than counted as a failure - it means the user
        /// never set that engine up.
        /// </summary>
        private async Task<TranslationResult> TryFallbackProvidersAsync(
            TranslationEngine selectedEngine,
            string sentence,
            TranslatorLanguage toLang,
            string fromLangCode,
            string toLangCode,
            TranslationResult originalFailure,
            CancellationToken cancellationToken)
        {
            var engines = _translationEngines;
            if (engines == null || engines.Count == 0)
            {
                return originalFailure;
            }

            foreach (var candidate in engines.OrderByDescending(x => x.Quality))
            {
                if (candidate.EngineName == selectedEngine.EngineName)
                {
                    continue;
                }

                // An engine the user switched off is not a stand-in. This used to
                // be moot: every engine needed a key, and one without a key threw
                // before it reached the network. An engine that needs no key has
                // nothing to throw, so without this it is called anyway - a dead
                // port on the player's own machine, or a service refusing us.
                if (!_credentials.IsEngineEnabled(candidate.EngineName))
                {
                    continue;
                }

                // Every engine ships its own language list with its own spelling of
                // the codes - DeepL says "RU", Yandex wants "ru" and rejects the
                // request outright otherwise - so the codes have to be re-resolved
                // against the engine actually being called.
                var candidateToCode = ResolveLanguageCode(candidate, toLang, toLangCode);
                if (candidateToCode == null)
                {
                    continue;
                }

                var candidateFromCode = string.Equals(fromLangCode, "auto", StringComparison.OrdinalIgnoreCase)
                    ? fromLangCode
                    : ResolveLanguageCode(candidate, null, fromLangCode) ?? "auto";

                var result = await InvokeSelectedProviderAsync(candidate.EngineName, sentence, candidateFromCode,
                    candidateToCode, cancellationToken).ConfigureAwait(false);

                if (result.IsSuccess && !string.IsNullOrEmpty(result.Text))
                {
                    _Logger?.LogInformation("{Message}",
                        "[FALLBACK] " + selectedEngine.EngineName + " -> " + candidate.EngineName);
                    return result;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            return originalFailure;
        }

        /// <summary>
        /// Finds how <paramref name="engine"/> spells a language, matching first on
        /// the language's own name and then on the code. Returns null when the
        /// engine does not offer it at all.
        /// </summary>
        internal static string ResolveLanguageCode(TranslationEngine engine, TranslatorLanguage language,
            string languageCode)
        {
            if (engine?.SupportedLanguages == null)
            {
                return null;
            }

            if (language != null && !string.IsNullOrEmpty(language.SystemName))
            {
                var byName = engine.SupportedLanguages.FirstOrDefault(x =>
                    string.Equals((x.SystemName ?? string.Empty).Trim(), language.SystemName.Trim(),
                        StringComparison.OrdinalIgnoreCase));

                if (byName != null)
                {
                    return byName.LanguageCode;
                }
            }

            var wanted = language?.LanguageCode ?? languageCode;
            if (string.IsNullOrEmpty(wanted))
            {
                return null;
            }

            var byCode = engine.SupportedLanguages.FirstOrDefault(x =>
                string.Equals(x.LanguageCode, wanted, StringComparison.OrdinalIgnoreCase));

            return byCode?.LanguageCode;
        }

        private async Task<TranslationResult> InvokeSelectedProviderAsync(
            TranslationEngineName engineName,
            string sentence,
            string fromLangCode,
            string toLangCode,
            CancellationToken cancellationToken)
        {
            if (!_TranslationProviders.TryGetValue(engineName, out var provider))
            {
                return TranslationResult.Failure(engineName, TranslationFailureKind.ProviderUnavailable,
                    "No provider registered for " + engineName);
            }

            try
            {
                var text = await provider.TranslateAsync(sentence, fromLangCode, toLangCode, cancellationToken)
                    .ConfigureAwait(false) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(text))
                {
                    return TranslationResult.Failure(engineName, TranslationFailureKind.EmptyResponse,
                        "Provider returned no translation.");
                }

                return TranslationResult.Success(engineName, text);
            }
            catch (QuotaExceededException quotaEx)
            {
                _Logger?.LogInformation("{Message}", "[PROVIDER_" + engineName + "_QUOTA] " + quotaEx.Message);
                return TranslationResult.Failure(engineName, TranslationFailureKind.QuotaExceeded, quotaEx.Message);
            }
            catch (MissingApiKeyException keyEx)
            {
                _Logger?.LogInformation("{Message}", "[PROVIDER_" + engineName + "_NO_KEY] " + keyEx.Message);
                return TranslationResult.Failure(engineName, TranslationFailureKind.MissingCredentials, keyEx.Message);
            }
            catch (Exception ex)
            {
                _Logger?.LogInformation("{Message}", "[PROVIDER_" + engineName + "_EXCEPTION] " + ex);
                return TranslationResult.Failure(engineName, TranslationFailureKind.ProviderException, ex.Message);
            }
        }

        private TranslatorLanguage ResolveSourceLanguage(
            TranslationEngine translationEngine,
            TranslatorLanguage fromLang,
            string sentence)
        {
            if (fromLang == null || fromLang.SystemName != "Auto")
                return fromLang;

            var detectedSystemLanguage = _detectLanguage(sentence ?? string.Empty);
            if (string.IsNullOrWhiteSpace(detectedSystemLanguage))
                return fromLang;

            var detectedLanguage = translationEngine.SupportedLanguages
                .FirstOrDefault(x => x.SystemName == detectedSystemLanguage);

            return detectedLanguage ?? fromLang;
        }


        private string PreprocessSentence(string sentence)
        {
            return sentence ?? string.Empty;
        }
    }
}