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
    /// Interaction logic for ImageAlert.xaml
    /// </summary>
    public partial class ImageAlert : DesktopAlertBase
    {
        public static DependencyProperty UrlProperty = DependencyProperty.Register("Url", typeof(string), typeof(ImageAlert));

        [Bindable(true)]
        public string Url
        {
            get { return (string)GetValue(UrlProperty); }
            set { SetValue(UrlProperty, value); }
        }

        public ImageAlert()
        {
            InitializeComponent();
        }
    }
}
