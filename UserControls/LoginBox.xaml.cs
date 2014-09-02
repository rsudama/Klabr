
using System.Windows;

namespace Gemcom.Klabr.UserControls
{
    public partial class LoginBox
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }

        public LoginBox()
        {
            InitializeComponent();
            username.Text = Username;
            password.Password = Password;
            domain.Text = Domain;
        }

        public LoginBox(string username, string domain) : this()
        {
            this.username.Text = username;
            this.domain.Text = domain;
        }

        private void loginButton_Click(object sender, RoutedEventArgs e)
        {
            Username = username.Text;
            Password = password.Password;
            Domain = domain.Text;
            DialogResult = true;
        }
    }
}
