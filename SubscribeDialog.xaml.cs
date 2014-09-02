
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr
{
    public partial class SubscribeDialog
    {
        // Discussion data to populate grid control
        private readonly ObservableCollection<SPDiscussion> _discussionCollection = new ObservableCollection<SPDiscussion>();
        // Status of column sorting
        private readonly IDictionary<ListView, GridViewColumnHeader> _sortColumns = new Dictionary<ListView, GridViewColumnHeader>();
        private readonly IDictionary<ListView, SortAdorner> _sortAdorners = new Dictionary<ListView, SortAdorner>();
        // Background worker to perform server interactions
        private readonly BackgroundWorker _refreshWorker = new BackgroundWorker();


        public ObservableCollection<SPDiscussion> SpDiscussionCollection
        {
            get { return _discussionCollection; }
        }

        public SubscribeDialog()
        {
            InitializeComponent();

            // Position the window based on last settings
            if (Properties.Settings.Default.SubscribeWindowLeft > -1)
                Left = Properties.Settings.Default.SubscribeWindowLeft;
            if (Properties.Settings.Default.SubscribeWindowTop > -1)
                Top = Properties.Settings.Default.SubscribeWindowTop;
            if (Properties.Settings.Default.SubscribeWindowHeight > -1)
                Height = Properties.Settings.Default.SubscribeWindowHeight;
            if (Properties.Settings.Default.SubscribeWindowWidth > -1)
                Width = Properties.Settings.Default.SubscribeWindowWidth;

            // Hide status and progress indicators until needed
            statusLabel.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;

            _refreshWorker.WorkerReportsProgress = true;
            _refreshWorker.WorkerSupportsCancellation = true;
            _refreshWorker.DoWork += RefreshWorkerDoWork;
            _refreshWorker.ProgressChanged += RefreshWorkerProgressChanged;
            _refreshWorker.RunWorkerCompleted += RefreshWorkerRunWorkerCompleted;

            // Load existing discussions and subscription data
            LoadDiscussions();
            LoadSubscriptions();

            if (_discussionCollection.Count == 0)
                _refreshWorker.RunWorkerAsync();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Shut down the worker thread if it is currently active
            if (_refreshWorker.IsBusy)
                _refreshWorker.CancelAsync();
        }

        private void RefreshWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                // Indicate operation in progress...
                _refreshWorker.ReportProgress(2, new WorkerData("Connecting to SharePoint..."));

                List<PublishedDiscussion> publishedDiscussions = MainWindow.Portal.GetPublishedDiscussions(
                        Properties.Settings.Default.PublishedDiscussionsUrl,
                        Properties.Settings.Default.PublishedDiscussionsListName);

                foreach (PublishedDiscussion publishedDiscussion in publishedDiscussions)
                {
                    SPDiscussion discussion = new SPDiscussion();
                    discussion.Title = publishedDiscussion.Title;
                    discussion.Description = publishedDiscussion.Description;
                    discussion.Moderators = publishedDiscussion.Moderators;
                    discussion.Communities = publishedDiscussion.Communities;
                    discussion.Url = publishedDiscussion.Url;
                    try
                    {
                        discussion = MainWindow.Portal.UpdateDiscussion(discussion);

                        // Filter out discussion boards that fall below the legal limit
                        if (discussion.ItemCount >= Properties.Settings.Default.DiscussionFilter)
                        {
                            _refreshWorker.ReportProgress(2, new WorkerData("Collecting discussion boards...", discussion));
                        }
                    }
                    catch (Exception) { /*ignored*/ }
                }
            }
            catch (Exception) { /*ignored*/ }

            _refreshWorker.ReportProgress(0, new WorkerData("Discussion boards loaded."));
        }

        private void RefreshWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // This is the trigger for cancellation of the synchronization process
            if (((BackgroundWorker)sender).CancellationPending)
            {
                return;
            }

            WorkerData workerData = (WorkerData)e.UserState;

            if (workerData.Status != null)
            {
                statusLabel.Content = (workerData.Status);
                statusLabel.Visibility = Visibility.Visible;
            }

            if (workerData.Discussion != null)
            {
                if (MainWindow.SubscribedDiscussions.FindDiscussion(workerData.Discussion.UniqueID) != null)
                    workerData.Discussion.IsSubscribed = true;
                _discussionCollection.Add(workerData.Discussion);
            }

            switch (e.ProgressPercentage)
            {
                case 0: progressBar.Visibility = Visibility.Hidden; 
                    break;

                case 1: progressBar.Value = 0; 
                    progressBar.Visibility = Visibility.Visible; 
                    break;

                case 2:
                {
                    // Update progress indicators
                    if (progressBar.Value == progressBar.Maximum)
                        progressBar.Value = 0;
                    else
                        progressBar.Value += 10;

                    progressBar.Visibility = Visibility.Visible;
                    break;
                }
            }
        }

        //
        // Invoked when background worker has completed its work.
        //
        private void RefreshWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                // If the user cancelled the synchronization operation, let them
                // know it was terminated
                if (e.Cancelled)
                {
                    MessageBox.Show("Refresh operation aborted.", Application.Current.MainWindow.Title,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Report any exceptions that might have occurred as errors
                else if (e.Error != null)
                {
                    string message = e.Error.Message;

                    if (e.Error.InnerException != null)
                        message += " : " + e.Error.InnerException.Message;
#if DEBUG
                    // When debugging, display the stack trace for exceptions
                    message += e.Error.StackTrace;
                    MessageBox.Show(message, Application.Current.MainWindow.Title, MessageBoxButton.OK, MessageBoxImage.Error);
#endif
                }
            }
            catch (Exception exception)
            {
                MainWindow.HandleException(exception);
            }
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            SaveDiscussions();

            foreach (SPDiscussion discussion in _discussionCollection)
            {
                if (discussion.IsSubscribed)
                    MainWindow.SubscribedDiscussions.Add(discussion, false);
                else
                {
                    if (MainWindow.SubscribedDiscussions.FindDiscussion(discussion.UniqueID) != null)
                        MainWindow.SubscribedDiscussions.Remove(discussion);
                }
            }

            MainWindow.SubscribedDiscussions.Save();

            Properties.Settings.Default.SubscribeWindowLeft = Left;
            Properties.Settings.Default.SubscribeWindowTop = Top;
            Properties.Settings.Default.SubscribeWindowWidth = Width;
            Properties.Settings.Default.SubscribeWindowHeight = Height;
            Properties.Settings.Default.Save();

            DialogResult = true;
        }

        /// <summary>
        /// When the list view is clicked, check to see if the hit target is a column header.
        /// If so, sort the grid based on the contents of the selected column.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDiscussionListViewClick(object sender, RoutedEventArgs e)
        {
            ListView listView = (ListView)sender;

            if (!(e.OriginalSource is GridViewColumnHeader))
                return;
            
            GridViewColumnHeader column = (GridViewColumnHeader)e.OriginalSource;
            string field;

            // The checkbox column isn't bound, so we have to special-case the field name
            if (((GridViewColumnHeader)e.OriginalSource).Column.DisplayMemberBinding == null)
                field = "IsSubscribed";
            else
                field = ((Binding)((GridViewColumnHeader)e.OriginalSource).Column.DisplayMemberBinding).Path.Path;

            if (!_sortColumns.ContainsKey(listView))
                _sortColumns.Add(listView, null);

            if (!_sortAdorners.ContainsKey(listView))
                _sortAdorners.Add(listView, null);

            if (_sortColumns[listView] != null)
            {
                AdornerLayer.GetAdornerLayer(_sortColumns[listView]).Remove(_sortAdorners[listView]);
                listView.Items.SortDescriptions.Clear();
            }

            ListSortDirection sortDirection;
            if (field.Equals("IsSubscribed"))
            {
                // All this nonsense just to get the checkbox column to sort in the opposite order
                // of the text columns, so the subscribed items come first in the sort order
                sortDirection = ListSortDirection.Descending;
                ListSortDirection adornerDirection = ListSortDirection.Ascending;
                if (_sortColumns[listView] == column && _sortAdorners[listView].SortDirection == adornerDirection)
                {
                    sortDirection = ListSortDirection.Ascending;
                    adornerDirection = ListSortDirection.Descending;
                }

                _sortColumns[listView] = column;
                _sortAdorners[listView] = new SortAdorner(_sortColumns[listView], adornerDirection);
                AdornerLayer.GetAdornerLayer(_sortColumns[listView]).Add(_sortAdorners[listView]);
                listView.Items.SortDescriptions.Add(new SortDescription(field, sortDirection));
            }
            else
            {
                sortDirection = ListSortDirection.Ascending;
                if (_sortColumns[listView] == column && _sortAdorners[listView].SortDirection == sortDirection)
                    sortDirection = ListSortDirection.Descending;

                _sortColumns[listView] = column;
                _sortAdorners[listView] = new SortAdorner(_sortColumns[listView], sortDirection);
                AdornerLayer.GetAdornerLayer(_sortColumns[listView]).Add(_sortAdorners[listView]);
                listView.Items.SortDescriptions.Add(new SortDescription(field, sortDirection));
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void LoadDiscussions()
        {
            FileStream reader = null;

            try
            {
                _discussionCollection.Clear();

                // Read the stored discussion data from file
                if (File.Exists(MainWindow.DiscussionsFile))
                {
                    reader = new FileStream(MainWindow.DiscussionsFile, FileMode.Open);
                    List<SPDiscussion> discussions = 
                        (List<SPDiscussion>)MainWindow.DiscussionSerializer.Deserialize(reader);
                    reader.Close();

                    foreach (SPDiscussion discussion in discussions)
                        _discussionCollection.Add(discussion);
                }
            }
            catch (Exception exception)
            {
                if (reader != null)
                    reader.Close();

                MainWindow.HandleException(exception);
            }
        }

        private void LoadSubscriptions()
        {
            foreach (SPDiscussion discussion in _discussionCollection)
            {
                if (MainWindow.SubscribedDiscussions.FindDiscussion(discussion.UniqueID) != null)
                    discussion.IsSubscribed = true;
            }
        }

        private void SaveDiscussions()
        {
            Stream writer = null;

            try
            {
                writer = new FileStream(MainWindow.DiscussionsFile, FileMode.Create);
                MainWindow.DiscussionSerializer.Serialize(writer, new List<SPDiscussion>(_discussionCollection));
                writer.Close();

            }
            catch (Exception exception)
            {
                if (writer != null)
                    writer.Close();

                MainWindow.HandleException(exception);
            }
        }
        
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // TODO: cleanup code

            if (Properties.Settings.Default.DiscussionViewColumnWidth1 > 0)
                ((GridView)discussionListView.View).Columns[1].Width = Properties.Settings.Default.DiscussionViewColumnWidth1;
            if (Properties.Settings.Default.DiscussionViewColumnWidth2 > 0)
                ((GridView)discussionListView.View).Columns[2].Width = Properties.Settings.Default.DiscussionViewColumnWidth2;
            if (Properties.Settings.Default.DiscussionViewColumnWidth3 > 0)
                ((GridView)discussionListView.View).Columns[3].Width = Properties.Settings.Default.DiscussionViewColumnWidth3;
            if (Properties.Settings.Default.DiscussionViewColumnWidth4 > 0)
                ((GridView)discussionListView.View).Columns[4].Width = Properties.Settings.Default.DiscussionViewColumnWidth4;
            if (Properties.Settings.Default.DiscussionViewColumnWidth5 > 0)
                ((GridView)discussionListView.View).Columns[5].Width = Properties.Settings.Default.DiscussionViewColumnWidth5;
            if (Properties.Settings.Default.DiscussionViewColumnWidth6 > 0)
                ((GridView)discussionListView.View).Columns[6].Width = Properties.Settings.Default.DiscussionViewColumnWidth6;
            if (Properties.Settings.Default.DiscussionViewColumnWidth7 > 0)
                ((GridView)discussionListView.View).Columns[7].Width = Properties.Settings.Default.DiscussionViewColumnWidth7;
            if (Properties.Settings.Default.DiscussionViewColumnWidth8 > 0)
                ((GridView)discussionListView.View).Columns[8].Width = Properties.Settings.Default.DiscussionViewColumnWidth8;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            // TODO: cleanup code

            Properties.Settings.Default.DiscussionViewColumnWidth1 =
                ((GridView)discussionListView.View).Columns[1].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth2 =
                ((GridView)discussionListView.View).Columns[2].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth3 =
                ((GridView)discussionListView.View).Columns[3].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth4 =
                ((GridView)discussionListView.View).Columns[4].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth5 =
                ((GridView)discussionListView.View).Columns[5].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth6 =
                ((GridView)discussionListView.View).Columns[6].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth7 =
                ((GridView)discussionListView.View).Columns[7].ActualWidth;
            Properties.Settings.Default.DiscussionViewColumnWidth8 =
                ((GridView)discussionListView.View).Columns[8].ActualWidth;
            Properties.Settings.Default.Save();
        }

        ///
        /// Data structure to pass from worker thread to progess update handler
        ///
        private class WorkerData
        {
            public string Status;
            public SPDiscussion Discussion;

            public WorkerData(string status)
            {
                Status = status;
            }

            //public WorkerData(string status, string site)
            //{
            //    Status = status;
            //    Site = site;
            //}

            //public WorkerData(SPDiscussion discussion)
            //{
            //    Discussion = discussion;
            //}

            public WorkerData(string status, SPDiscussion discussion)
            {
                Status = status;
                Discussion = discussion;
            }
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    OnRefresh(sender, e);
                    break;
            }
        }

        private void OnRefresh(object sender, RoutedEventArgs e)
        {
            _discussionCollection.Clear();

            if (_refreshWorker.IsBusy)
            {
                _refreshWorker.CancelAsync();

                while (_refreshWorker.IsBusy)
                {
                }
            }

            _refreshWorker.RunWorkerAsync();
        }
    }
}
