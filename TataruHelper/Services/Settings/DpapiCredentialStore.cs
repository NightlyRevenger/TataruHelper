using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

using Translation.Credentials;
using Translation.Models;

namespace FFXIVTataruHelper.Services.Settings
{
    public sealed class DpapiCredentialStore : ITranslationCredentialStore
    {
        private const string SecretsFileName = "Secrets.dat";
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("TataruHelper.TranslationSecrets.v1");

        private readonly string _path;
        private readonly object _gate = new object();
        private Dictionary<string, string> _entries;

        /// <summary>
        /// The file existed but could not be read at construction. While it is
        /// set, a save must not write the in-memory state over the disk copy -
        /// memory holds what the file failed to give us, i.e. nothing.
        /// </summary>
        private bool _diskLoadFailed;

        public DpapiCredentialStore() : this(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TataruHelper"))
        {
        }

        public DpapiCredentialStore(string directory)
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, SecretsFileName);
            _entries = Load();
        }

        public string GetApiKey(TranslationEngineName engine) => Get(Key(engine, "apiKey"));

        public string GetRegion(TranslationEngineName engine) => Get(Key(engine, "region"));

        public string GetModel(TranslationEngineName engine) => Get(Key(engine, "model"));

        public string GetEndpoint(TranslationEngineName engine) => Get(Key(engine, "endpoint"));

        public bool IsEngineEnabled(TranslationEngineName engine)
        {
            var raw = Get(Key(engine, "enabled"));
            if (raw.Length == 0)
                return TranslationEngineDefaults.IsOnByDefault(engine);

            return !string.Equals(raw, "0", StringComparison.Ordinal);
        }

        public void SetApiKey(TranslationEngineName engine, string apiKey) => Set(Key(engine, "apiKey"), apiKey);

        public void SetRegion(TranslationEngineName engine, string region) => Set(Key(engine, "region"), region);

        public void SetModel(TranslationEngineName engine, string model) => Set(Key(engine, "model"), model);

        public void SetEndpoint(TranslationEngineName engine, string endpoint)
            => Set(Key(engine, "endpoint"), endpoint);

        /// <summary>
        /// Writes "1" rather than an empty string: <see cref="Set"/> treats empty
        /// as "forget this", which for an engine that is off by default reads
        /// back as off again - the switch would not hold.
        /// </summary>
        public void SetEngineEnabled(TranslationEngineName engine, bool isEnabled)
            => Set(Key(engine, "enabled"), isEnabled ? "1" : "0");

        public void Save()
        {
            lock (_gate)
            {
                Persist(_entries);
            }
        }

        private static string Key(TranslationEngineName engine, string field) => engine + ":" + field;

        /// <summary>
        /// Every value here - a key, a model, an address, a region - is one
        /// word with nothing around it, and whatever was around it when it
        /// was pasted in is not part of it. A trailing space after a model
        /// name made OpenAI answer that the model did not exist, for every
        /// model typed, while the default one worked. Trimmed on the way in,
        /// and on the way out for what was saved before.
        /// </summary>
        private string Get(string key)
        {
            lock (_gate)
            {
                string value;
                return _entries.TryGetValue(key, out value) ? (value ?? string.Empty).Trim() : string.Empty;
            }
        }

        private void Set(string key, string value)
        {
            value = value?.Trim();

            lock (_gate)
            {
                if (string.IsNullOrEmpty(value))
                    _entries.Remove(key);
                else
                    _entries[key] = value;
            }
        }

        private Dictionary<string, string> Load()
        {
            try
            {
                if (!File.Exists(_path))
                    return new Dictionary<string, string>(StringComparer.Ordinal);

                var encrypted = File.ReadAllBytes(_path);
                if (encrypted.Length == 0)
                    return new Dictionary<string, string>(StringComparer.Ordinal);

                var plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(plain);
                var loaded = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                return loaded ?? new Dictionary<string, string>(StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                // The file is there but will not come out - DPAPI refusing, or a
                // file only halfway written. It still holds what it holds, so
                // remember it and keep every later save away from it.
                try
                {
                    if (File.Exists(_path))
                    {
                        _diskLoadFailed = true;
                        Logger.WriteLog("DpapiCredentialStore could not read " + _path + "; leaving the file in place.");
                        Logger.WriteLog(ex);
                    }
                }
                catch (Exception probeEx)
                {
                    Logger.WriteLog("DpapiCredentialStore failed to probe " + _path);
                    Logger.WriteLog(probeEx);
                }

                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        private void Persist(Dictionary<string, string> entries)
        {
            if (_diskLoadFailed)
            {
                // The disk copy could not be read, so writing the in-memory
                // state (loaded as nothing) over it would put it out of
                // existence. The keys live in memory for this session.
                return;
            }

            // The bytes go to a side file and move across in one step, so a
            // crash halfway cannot leave the live file empty or torn.
            var temporaryPath = _path + ".new";

            try
            {
                var json = JsonConvert.SerializeObject(entries);
                var plain = Encoding.UTF8.GetBytes(json);
                var encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(temporaryPath, encrypted);
                File.Move(temporaryPath, _path, true);
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(temporaryPath))
                        File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                }

                // swallow - keys live in-memory for this session even if disk write fails.
                Logger.WriteLog("DpapiCredentialStore could not persist " + _path);
                Logger.WriteLog(ex);
            }
        }
    }
}