using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace FFXIITataruHelper
{
    /// <summary>
    /// Interaction logic for AboutWin.xaml
    /// </summary>
    public partial class AboutWin : Window
    {
        public AboutWin()
        {
            InitializeComponent();
            string caption = "TataruHelper version: " + Convert.ToString(System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            TataruVersionBlock.Text = caption;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
