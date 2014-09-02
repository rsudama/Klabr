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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls.Primitives;


namespace Gemcom.Klabr.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:da="clr-namespace:DesktopAlert"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:da="clr-namespace:Gemcom.Klabr.Controls;assembly=DesktopAlert"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <Gemcom.Klabr.Controls:DesktopAlertBase/>
    ///
    /// </summary>
    /// 
    public abstract class DesktopAlertBase : Window
    {
        private DoubleAnimation _fadeInAnimation;
        private DoubleAnimation _fadeOutAnimation;
        private DispatcherTimer _activeTimer;

        static DesktopAlertBase()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DesktopAlertBase), new FrameworkPropertyMetadata(typeof(DesktopAlertBase)));
        }

        // We need to add a public constructor so that we can set up our
        // new window properly
        public DesktopAlertBase()
        {
            Visibility = Visibility.Visible;

            // Let's set up a dummy window size
            Width = 350;
            Height = 75;

            // Set some default properties for the alerts.
            // These can be changed by the derived alerts.
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
            AllowsTransparency = true;
            Opacity = 0.8;
            BorderThickness = new Thickness(1);
            BorderBrush = Brushes.Black;
            Background = Brushes.White;

            // Set up the fade in and fade out animations
            _fadeInAnimation = new DoubleAnimation();
            _fadeInAnimation.From = 0;
            _fadeInAnimation.To = 0.8;
            _fadeInAnimation.Duration = new Duration(TimeSpan.Parse("0:0:1.5"));

            // For the fade out we omit the from, so that it can be smoothly initiated
            // from a fade in that gets interrupted when the user wants to close the window
            _fadeOutAnimation = new DoubleAnimation();
            _fadeOutAnimation.To = 0;
            _fadeOutAnimation.Duration = new Duration(TimeSpan.Parse("0:0:1.5"));

            Loaded += new RoutedEventHandler(DesktopAlertBase_Loaded);
        }

        // When the template is applied to the control, look for a button called "PART_CloseButton".
        // If such a button exists, hook up an event handler so that the alert can be closed when
        // the user clicks the button.
        public override void OnApplyTemplate()
        {
            ButtonBase closeButton = Template.FindName("PART_CloseButton", this) as ButtonBase;
            if (closeButton != null)
                closeButton.Click += new RoutedEventHandler(closeButton_Click);
        }

        // In the Loaded-event handler we need to figure out where to place the alert window.
        // Currently they are all placed in the bottom right corner, and can not take into
        // account other currently open alert windows.
        void DesktopAlertBase_Loaded(object sender, RoutedEventArgs e)
        {
            // Figure out where to place the window based on the current screen resolution
            Rect workAreaRectangle = System.Windows.SystemParameters.WorkArea;
            Left = workAreaRectangle.Right - Width - BorderThickness.Right;
            Top = workAreaRectangle.Bottom - Height - BorderThickness.Bottom;

            _fadeInAnimation.Completed += new EventHandler(_fadeInAnimation_Completed);

            // Start the fade in animation
            BeginAnimation(DesktopAlertBase.OpacityProperty, _fadeInAnimation);
        }

        // When the fade in animation is completed, start another timer that fires an event when the
        // window has been visible for 10 seconds
        void _fadeInAnimation_Completed(object sender, EventArgs e)
        {
            _activeTimer = new DispatcherTimer();
            _activeTimer.Interval = TimeSpan.Parse("0:0:10");

            // Attach an anonymous method to the timer so that we can start fading out the alert
            // when the timer is done.
            _activeTimer.Tick += delegate(object obj, EventArgs ea) { FadeOut(); };

            _activeTimer.Start();
        }

        // Set up the fade out animation, and hook up an event handler to fire when it is completed.
        private void FadeOut()
        {
            // Attach an anonymous method to the Completed-event of the fade out animation
            // so that we can close the alert window when the animation is done.
            _fadeOutAnimation.Completed += delegate(object sender, EventArgs e) { Close(); };

            BeginAnimation(DesktopAlertBase.OpacityProperty, _fadeOutAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        // If the user clicks the close button, stop the timer that counts how long the alert
        // window has been open, and start the fading out of the window.
        void closeButton_Click(object sender, RoutedEventArgs e)
        {
            _activeTimer.Stop();
            FadeOut();
        }
    }
}
