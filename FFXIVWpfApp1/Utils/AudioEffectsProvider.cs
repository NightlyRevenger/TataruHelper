using System;
using NAudio.Wave;

namespace FFXIVTataruHelper.Utils
{
    class AudioEffectsProvider : ISampleProvider, IDisposable
    {
        public WaveFormat WaveFormat => _sourceProvider.WaveFormat;

        private readonly ISampleProvider _sourceProvider;
        private readonly SoundTouch _soundTouch;

        private readonly float[] _sourceReadBuffer;
        private readonly float[] _soundTouchReadBuffer;

        private readonly int _channelCount;

        public AudioEffectsProvider(ISampleProvider sourceProvider, int readDurationMilliseconds)
        {
            _sourceProvider = sourceProvider;
            _channelCount = WaveFormat.Channels;

            _soundTouch = new SoundTouch();
            _soundTouch.SetRate(1.0f);
            _soundTouch.SetPitchSemiTones(0f);
            _soundTouch.SetSampleRate(WaveFormat.SampleRate);
            _soundTouch.SetChannels(_channelCount);

            _sourceReadBuffer = new float[(WaveFormat.SampleRate * _channelCount * (long)readDurationMilliseconds) / 1000];
            _soundTouchReadBuffer = new float[_sourceReadBuffer.Length * 10];
        }

        public AudioEffectsProvider(ISampleProvider sourceProvider, int readDurationMilliseconds, float speed) : this(sourceProvider, readDurationMilliseconds)
        {
            _soundTouch.SetRate(speed);
        }

        public AudioEffectsProvider(ISampleProvider sourceProvider, int readDurationMilliseconds, float speed, float pitch) : this(sourceProvider, readDurationMilliseconds, speed)
        {
            _soundTouch.SetPitchSemiTones(pitch);
        }

        public void SetSpeed(float value)
        {
            if (_soundTouch != null)
                _soundTouch.SetRate(value);
        }

        public void SetPitch(float value)
        {
            if (_soundTouch != null)
                _soundTouch.SetPitchSemiTones(value);
        }

        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;
            bool reachedEndOfSource = false;

            while (samplesRead < count)
            {
                if (_soundTouch.NumberOfSamplesAvailable == 0)
                {
                    var readFromSource = _sourceProvider.Read(_sourceReadBuffer, 0, _sourceReadBuffer.Length);
                    if (readFromSource > 0)
                    {
                        _soundTouch.PutSamples(_sourceReadBuffer, readFromSource / _channelCount);
                    }
                    else
                    {
                        reachedEndOfSource = true;
                        _soundTouch.Flush();
                    }
                }
                var desiredSampleFrames = (count - samplesRead) / _channelCount;

                var received = _soundTouch.ReceiveSamples(_soundTouchReadBuffer, desiredSampleFrames) * _channelCount;
                for (int n = 0; n < received; n++)
                {
                    buffer[offset + samplesRead++] = _soundTouchReadBuffer[n];
                }
                if (received == 0 && reachedEndOfSource) break;
            }

            if (reachedEndOfSource)
                _soundTouch.Clear();

            return samplesRead;
        }

        public void Dispose()
        {
            _soundTouch.Dispose();
        }
    }
}