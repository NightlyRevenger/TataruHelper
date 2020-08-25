using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FFXIVTataruHelper.Utils
{
    public class TextToSpeech
    {
        private const string BASE_URL_TTS = "https://translate.google.com/translate_tts?ie=UTF-8";

        private List<MemoryStream> _playList;

        private float _speed;
        private float _pitch;
        private float _volume;

        private WaveOut _waveOut;
        private AudioEffectsProvider _currentFile;

        public bool IsPlaying
        {
            get => _waveOut.PlaybackState == PlaybackState.Playing;
        }
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;

                _waveOut.Volume = _volume;
            }
        }
        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;

                if (_currentFile != null)
                    _currentFile.SetSpeed(_speed);
            }
        }
        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = value;

                if (_currentFile != null)
                    _currentFile.SetPitch(_pitch);
            }
        }

        public TextToSpeech()
        {
            _playList = new List<MemoryStream>();
            _waveOut = new WaveOut();

            _waveOut.PlaybackStopped += OnPlaybackStopped;
        }

        public async Task PlayAsync(string text, string lang)
        {
            try
            {
                await AddToPlayList(text, lang);

                if (!IsPlaying)
                    Player();
            }
            catch (Exception e)
            {
                Logger.WriteLog(e);
                _waveOut.Dispose();
            }
        }

        private async Task AddToPlayList(string text, string lang)
        {
            MemoryStream voiceStream;

            if (text.Length > 200)
            {
                var words = text.Split(' ');
                var sentence = "";

                foreach (var word in words)
                {
                    if ((sentence.Length + word.Length + 1) <= 200)
                        sentence += $"{word} ";

                    else
                    {
                        voiceStream = await GetVoiceStream(sentence, lang);
                        _playList.Add(voiceStream);

                        sentence = $"{word} ";
                    }
                }

                if (sentence.Length > 0)
                {
                    voiceStream = await GetVoiceStream(sentence, lang);
                    _playList.Add(voiceStream);
                }
            }
            else
            {
                voiceStream = await GetVoiceStream(text, lang);
                _playList.Add(voiceStream);
            }
        }

        private async void Player()
        {
            using (var voiceStream = _playList.First())
            using (var mp3reader = new Mp3FileReader(voiceStream))
            using (var effectsProvider = new AudioEffectsProvider(mp3reader.ToSampleProvider(), 100, Speed, Pitch))
            {
                _waveOut.Init(effectsProvider);
                _currentFile = effectsProvider;

                _waveOut.Play();

                while (IsPlaying)
                {
                    await Task.Delay(1000);
                }
            }
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e)
        {
            _currentFile = null;

            _playList.Remove(_playList.First());

            if (_playList.Count > 0)
                Player();
        }

        private async Task<MemoryStream> GetVoiceStream(string text, string lang)
        {
            var uri = new Uri(BASE_URL_TTS + $"&q={text}&tl={lang}&client=tw-ob");

            var ms = new MemoryStream();

            using (var client = new HttpClient())
            using (var result = await client.GetStreamAsync(uri.AbsoluteUri))
            {
                await result.CopyToAsync(ms);
                ms.Position = 0;
            }

            return ms;
        }
    }
}
