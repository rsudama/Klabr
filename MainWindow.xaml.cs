using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web.Services.Protocols;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using Gemcom.Klabr.Properties;
using Gemcom.Klabr.SPWebLib;
using Gemcom.Klabr.UserControls;
using Microsoft.Windows.Controls.Ribbon;


namespace Gemcom.Klabr
{
    public partial class MainWindow
    {
        private static readonly string AppDataFolder = ApplicationExtensions.GetLocalUserAppDataPath();
        public static readonly string DiscussionsFile = AppDataFolder + "\\" + Settings.Default.DiscussionsFile;
        public static readonly string SubscribedDiscussionsFile = AppDataFolder + "\\" + Settings.Default.SubscribedDiscussionsFile;
        private static readonly string LogFile = AppDataFolder + "\\" + Settings.Default.LogFile;
        private const string HelpFile = "KlabrUsersGuide.pdf";
        public static readonly XmlSerializer DiscussionSerializer = new XmlSerializer(typeof(List<SPDiscussion>));
        public static Portal Portal;
        public static readonly SubscribedDiscussions SubscribedDiscussions = new SubscribedDiscussions(SubscribedDiscussionsFile);
        
        private static StreamWriter _logWriter;
        private readonly DispatcherTimer _pollingTimer = new DispatcherTimer();
        private readonly DispatcherTimer _notificationTimer = new DispatcherTimer();
        private AlertBox _alertBox;
#if STARTUP_LOAD
        private readonly BackgroundWorker _startupWorker = new BackgroundWorker();
        private Cursor _cursor;
#endif


