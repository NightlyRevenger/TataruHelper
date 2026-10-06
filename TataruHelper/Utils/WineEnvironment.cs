using System;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace FFXIVTataruHelper.Utils
{
    /// <summary>
    /// Whether this copy is running under Wine, which is how it runs on Linux:
    /// in the game's own prefix, beside the game, so that reading the game's
    /// memory goes through the same wineserver the game does.
    ///
    /// Told by the registry: every prefix has HKLM\Software\Wine, written by
    /// wineboot when the prefix is made, and Windows never has. The usual test
    /// - ntdll exporting wine_get_version - is not good enough here, and was
    /// the first one tried: XIVLauncher's prefix sets HideWineExports, which
    /// hides exactly that export from every program, so a copy beside the game
    /// answered "not Wine" and went on stealing the game's focus. The export
    /// is still read when it can be, for the version number in the log.
    ///
    /// A copy on Windows finds neither, and behaves exactly as it always has.
    ///
    /// Wanted because a few things that are harmless on Windows are not under
    /// Wine on a Linux desktop, where windows from the game and from this
    /// application are stacked by the desktop's own window manager.
    /// </summary>
    public static class WineEnvironment
    {
        private static readonly Lazy<string> _version = new Lazy<string>(ReadVersion);

        private static readonly Lazy<bool> _isRunning = new Lazy<bool>(Detect);

        /// <summary>Whether the process is running under Wine.</summary>
        public static bool IsRunning => _isRunning.Value;

        /// <summary>
        /// Wine's own version string - or null, on Windows or under a Wine
        /// that hides it.
        /// </summary>
        public static string Version => _version.Value;

        private static bool Detect()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Wine"))
                {
                    if (key != null)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // A registry that cannot be read says nothing either way.
            }

            return Version != null;
        }

        private static string ReadVersion()
        {
            try
            {
                var ntdll = GetModuleHandleW("ntdll.dll");
                if (ntdll == IntPtr.Zero)
                {
                    return null;
                }

                var export = GetProcAddress(ntdll, "wine_get_version");
                if (export == IntPtr.Zero)
                {
                    return null;
                }

                var version = Marshal.GetDelegateForFunctionPointer<WineGetVersion>(export)();
                return Marshal.PtrToStringAnsi(version) ?? string.Empty;
            }
            catch (Exception)
            {
                // Nothing here is worth stopping over: not knowing is Windows.
                return null;
            }
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr WineGetVersion();

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr GetModuleHandleW(string moduleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);
    }
}
