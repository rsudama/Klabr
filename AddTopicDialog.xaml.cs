
using System;
using System.Windows;
using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr
{
    public partial class AddTopicDialog
    {
        private readonly SPDiscussion _discussion;

        public AddTopicDialog()
        {
            InitializeComponent();
            topicTitle.Focus();
        }

        public AddTopicDialog(SPDiscussion discussion)
            : this()
        {
            _discussion = discussion;
            Title = "Klabr - Add Topic : " + discussion.Title;
        }

        public Guid UniqueID { get; private set; }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: use validation and set set error icon
            if (topicTitle.Text.Length == 0)
            {
                const string message = "Topic is required";
                MessageBox.Show(message);
                return;
            }

            try
            {
                string body = editorTextBox.Text;
                string signature = Properties.Settings.Default.Signature;

                if (signature.Length > 0)
                    body = body.Replace("</BODY></HTML>", "<P>" + signature + "</P></BODY></HTML>");

                UniqueID = MainWindow.Portal.DiscussionAddTopic(_discussion, topicTitle.Text, body);
                DialogResult = true;
                return;
            }
            catch (Exception exception)
            {
                MainWindow.HandleException(exception);
            }

            DialogResult = false;
        }
    }
}