        public MainWindow()
        {
            Resources.MergedDictionaries.Add(PopularApplicationSkins.Office2007Black);

            try
            {
                InitializeComponent();

#if TRAY
                // Make into tray appplication
                System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();
                notifyIcon.Text = "Klabr";
                notifyIcon.Icon = new Icon("klabr_icon_48.ico");
                notifyIcon.Visible = true;
                notifyIcon.DoubleClick += OnDoubleClickTrayIcon;
#endif

                // Set the window size and position from the saved settings
                if (Settings.Default.MainWindowLeft > -1)
                    Left = Settings.Default.MainWindowLeft;
                if (Settings.Default.MainWindowTop > -1)
                    Top = Settings.Default.MainWindowTop;
                if (Settings.Default.MainWindowHeight > -1)
                    Height = Settings.Default.MainWindowHeight;
                if (Settings.Default.MainWindowWidth > -1)
                    Width = Settings.Default.MainWindowWidth;

                if (!Directory.Exists(AppDataFolder))
                    Directory.CreateDirectory(AppDataFolder);

                _logWriter = new StreamWriter(LogFile);
                Log(DateTime.Now.ToString());

                Portal = new Portal(_logWriter);

                Loaded += OnWindowLoaded;
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }

        #region Tray Events

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        private void OnDoubleClickTrayIcon(object sender, EventArgs args)
        {
            Show();
            WindowState = WindowState.Normal;
        }

        #endregion

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
#if STARTUP_LOAD
            _startupWorker.WorkerReportsProgress = false;
            _startupWorker.WorkerSupportsCancellation = true;
            _startupWorker.DoWork += StartupWorkerDoWork;
            _startupWorker.RunWorkerCompleted += StartupWorkerRunWorkerCompleted;

            _cursor = Cursor;
            Cursor = Cursors.AppStarting;
            _startupWorker.RunWorkerAsync();
#else
            UpdateDiscussionTabs();
#endif

            if (Settings.Default.PollingInterval > 0)
                StartPolling();

            if (Settings.Default.NotificationInterval > 0)
                StartNotification();
        }

#if STARTUP_LOAD
        private static void StartupWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                Portal.SiteGetSites(Properties.Settings.Default.PortalUrl, false);
            }
            catch (Exception exception)
            {
                e.Result = exception;
            }
        }

        private void StartupWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                Cursor = _cursor;

                // If the user cancelled the synchronization operation, let them
                // know it was terminated
                if (e.Cancelled)
                {
                    const string message = "Startup operation aborted.";
                    MessageBox.Show(message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                    // Report any exceptions that might have occurred as errors
                else if (e.Error != null || e.Result != null)
                {
                    string message = "Undefined error";

                    if (e.Error != null)
                    {
                        message = e.Error.Message;
                        if (e.Error.InnerException != null)
                            message += " : " + e.Error.InnerException.Message;
                    }
                    else if (e.Result != null)
                    {
                        Exception exception = (Exception) e.Result;
                        message = exception.Message;

                        if (exception.InnerException != null)
                            message += " : " + exception.InnerException.Message;
                    }
                    MessageBox.Show(message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }

                UpdateDiscussionTabs();
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }
#endif

        private void StartPolling()
        {
            // Stop the timer if it is currently running
            _pollingTimer.Stop();
            _pollingTimer.Interval = TimeSpan.FromSeconds(Settings.Default.PollingInterval);
            _pollingTimer.Tick += OnPollingTimerTick;
            _pollingTimer.Start();
        }

        private void OnPollingTimerTick(object sender, EventArgs e)
        {
            try
            {
                foreach (TabItem tab in discussionTabControl.Items)
                {
                    tab.FontWeight = FontWeights.Normal;

                    if (tab.Tag == null)
                        continue;

                    Guid discussionId = (Guid) tab.Tag;

                    SPDiscussion discussion = SubscribedDiscussions.FindDiscussion(discussionId);

                    if (discussion != null)
                    {
                        // Wish we could just depend on the discussion's "Modified" attribute,
                        // but it doesn't seem to get updated when topics change
                        List<SPTopic> topics = Portal.DiscussionGetTopics(discussion);
                        tab.Header = discussion.Title + " (" + topics.Count + ")";

                        foreach (SPTopic topic in topics)
                        {
                            SPTopic subscribedTopic = SubscribedDiscussions.FindTopic(discussion.UniqueID, topic.UniqueID);

                            if (subscribedTopic == null ||
                                topic.LastModified > subscribedTopic.LastViewed)
                            {
                                // Highlight the tab if it's been modified since the last time it was viewed
                                tab.FontWeight = FontWeights.Bold;
                                SubscribedDiscussions.SaveDiscussion(discussion);
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                HandleException(exception, false);
            }
        }

        private void StartNotification()
        {
            // Stop the timer if it is currently running
            _notificationTimer.Stop();

            // Tick the clock once before we start the first interval timer
            //OnNotificationTimerTick(this, new RoutedEventArgs());

            // Start the interval timer
            _notificationTimer.Interval = TimeSpan.FromMinutes(Settings.Default.NotificationInterval);
            _notificationTimer.Tick += OnNotificationTimerTick;
            _notificationTimer.Start();
        }

        private void OnNotificationTimerTick(object sender, EventArgs e)
        {
            try
            {
                if (_alertBox == null || !_alertBox.IsActive)
                {
                    foreach (SPDiscussion discussion in SubscribedDiscussions)
                    {
                        foreach (SPTopic topic in discussion.Topics)
                        {
                            // Post notification for all subscribed topics that have been modified
                            // since the last time they were viewed
                            if (topic.IsSubscribed &&
                                topic.LastModified <= DateTime.Now &&
                                topic.LastModified > topic.LastViewed)
                            {
                                _alertBox = new AlertBox(Application.Current.MainWindow.Title,
                                                         SubscribedDiscussions, discussion, topic);
                                _alertBox.Show();
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                HandleException(exception, false);
            }
        }

        /// <summary>
        /// Create the set of tabs, one for each subscribed discussion board.
        /// </summary>
        private void UpdateDiscussionTabs()
        {
            // Disable tab selection while we're updating the tabs
            discussionTabControl.SelectionChanged -= OnDiscussionTabControlSelectionChanged;

            discussionTabControl.Items.Clear();

            if (SubscribedDiscussions.Count == 0)
            {
                // Indicate that there is nothing to put on the tabs yet...
                Label label = new Label();
                label.HorizontalAlignment = HorizontalAlignment.Center;
                label.VerticalAlignment = VerticalAlignment.Center;
                label.FontSize = 14.0;
                label.FontWeight = FontWeights.Bold;
                label.Content = "You are not subscribed to any discussions (use 'Manage Subscriptions').";

                TabItem tabItem = new TabItem();
                tabItem.Content = label;

                discussionTabControl.Items.Add(tabItem);
                discussionTabControl.SelectedIndex = 0;
                return;
            }

            // Create a tab for each subscribed discussion
            foreach (SPDiscussion discussion in SubscribedDiscussions)
            {
                TabItem newTab = new TabItem();
                try
                {
                    newTab.Header = discussion.Title;
                    newTab.ToolTip = discussion.Description;
                    newTab.Tag = discussion.UniqueID;

                    // Highlight the tab if it has items more recent than the last time it was viewed
                    if (discussion.LastModified > discussion.LastViewed)
                        newTab.FontWeight = FontWeights.Bold;
                }
                catch (Exception)
                {
                    MessageBox.Show("Swallowed exception. Notify developer.");
                }

                // Add the tab
                discussionTabControl.Items.Add(newTab);
            }

            if (Settings.Default.TopicViewSelectedDiscussion != Guid.Empty)
            {
                foreach (TabItem tab in discussionTabControl.Items)
                {
                    if (((Guid)tab.Tag).Equals(Settings.Default.TopicViewSelectedDiscussion))
                    {
                         discussionTabControl.SelectedItem = tab;
                    }
                }
                if (discussionTabControl.SelectedItem == null && discussionTabControl.Items.Count > 0)
                    discussionTabControl.SelectedIndex = 0;                
            }

            if (discussionTabControl.SelectedItem != null)
            {
                if (Properties.Settings.Default.AutoLoadTopics ||
                    MessageBox.Show("Do you want to load topics for the active discussion now?",
                    this.Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    UpdateDiscussionTab((TabItem)discussionTabControl.SelectedItem);
                }
            }

            // Re-enable tab selection
            discussionTabControl.SelectionChanged += OnDiscussionTabControlSelectionChanged;
        }

        /// <summary>
        /// Update the contents of the selected discussion tab.
        /// </summary>
        /// <param name="discussionTab"></param>
        private void UpdateDiscussionTab(TabItem discussionTab)
        {
            Cursor cursor = Cursor;
            Cursor = Cursors.AppStarting;

            SPDiscussion discussion = SubscribedDiscussions.FindDiscussion((Guid)discussionTab.Tag);

            try
            {
                // Save the subscribed topics
                List<SPTopic> oldTopics = discussion.Topics;
                discussion.Topics = Portal.DiscussionGetTopics(discussion);

                foreach (SPTopic newTopic in discussion.Topics)
                {
                    SPTopic topic = newTopic;
                    SPTopic oldTopic = oldTopics.Find(t => t.UniqueID == topic.UniqueID);

                    if (oldTopic == null) 
                        continue;

                    newTopic.IsSubscribed = oldTopic.IsSubscribed;
                    newTopic.LastViewed = oldTopic.LastViewed;
                }

                SubscribedDiscussions.SaveDiscussion(discussion);

                // If any topic has been modified since it was last viewed, highlight the discussion tab
                discussionTab.FontWeight = FontWeights.Normal;

                foreach (SPTopic topic in discussion.Topics)
                {
                    if (topic.LastModified <= topic.LastViewed) 
                        continue;

                    discussionTab.FontWeight = FontWeights.Bold;
                    break;
                }
                discussionTab.Header = discussion.Title + " (" + discussion.Topics.Count + ")";
            }
            catch (Exception exception)
            {
                string message;
                if (exception is SoapException)
                    message = ((SoapException)exception).Detail.InnerText;
                else
                    message = exception.Message;

                discussionTab.Content = new Label
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 14.0,
                    FontWeight = FontWeights.Bold,
                    Content = "Unable to load topics for this discussion: " + message,
                };
            }

            Cursor = cursor;

            if (discussion.Topics == null)
                return;

            TopicView topicView = new TopicView(discussion);
            topicView.HorizontalAlignment = HorizontalAlignment.Stretch;
            topicView.VerticalAlignment = VerticalAlignment.Stretch;
            topicView.Width = double.NaN;
            topicView.Height = double.NaN;

            discussionTab.Content = topicView;
        }

        private void OnHelp(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                    (Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/" + HelpFile);

                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }

        private void OnDiscussionTabControlSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (discussionTabControl.SelectedItem == null ||
                ((TabItem)discussionTabControl.SelectedItem).Tag == null)
                    return;

            Settings.Default.TopicViewSelectedDiscussion =
                (Guid)((TabItem)discussionTabControl.SelectedItem).Tag;
            UpdateDiscussionTab((TabItem)discussionTabControl.SelectedItem);
        }

        private void OnCanExecute(object target, CanExecuteRoutedEventArgs args)
        {
            if ((args.OriginalSource == RemoveSubscriptionButton ||
                 args.OriginalSource == AddTopicButton)
                && discussionTabControl.SelectedItem == null)
                args.CanExecute = false;
            else
                args.CanExecute = true;
        }

        private void OnManageSubscriptions(object sender, ExecutedRoutedEventArgs e)
        {
            Cursor cursor = Cursor;
            Cursor = Cursors.AppStarting;

            try
            {
                SubscribeDialog subscribeWindow = new SubscribeDialog();
                IsEnabled = false;

                if (subscribeWindow.ShowDialog() == true)
                    UpdateDiscussionTabs();

                IsEnabled = true;
                Cursor = cursor;
            }
            catch (Exception exception)
            {
                IsEnabled = true;
                Cursor = cursor;
                HandleException(exception);
            }
        }

        private void OnAddTopic(object sender, ExecutedRoutedEventArgs e)
        {
            if (discussionTabControl.SelectedItem == null)
                return;
            SPDiscussion discussion = SubscribedDiscussions.FindDiscussion((Guid)((TabItem)discussionTabControl.SelectedItem).Tag);

            AddTopicDialog addTopic = new AddTopicDialog(discussion);

            if (addTopic.ShowDialog() == true)
            {
                // Have to do this first so we can mark the new topic as read
                List<SPTopic> topics = Portal.DiscussionGetTopics(discussion);
                SPTopic topic = topics.Find(t => addTopic.UniqueID == t.UniqueID);
                topic.LastViewed = DateTime.Now;
                SubscribedDiscussions.Add(discussion);

                UpdateDiscussionTab((TabItem)discussionTabControl.SelectedItem);
            }
        }

        private void OnLogin(object sender, ExecutedRoutedEventArgs e)
        {
            // Get username, password and domain
            LoginBox login = new LoginBox(Settings.Default.DefaultUsername, Settings.Default.DefaultDomain);

            if (login.ShowDialog() == true)
            {
                Cursor cursor = Cursor;
                Cursor = Cursors.AppStarting;

                // Attempt to login to the portal
                try
                {
                    Portal.Login(Settings.Default.PortalUrl, login.Username, login.Password, login.Domain);
                    Settings.Default.DefaultUsername = login.Username;
                    Settings.Default.DefaultDomain = login.Domain;
                    Settings.Default.Save();
                    Cursor = cursor;
                }
                catch (Exception exception)
                {
                    Cursor = cursor;
                    HandleException(exception);
                }
            }
        }

        private void OnAbout(object sender, ExecutedRoutedEventArgs e)
        {
            AboutBox about = new AboutBox(this);
            about.ShowDialog();
        }

        private void OnOptions(object sender, ExecutedRoutedEventArgs e)
        {
            int existingNotificationInterval = Settings.Default.NotificationInterval;
            OptionsDialog options = new OptionsDialog();
            options.ShowDialog();

            // If the notification interval was changed, reset the timer
            if (Settings.Default.NotificationInterval != existingNotificationInterval)
            {
                StartNotification();
            }
        }

        private void OnCloseApplication(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            try
            {
                // Save the window configuration settings
                Settings.Default.MainWindowLeft = Left;
                Settings.Default.MainWindowTop = Top;
                Settings.Default.MainWindowWidth = Width;
                Settings.Default.MainWindowHeight = Height;
                Settings.Default.Save();

                Log(DateTime.Now.ToString());
                _logWriter.Close();

                e.Cancel = false;
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }

        public static void HandleException(Exception ex)
        {
            HandleException(ex, true);
        }

        private static void HandleException(Exception ex, Boolean showMessage)
        {
            try
            {
                string message;

                if (ex is SoapException)
                    message = ((SoapException)ex).Detail.InnerText;
                else
                    message = ex.Message;

                if (ex.InnerException != null)
                    message += " : " + ex.InnerException.Message;

                Log(message);

                if (showMessage)
                {
                    MessageBox.Show(message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (InvalidOperationException)
            {
                //MessageBox.Show("Swallowed exception. Notify developer.");
            }
            catch (Exception)
            {
                MessageBox.Show("Swallowed exception. Notify developer.");
            }
        }

        private static void Log(string message)
        {
            try
            {
                _logWriter.WriteLine(message);
                _logWriter.Flush();
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (discussionTabControl.SelectedItem == null)
                return;

            switch (e.Key)
            {
                case Key.F5:
                    UpdateDiscussionTab((TabItem)discussionTabControl.SelectedItem);
                    break;

                case Key.F6:
                    try
                    {
                        SPDiscussion discussion = SubscribedDiscussions.FindDiscussion((Guid)((TabItem)discussionTabControl.SelectedItem).Tag);
                        ProcessStartInfo startInfo = new ProcessStartInfo(discussion.Url);
                        startInfo.WindowStyle = ProcessWindowStyle.Normal;
                        Process.Start(startInfo);
                    }
                    catch (Exception exception)
                    {
                        HandleException(exception);
                    }
                    break;
            }
        }

        private void OnAddDiscussion(object sender, ExecutedRoutedEventArgs e)
        {
            Cursor cursor = Cursor;
            Cursor = Cursors.AppStarting;

            AddDiscussionDialog dialog = new AddDiscussionDialog();

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    SPDiscussion discussion = Portal.GetDiscussion(dialog.Url);
                    SubscribedDiscussions.Add(discussion);
                    UpdateDiscussionTabs();
                }
                catch (Exception exception)
                {
                    HandleException(exception);
                }
            }
            Cursor = cursor;
        }

        private void OnRemoveDiscussion(object sender, ExecutedRoutedEventArgs e)
        {
            if (discussionTabControl.SelectedItem == null)
                return;

            SPDiscussion discussion = SubscribedDiscussions.FindDiscussion((Guid) ((TabItem) discussionTabControl.SelectedItem).Tag);

            string message = "Remove subscription to discussion \"" + discussion.Title + "\"?";
            string title = Application.Current.MainWindow.Title;
            MessageBoxResult result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                SubscribedDiscussions.Remove(discussion);
                UpdateDiscussionTabs();
            }
            catch (Exception exception)
            {
                HandleException(exception);
            }
        }

        private void OnMarkAllRead(object sender, ExecutedRoutedEventArgs e)
        {
            if (discussionTabControl.SelectedItem == null)
                return;

            SPDiscussion discussion = SubscribedDiscussions.FindDiscussion((Guid)((TabItem)discussionTabControl.SelectedItem).Tag);

            foreach (SPTopic topic in discussion.Topics)
            {
                topic.LastViewed = DateTime.Now;
            }

            SubscribedDiscussions.Save();

            ((TabItem)discussionTabControl.SelectedItem).FontWeight = FontWeights.Normal;
        }
    }
}