
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr.UserControls
{
    public partial class AlertBox
    {
        private readonly SPDiscussion _discussion;
        private readonly SPTopic _topic;
        private readonly DoubleAnimation _fadeInAnimation;
        private readonly DoubleAnimation _fadeOutAnimation;
        private DispatcherTimer _activeTimer;

        public AlertBox(string title, List<SPDiscussion> subscribedDiscussions, SPDiscussion discussion, SPTopic topic)
        {
            InitializeComponent();

            _discussion = discussion;
            _topic = topic;

            header.Content = title;
            //text.Text = "Discussion '" + discussion.Title + "', Topic '" + _topic.Title + "' has been modified";
            text.Text = string.Format("Discussion '{0}', Topic '{1}' has been modified", discussion.Title, _topic.Title);

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
        }

        // In the Loaded-event handler we need to figure out where to place the alert window.
        // Currently they are all placed in the bottom right corner, and can not take into
        // account other currently open alert windows.
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Figure out where to place the window based on the current screen resolution
            Rect workAreaRectangle = SystemParameters.WorkArea;
            Left = workAreaRectangle.Right - Width - BorderThickness.Right;
            Top = workAreaRectangle.Bottom - Height - BorderThickness.Bottom;

            _fadeInAnimation.Completed += FadeInAnimation_Completed;

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
            _activeTimer.Tick += delegate { FadeOut(); };
            _activeTimer.Start();
        }

        // Set up the fade out animation, and hook up an event handler to fire when it is completed.
        private void FadeOut()
        {
            // Attach an anonymous method to the Completed-event of the fade out animation
            // so that we can close the alert window when the animation is done.
            _fadeOutAnimation.Completed += delegate { Close(); };

            BeginAnimation(OpacityProperty, _fadeOutAnimation, HandoffBehavior.SnapshotAndReplace);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExplicitClose();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ExplicitClose();
        }

        private void ExplicitClose()
        {
            // User has made explicit action to close the notification window
            _discussion.LastViewed = DateTime.Now;
            _topic.LastViewed = DateTime.Now;
            MainWindow.SubscribedDiscussions.SaveDiscussion(_discussion);
            Close();
        }

        void OnClosing(object sender, CancelEventArgs e)
        {
            // Stop the timer that counts how long the alert window has been open
            if (_activeTimer != null)
                _activeTimer.Stop();
        }
    }
}
