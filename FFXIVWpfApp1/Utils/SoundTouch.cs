using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FFXIVTataruHelper.Utils
{
    enum SoundTouchSettings
    {
        /// <summary>
        /// Available setting IDs for the 'setSetting' and 'get_setting' functions.
        /// Enable/disable anti-alias filter in pitch transposer (0 = disable)
        /// </summary>
        UseAaFilter = 0,

        /// <summary>
        /// Pitch transposer anti-alias filter length (8 .. 128 taps, default = 32)
        /// </summary>
        AaFilterLength = 1,

        /// <summary>
        /// Enable/disable quick seeking algorithm in tempo changer routine
        /// (enabling quick seeking lowers CPU utilization but causes a minor sound
        ///  quality compromising)
        /// </summary>
        UseQuickSeek = 2,

        /// <summary>
        /// Time-stretch algorithm single processing sequence length in milliseconds. This determines 
        /// to how long sequences the original sound is chopped in the time-stretch algorithm. 
        /// See "STTypes.h" or README for more information.
        /// </summary>
        SequenceMs = 3,

        /// <summary>
        /// Time-stretch algorithm seeking window length in milliseconds for algorithm that finds the 
        /// best possible overlapping location. This determines from how wide window the algorithm 
        /// may look for an optimal joining location when mixing the sound sequences back together. 
        /// See "STTypes.h" or README for more information.
        /// </summary>
        SeekWindowMs = 4,

        /// <summary>
        /// Time-stretch algorithm overlap length in milliseconds. When the chopped sound sequences 
        /// are mixed back together, to form a continuous sound stream, this parameter defines over 
        /// how long period the two consecutive sequences are let to overlap each other. 
        /// See "STTypes.h" or README for more information.
        /// </summary>
        OverlapMs = 5
    };

    class SoundTouch : IDisposable
    {
        private IntPtr handle;
        private string versionString;
        private readonly bool is64Bit;
        public SoundTouch()
        {
            is64Bit = Marshal.SizeOf<IntPtr>() == 8;

            handle = is64Bit ? SoundTouchInterop64.soundtouch_createInstance() :
                SoundTouchInterop32.soundtouch_createInstance();
        }

        public string VersionString
        {
            get
            {
                if (versionString == null)
                {
                    var s = new StringBuilder(100);
                    if (is64Bit)
                        SoundTouchInterop64.soundtouch_getVersionString2(s, s.Capacity);
                    else
                        SoundTouchInterop32.soundtouch_getVersionString2(s, s.Capacity);
                    versionString = s.ToString();
                }
                return versionString;
            }
        }

        public void SetPitchOctaves(float pitchOctaves)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setPitchOctaves(handle, pitchOctaves);
            else
                SoundTouchInterop32.soundtouch_setPitchOctaves(handle, pitchOctaves);
        }

        public void SetPitchSemiTones(float pitchSemiTones)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setPitchSemiTones(handle, pitchSemiTones);
            else
                SoundTouchInterop32.soundtouch_setPitchSemiTones(handle, pitchSemiTones);
        }

        public void SetSampleRate(int sampleRate)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setSampleRate(handle, (uint) sampleRate);
            else 
                SoundTouchInterop32.soundtouch_setSampleRate(handle, (uint)sampleRate);
        }

        public void SetChannels(int channels)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setChannels(handle, (uint) channels);
            else
                SoundTouchInterop32.soundtouch_setChannels(handle, (uint)channels);
        }

        private void DestroyInstance()
        {
            if (handle != IntPtr.Zero)
            {
                if (is64Bit)
                    SoundTouchInterop64.soundtouch_destroyInstance(handle);
                else
                    SoundTouchInterop32.soundtouch_destroyInstance(handle);
                handle = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            DestroyInstance();
            GC.SuppressFinalize(this);
        }

        ~SoundTouch()
        {
            DestroyInstance();
        }

        public void PutSamples(float[] samples, int numSamples)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_putSamples(handle, samples, numSamples);
            else
                SoundTouchInterop32.soundtouch_putSamples(handle, samples, numSamples);
        }

        public int ReceiveSamples(float[] outBuffer, int maxSamples)
        {
            if (is64Bit)
                return (int)SoundTouchInterop64.soundtouch_receiveSamples(handle, outBuffer, (uint)maxSamples);
            return (int)SoundTouchInterop32.soundtouch_receiveSamples(handle, outBuffer, (uint)maxSamples);
        }

        public bool IsEmpty
        {
            get
            {
                if (is64Bit)
                    return SoundTouchInterop64.soundtouch_isEmpty(handle) != 0;
                return SoundTouchInterop32.soundtouch_isEmpty(handle) != 0;
            }
        }

        public int NumberOfSamplesAvailable
        {
            get
            {
                if (is64Bit)
                   return (int)SoundTouchInterop64.soundtouch_numSamples(handle);
                return (int)SoundTouchInterop32.soundtouch_numSamples(handle);
            }
        }

        public int NumberOfUnprocessedSamples
        {
            get
            {
                if (is64Bit)
                    return SoundTouchInterop64.soundtouch_numUnprocessedSamples(handle);
                return SoundTouchInterop32.soundtouch_numUnprocessedSamples(handle);
            }
        }

        public void Flush()
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_flush(handle);
            else
                SoundTouchInterop32.soundtouch_flush(handle);
        }

        public void Clear()
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_clear(handle);
            else
                SoundTouchInterop32.soundtouch_clear(handle);
        }

        public void SetRate(float newRate)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setRate(handle, newRate);
            else
                SoundTouchInterop32.soundtouch_setRate(handle, newRate);
        }

        public void SetTempo(float newTempo)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setTempo(handle, newTempo);
            else
                SoundTouchInterop32.soundtouch_setTempo(handle, newTempo);
        }

        public int GetUseAntiAliasing()
        {
            if (is64Bit)
                return SoundTouchInterop64.soundtouch_getSetting(handle, SoundTouchSettings.UseAaFilter);
            return SoundTouchInterop32.soundtouch_getSetting(handle, SoundTouchSettings.UseAaFilter);
        }

        public void SetUseAntiAliasing(bool useAntiAliasing)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setSetting(handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
            else
                SoundTouchInterop32.soundtouch_setSetting(handle, SoundTouchSettings.UseAaFilter, useAntiAliasing ? 1 : 0);
        }

        public void SetUseQuickSeek(bool useQuickSeek)
        {
            if (is64Bit)
                SoundTouchInterop64.soundtouch_setSetting(handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
            else
                SoundTouchInterop32.soundtouch_setSetting(handle, SoundTouchSettings.UseQuickSeek, useQuickSeek ? 1 : 0);
        }

        public int GetUseQuickSeek()
        {
            if (is64Bit)
                return SoundTouchInterop64.soundtouch_getSetting(handle, SoundTouchSettings.UseQuickSeek);
            return SoundTouchInterop32.soundtouch_getSetting(handle, SoundTouchSettings.UseQuickSeek);
        }
    }

    class SoundTouchInterop32
    {
        private const string SoundTouchDllName = "SoundTouch.dll";

        /// <summary>
        /// Create a new instance of SoundTouch processor.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr soundtouch_createInstance();

        /// <summary>
        /// Destroys a SoundTouch processor instance.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_destroyInstance(IntPtr h);

        /// <summary>
        /// Get SoundTouch library version string - alternative function for 
        /// environments that can't properly handle character string as return value
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_getVersionString2(StringBuilder versionString, int bufferSize);

        /// <summary>
        /// Get SoundTouch library version Id
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint soundtouch_getVersionId();

        /// <summary>
        /// Sets new rate control value. Normal rate = 1.0, smaller values
        /// represent slower rate, larger faster rates.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setRate(IntPtr h, float newRate);

        /// <summary>
        /// Sets new tempo control value. Normal tempo = 1.0, smaller values
        /// represent slower tempo, larger faster tempo.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setTempo(IntPtr h, float newTempo);

        /// <summary>
        /// Sets new rate control value as a difference in percents compared
        /// to the original rate (-50 .. +100 %);
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setRateChange(IntPtr h, float newRate);

        /// <summary>
        /// Sets new tempo control value as a difference in percents compared
        /// to the original tempo (-50 .. +100 %);
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

        /// <summary>
        /// Sets new pitch control value. Original pitch = 1.0, smaller values
        /// represent lower pitches, larger values higher pitch.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setPitch(IntPtr h, float newPitch);

        /// <summary>
        /// Sets pitch change in octaves compared to the original pitch  
        /// (-1.00 .. +1.00);
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setPitchOctaves(IntPtr h, float newPitch);

        /// <summary>
        /// Sets pitch change in semi-tones compared to the original pitch
        /// (-12 .. +12);
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setPitchSemiTones(IntPtr h, float newPitch);

        /// <summary>
        /// Sets the number of channels, 1 = mono, 2 = stereo
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

        /// <summary>
        /// Sets sample rate.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

        /// <summary>
        /// Flushes the last samples from the processing pipeline to the output.
        /// Clears also the internal processing buffers.
        ///
        /// Note: This function is meant for extracting the last samples of a sound
        /// stream. This function may introduce additional blank samples in the end
        /// of the sound stream, and thus it's not recommended to call this function
        /// in the middle of a sound stream.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_flush(IntPtr h);

        /// <summary>
        /// Adds 'numSamples' pcs of samples from the 'samples' memory position into
        /// the input of the object. Notice that sample rate _has_to_ be set before
        /// calling this function, otherwise throws a runtime_error exception.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="samples">Pointer to sample buffer.</param>
        /// <param name="numSamples">Number of samples in buffer. Notice that in case of stereo-sound a single sample contains data for both channels.</param>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_putSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] samples, int numSamples);

        /// <summary>
        /// Clears all the samples in the object's output and internal processing
        /// buffers.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void soundtouch_clear(IntPtr h);

        /// <summary>
        /// Changes a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="settingId">Setting ID number, see SETTING_... defines.</param>
        /// <param name="value">New setting value.</param>
        /// <returns>'TRUE' if the setting was succesfully changed</returns>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool soundtouch_setSetting(IntPtr h, SoundTouchSettings settingId, int value);

        /// <summary>
        /// Reads a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="settingId">Setting ID number, see SETTING_... defines.</param>
        /// <returns>The setting value</returns>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int soundtouch_getSetting(IntPtr h, SoundTouchSettings settingId);

        /// <summary>
        /// Returns number of samples currently unprocessed.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int soundtouch_numUnprocessedSamples(IntPtr h);

        /// <summary>
        ///  Adjusts book-keeping so that given number of samples are removed from beginning of the 
        ///  sample buffer without copying them anywhere. 
        /// 
        ///  Used to reduce the number of samples in the buffer when accessing the sample buffer directly
        ///  with 'ptrBegin' function.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="outBuffer">Buffer where to copy output samples.</param>
        /// <param name="maxSamples">How many samples to receive at max.</param>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint soundtouch_receiveSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] outBuffer, uint maxSamples);

        /// <summary>
        /// Returns number of samples currently available.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint soundtouch_numSamples(IntPtr h);

        /// <summary>
        /// Returns nonzero if there aren't any samples available for outputting.
        /// </summary>
        [DllImport(SoundTouchDllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int soundtouch_isEmpty(IntPtr h);

    }

    class SoundTouchInterop64
    {
        private const string SoundTouchDllName = "SoundTouch_x64.dll";

        /// <summary>
        /// Create a new instance of SoundTouch processor.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern IntPtr soundtouch_createInstance();

        /// <summary>
        /// Destroys a SoundTouch processor instance.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_destroyInstance(IntPtr h);

        /// <summary>
        /// Get SoundTouch library version string - alternative function for 
        /// environments that can't properly handle character string as return value
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_getVersionString2(StringBuilder versionString, int bufferSize);

        /// <summary>
        /// Get SoundTouch library version Id
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern uint soundtouch_getVersionId();

        /// <summary>
        /// Sets new rate control value. Normal rate = 1.0, smaller values
        /// represent slower rate, larger faster rates.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setRate(IntPtr h, float newRate);

        /// <summary>
        /// Sets new tempo control value. Normal tempo = 1.0, smaller values
        /// represent slower tempo, larger faster tempo.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setTempo(IntPtr h, float newTempo);

        /// <summary>
        /// Sets new rate control value as a difference in percents compared
        /// to the original rate (-50 .. +100 %);
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setRateChange(IntPtr h, float newRate);

        /// <summary>
        /// Sets new tempo control value as a difference in percents compared
        /// to the original tempo (-50 .. +100 %);
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setTempoChange(IntPtr h, float newTempo);

        /// <summary>
        /// Sets new pitch control value. Original pitch = 1.0, smaller values
        /// represent lower pitches, larger values higher pitch.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setPitch(IntPtr h, float newPitch);

        /// <summary>
        /// Sets pitch change in octaves compared to the original pitch  
        /// (-1.00 .. +1.00);
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setPitchOctaves(IntPtr h, float newPitch);

        /// <summary>
        /// Sets pitch change in semi-tones compared to the original pitch
        /// (-12 .. +12);
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setPitchSemiTones(IntPtr h, float newPitch);

        /// <summary>
        /// Sets the number of channels, 1 = mono, 2 = stereo
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setChannels(IntPtr h, uint numChannels);

        /// <summary>
        /// Sets sample rate.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_setSampleRate(IntPtr h, uint srate);

        /// <summary>
        /// Flushes the last samples from the processing pipeline to the output.
        /// Clears also the internal processing buffers.
        ///
        /// Note: This function is meant for extracting the last samples of a sound
        /// stream. This function may introduce additional blank samples in the end
        /// of the sound stream, and thus it's not recommended to call this function
        /// in the middle of a sound stream.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_flush(IntPtr h);

        /// <summary>
        /// Adds 'numSamples' pcs of samples from the 'samples' memory position into
        /// the input of the object. Notice that sample rate _has_to_ be set before
        /// calling this function, otherwise throws a runtime_error exception.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="samples">Pointer to sample buffer.</param>
        /// <param name="numSamples">Number of samples in buffer. Notice that in case of stereo-sound a single sample contains data for both channels.</param>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_putSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] samples, int numSamples);

        /// <summary>
        /// Clears all the samples in the object's output and internal processing
        /// buffers.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern void soundtouch_clear(IntPtr h);

        /// <summary>
        /// Changes a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="settingId">Setting ID number, see SETTING_... defines.</param>
        /// <param name="value">New setting value.</param>
        /// <returns>'TRUE' if the setting was succesfully changed</returns>
        [DllImport(SoundTouchDllName)]
        public static extern bool soundtouch_setSetting(IntPtr h, SoundTouchSettings settingId, int value);

        /// <summary>
        /// Reads a setting controlling the processing system behaviour. See the
        /// 'SETTING_...' defines for available setting ID's.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="settingId">Setting ID number, see SETTING_... defines.</param>
        /// <returns>The setting value</returns>
        [DllImport(SoundTouchDllName)]
        public static extern int soundtouch_getSetting(IntPtr h, SoundTouchSettings settingId);

        /// <summary>
        /// Returns number of samples currently unprocessed.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern int soundtouch_numUnprocessedSamples(IntPtr h);

        /// <summary>
        ///  Adjusts book-keeping so that given number of samples are removed from beginning of the 
        ///  sample buffer without copying them anywhere. 
        /// 
        ///  Used to reduce the number of samples in the buffer when accessing the sample buffer directly
        ///  with 'ptrBegin' function.
        /// </summary>
        /// <param name="h">Handle</param>
        /// <param name="outBuffer">Buffer where to copy output samples.</param>
        /// <param name="maxSamples">How many samples to receive at max.</param>
        [DllImport(SoundTouchDllName)]
        public static extern uint soundtouch_receiveSamples(IntPtr h, [MarshalAs(UnmanagedType.LPArray)] float[] outBuffer, uint maxSamples);

        /// <summary>
        /// Returns number of samples currently available.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern uint soundtouch_numSamples(IntPtr h);

        /// <summary>
        /// Returns nonzero if there aren't any samples available for outputting.
        /// </summary>
        [DllImport(SoundTouchDllName)]
        public static extern int soundtouch_isEmpty(IntPtr h);

    }
}
