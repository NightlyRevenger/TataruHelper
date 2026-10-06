using System;
using System.Runtime.InteropServices;

namespace FFXIVTataruHelper.Utils
{
    /// <summary>
    /// Whether this copy is running under Wine, which is how it runs on Linux:
    /// in the game's own prefix, beside the game, so that reading the game's
    /// memory goes through the same wineserver the game does.
    ///
    /// Wine says so the way it always has - its ntdll exports wine_get_version,
    /// which Windows's never did. Nothing else is asked: a copy on Windows sees
    /// no such export and behaves exactly as it always has.
    ///
    /// Wanted because a few things that are harmless on Windows are not under
    /// Wine on a Linux desktop, where windows from the game and from this
    /// application are stacked by the desktop's own window manager.
    /// </summary>
    public static class WineEnvironment
    {
        private static readonly Lazy<string> _version = new Lazy<string>(ReadVersion);

        /// <summary>Whether the process is running under Wine.</summary>
        public static bool IsRunning => _version.Value != null;

        /// <summary>Wine's own version string, or null on Windows.</summary>
        public static string Version => _version.Value;

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
