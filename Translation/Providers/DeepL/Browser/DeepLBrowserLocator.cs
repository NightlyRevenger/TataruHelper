using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.Win32;

namespace Translation.Providers.DeepL.Browser
{
    /// <summary>A Chromium-based browser found on this machine, and how to start it.</summary>
    public sealed class FoundBrowser
    {
        public FoundBrowser(string path, bool isHostProgram)
        {
            Path = path;
            IsHostProgram = isHostProgram;
        }

        /// <summary>A Windows path, or a path on the host when <see cref="IsHostProgram"/> is set.</summary>
        public string Path { get; }

        /// <summary>
        /// Running under Wine, the browser is the host system's own program and
        /// has to be started through Wine's bridge to it.
        /// </summary>
        public bool IsHostProgram { get; }
    }

    /// <summary>
    /// Finds a browser the DeepL page can be driven in. The player's own is
    /// used, so nothing is shipped with the application.
    /// </summary>
    public static class DeepLBrowserLocator
    {
        private static readonly string[] HostPrograms =
        {
            "/usr/bin/google-chrome-stable", "/usr/bin/google-chrome", "/usr/bin/chromium",
            "/usr/bin/chromium-browser", "/usr/bin/brave", "/usr/bin/brave-browser",
            "/usr/bin/microsoft-edge-stable", "/usr/bin/vivaldi-stable", "/usr/bin/vivaldi",
        };

        public static bool IsRunningUnderWine()
        {
            if (!OperatingSystem.IsWindows())
            {
                return false;
            }

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Wine"))
                {
                    return key != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static FoundBrowser Find()
        {
            if (IsRunningUnderWine())
            {
                foreach (var program in HostPrograms)
                {
                    // Wine shows the host's file system as drive Z:.
                    if (File.Exists("Z:" + program.Replace('/', '\\')))
                    {
                        return new FoundBrowser(program, true);
                    }
                }

                return null;
            }

            foreach (var path in WindowsCandidates())
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    return new FoundBrowser(path, false);
                }
            }

            return null;
        }

        private static IEnumerable<string> WindowsCandidates()
        {
            // The ones installed with the system come first: Edge is on every Windows 10 and 11.
            var roots = new[]
            {
                Environment.GetEnvironmentVariable("ProgramFiles(x86)"),
                Environment.GetEnvironmentVariable("ProgramFiles"),
                Environment.GetEnvironmentVariable("LocalAppData"),
            };

            var relative = new[]
            {
                @"Microsoft\Edge\Application\msedge.exe",
                @"Google\Chrome\Application\chrome.exe",
                @"BraveSoftware\Brave-Browser\Application\brave.exe",
                @"Chromium\Application\chrome.exe",
                @"Vivaldi\Application\vivaldi.exe",
            };

            foreach (var name in relative)
            {
                foreach (var root in roots)
                {
                    if (!string.IsNullOrEmpty(root))
                    {
                        yield return Path.Combine(root, name);
                    }
                }
            }

            if (OperatingSystem.IsWindows())
            {
                foreach (var exe in new[] { "msedge.exe", "chrome.exe", "brave.exe" })
                {
                    yield return ReadAppPath(exe);
                }
            }
        }

        private static string ReadAppPath(string exe)
        {
            try
            {
                var subKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exe;
                using (var key = Registry.LocalMachine.OpenSubKey(subKey) ?? Registry.CurrentUser.OpenSubKey(subKey))
                {
                    return key?.GetValue(null) as string;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
