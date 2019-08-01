using FFXIITataruHelper.Translation;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FFXIITataruHelper.Utils
{
    public class TextToSpeeach
    {
        private WaveOut _waveOut;

        private List<Stream> _playList;

        private readonly WebTranslator _WebTranslator;

        private TranslatorLanguague TranslatorLanguague
        {
            get => _WebTranslator.TargetTranslatorLanguague;
        }

        public TextToSpeeach(WebTranslator _WebTranslator)
        {
            _playList = new List<Stream>();

            _waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback());
            _waveOut.PlaybackStopped += OnPlaybackStopped;

            this._WebTranslator = _WebTranslator;
        }

        public async void Play(string text)
        {
            try
            {
                await AddToPlayList(text);

                if (_waveOut.PlaybackState != PlaybackState.Playing)
                    PlayPlayList();
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
                _waveOut.Dispose();
            }
        }

        private async Task AddToPlayList(string text)
        {
            // Api only accepts up to 200 characters per request.
            if (text.Length <= 200)
            {
                var stream = await GetAudioStream(text);

                _playList.Add(stream);
            }
            else
            {
                var streams = new List<Stream>();
                var splitNumber = Math.Ceiling((double)text.Length / 160);

                var words = text.Split(' ').ToList();
                var wordNumber = (int)Math.Ceiling((double)words.Count / splitNumber);

                for (int i = 0; i < splitNumber; i++)
                {
                    var wds = words.Skip(wordNumber * i).Take(wordNumber).ToList();
                    var sentence = "";

                    wds.ForEach(w => sentence = sentence + $"{w} ");

                    var stream = await GetAudioStream(sentence);
                    streams.Add(stream);
                }

                foreach (var stream in streams)
                    _playList.Add(stream);
            }
        }

        private async Task<Stream> GetAudioStream(string text)
        {
            var builder = new UriBuilder("https://translate.google.com/translate_tts");
            builder.Query = $"ie=UTF-8&q={text}&tl={TranslatorLanguague.LanguageCode}&client=gtx&ttsspeed=1&ttsspeed=1";

            using (var client = new HttpClient())
            {
                var request = await client.GetAsync(builder.Uri);
                var result = await request.Content.ReadAsStreamAsync();

                return result;
            }
        }

        private async void PlayPlayList()
        {
            using (var memoryStream = new MemoryStream())
            {
                Stream stream = _playList.First();
                byte[] buffer = new byte[32768];
                int read;

                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    memoryStream.Write(buffer, 0, read);

                memoryStream.Position = 0;

                await Task.Run(() => Player(memoryStream));
            }
        }

        private void Player(MemoryStream memoryStream)
        {
            using (var blockAlignedStream = new BlockAlignReductionStream(
                    WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(memoryStream))))
            {
                _waveOut.Init(blockAlignedStream);
                _waveOut.Play();

                while (_waveOut.PlaybackState == PlaybackState.Playing)
                    System.Threading.Thread.Sleep(1000);
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            _playList.Remove(_playList.First());

            if (_playList.FirstOrDefault() != null)
                PlayPlayList();
            else
                _waveOut.Dispose();
        }
    }
}
