// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: http://www.viva64.com




namespace Hardcodet.Wpf.TaskbarNotification.Interop
{
    /// <summary>
    /// The state of the icon - can be set to
    /// hide the icon.
    /// </summary>
    public enum IconState
    {
        /// <summary>
        /// The icon is visible.
        /// </summary>
        Visible = 0x00,

        /// <summary>
        /// Hide the icon.
        /// </summary>
        Hidden = 0x01,

        // The icon is shared - currently not supported, thus commented out.
        //Shared = 0x02
    }
}