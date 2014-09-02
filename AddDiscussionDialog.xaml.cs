
using System.Windows;

namespace Gemcom.Klabr
{
    public partial class AddDiscussionDialog
    {
        public string Url = string.Empty;

        public AddDiscussionDialog()
        {
            InitializeComponent();
            discussionUrl.Focus();
        }

        private void submitButton_Click(object sender, RoutedEventArgs e)
        {
            Url = discussionUrl.Text;
            DialogResult = true;
        }
    }
}
