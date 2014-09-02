using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.ComponentModel;


namespace Gemcom.Klabr.Controls
{
    /// <summary>
    /// Interaction logic for SimpleAlert.xaml
    /// </summary>
    public partial class SimpleAlert : DesktopAlertBase
    {
        public static DependencyProperty MessageProperty = DependencyProperty.Register("Message", typeof(string), typeof(SimpleAlert));

        [Bindable(true)]
        public string Message
        {
            get { return (string)GetValue(MessageProperty); }
            set { SetValue(MessageProperty, value); }
        }

        public SimpleAlert()
        {
            InitializeComponent();
        }
    }
}
