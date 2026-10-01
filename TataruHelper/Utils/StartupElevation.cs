using System;

namespace FFXIVTataruHelper.Utils
{
    /// <summary>
    /// Whether this copy has to start itself again with a raised hand.
    ///
    /// The application reads another process's memory, so it wants to be an
    /// administrator - and for a long time it said so in its manifest, which
    /// seemed the honest way to ask. It is not: a manifest that demands
    /// elevation makes the executable unstartable by anything that cannot
    /// elevate, and the installer is exactly that. Velopack runs its hooks
    /// with a plain CreateProcess, so every one of them came back as
    ///
    ///   Failed to start hook --veloapp-install:
    ///   The requested operation requires elevation. (os error 740)
    ///
    /// and the installer told the person it had failed. Read off a reporter's
    /// own log on 2026-10-01. It was invisible here because an install run
    /// from an elevated console hands its elevation down to the hooks.
    ///
    /// So the manifest asks for nothing and the application asks for itself,
    /// which it can do through the shell - the one way that is allowed to
    /// raise a prompt.
    /// </summary>
    public static class StartupElevation
    {
        /// <summary>
        /// Whether to start again as an administrator.
        ///
        /// Not for a copy that is already one, and not for a job: building the
        /// reference index is run by the release script, which reads the exit
        /// code, and a copy that steps aside for an elevated child has no exit
        /// code to give it. The script is elevated already.
        /// </summary>
        public static bool IsWanted(string[] args, bool isAdministrator)
        {
            if (isAdministrator)
            {
                return false;
            }

            return !IsAJob(args);
        }

        private static bool IsAJob(string[] args)
        {
            return ReferenceIndexCommand.Parse(args) != null;
        }
    }
}
