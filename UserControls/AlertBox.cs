using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;


namespace Gemcom.Klabr.Controls
{
    public class AlertBox : Window
    {
        private DoubleAnimation _fadeInAnimation;
        private DoubleAnimation _fadeOutAnimation;
        private DispatcherTimer _activeTimer;


        public AlertBox(string message)
        {
            Visibility = Visibility.Visible;

            Width = 300;
            Height = 75;

            Topmost = true;
            AllowsTransparency = true;
            Opacity = 0.8;
            ShowInTaskbar = false;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            BorderThickness = new Thickness(2);
            BorderBrush = Brushes.DarkGray;
            Background = Brushes.LightGray;

            Grid grid = new Grid();
            Grid headerBar = new Grid
            {
                Height = 20,
                Background = Brushes.DarkGray,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
            };
            headerBar.Children.Add
            (
                new Label
                {
                    Content = "Klabr",
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                }
            );
            headerBar.Children.Add
            (
                new Button
                {
                    Content = "X",
                    Background = Brushes.LightGray,
                    HorizontalAlignment = HorizontalAlignment.Right,
                }
            );
            grid.Children.Add(headerBar);

            TextBlock text = new TextBlock
            {
                Text = message,
                FontSize = 12,
                Padding = new Thickness(5),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
            };
            text.MouseDown += new MouseButtonEventHandler(OnClicked);
            grid.Children.Add(text);
            this.Content = grid;

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

            Loaded += new RoutedEventHandler(OnLoaded);
            Closing += new CancelEventHandler(OnClosing);
        }

        // In the Loaded-event handler we need to figure out where to place the alert window.
        // Currently they are all placed in the bottom right corner, and can not take into
        // account other currently open alert windows.
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Figure out where to place the window based on the current screen resolution
            Rect workAreaRectangle = System.Windows.SystemParameters.WorkArea;
            Left = workAreaRectangle.Right - Width - BorderThickness.Right;
            Top = workAreaRectangle.Bottom - Height - BorderThickness.Bottom;

            _fadeInAnimation.Completed += new EventHandler(FadeInAnimation_Completed);

            // Start the fade in animation
            BeginAnimation(OpacityProperty, _fadeInAnimation);
        }

        // When the fade in animation is completed, start another timer that fires an event when the
        // window has been visible for 10 seconds
        void FadeInAnimation_Completed(object sender, EventArgs e)
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

            BeginAnimation(OpacityProperty, _fadeOutAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        // If the user clicks the close button, stop the timer that counts how long the alert
        // window has been open, and start the fading out of the window.
        void OnClosing(object sender, CancelEventArgs e)
        {
            if (_activeTimer != null)
                _activeTimer.Stop();
            //FadeOut();
        }

        void OnClicked(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}
