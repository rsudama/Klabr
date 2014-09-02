
using System.Windows;

namespace Gemcom.Klabr
{
    public partial class OptionsDialog
    {
        public OptionsDialog()
        {
            InitializeComponent();

            minItems.Value = Properties.Settings.Default.DiscussionFilter;
            pollingInterval.Value = Properties.Settings.Default.PollingInterval;
            notificationInterval.Value = Properties.Settings.Default.NotificationInterval;
            autoLoadTopics.IsChecked = Properties.Settings.Default.AutoLoadTopics;
            signature.Text = Properties.Settings.Default.Signature;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DiscussionFilter = (int)minItems.Value;
            Properties.Settings.Default.PollingInterval = (int)pollingInterval.Value;
            Properties.Settings.Default.NotificationInterval = (int)notificationInterval.Value;
            Properties.Settings.Default.AutoLoadTopics = (bool)autoLoadTopics.IsChecked;
            Properties.Settings.Default.Signature = signature.Text;
            Properties.Settings.Default.Save();

            DialogResult = true;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
