
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Gemcom.Klabr.SPWebLib;

namespace Gemcom.Klabr
{
    public partial class TopicView
    {
        private readonly ObservableCollection<SPTopic> _topicCollection = new ObservableCollection<SPTopic>();
        private readonly IDictionary<ListView, GridViewColumnHeader> _sortColumns = new Dictionary<ListView, GridViewColumnHeader>();
        private readonly IDictionary<ListView, SortAdorner> _sortAdorners = new Dictionary<ListView, SortAdorner>();
        readonly SPDiscussion _discussion;

        public ObservableCollection<SPTopic> SpTopicCollection
        {
            get { return _topicCollection; }
        }

        public TopicView(SPDiscussion discussion)
        {
            InitializeComponent();

            _discussion = discussion;

            // Copy the results to our ObservableCollection and attach the 
            // PropertyChanged Event so we know when this has been updated
            foreach (SPTopic topic in discussion.Topics)
            {
                _topicCollection.Add(topic);
                topic.PropertyChanged += TopicChanged;
            }

            // Add this to handle double-click on topics if needed
            //topicListView.AddHandler(   
            //    Control.MouseDoubleClickEvent,   
            //    new RoutedEventHandler(topicListView_DoubleClick));   
        }
  
        private void TopicChanged(object sender, PropertyChangedEventArgs e)
        {
            MainWindow.SubscribedDiscussions.SaveDiscussion(_discussion);
        }

        private void OnClick(object sender, RoutedEventArgs e)
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

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (topicListView.SelectedItem == null)
                return;

            ReplyDialog replies = new ReplyDialog(_discussion, (SPTopic)topicListView.SelectedItem);
            // note that current event handler cannot complete until modal dialog closes!!
            replies.ShowDialog();
            ((SPTopic)topicListView.SelectedItem).LastViewed = DateTime.Now;

            // TODO: why is Handled set to true?
            //e.Handled = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // TODO: cleanup code with setting variable

            if (Properties.Settings.Default.TopicViewColumnWidth1 > 0)
                ((GridView)topicListView.View).Columns[1].Width = Properties.Settings.Default.TopicViewColumnWidth1;
            if (Properties.Settings.Default.TopicViewColumnWidth2 > 0)
                ((GridView)topicListView.View).Columns[2].Width = Properties.Settings.Default.TopicViewColumnWidth2;
            if (Properties.Settings.Default.TopicViewColumnWidth3 > 0)
                ((GridView)topicListView.View).Columns[3].Width = Properties.Settings.Default.TopicViewColumnWidth3;
            if (Properties.Settings.Default.TopicViewColumnWidth4 > 0)
                ((GridView)topicListView.View).Columns[4].Width = Properties.Settings.Default.TopicViewColumnWidth4;
            if (Properties.Settings.Default.TopicViewColumnWidth5 > 0)
                ((GridView)topicListView.View).Columns[5].Width = Properties.Settings.Default.TopicViewColumnWidth5;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {            
            // TODO: cleanup code with setting variable

            Properties.Settings.Default.TopicViewColumnWidth1 = ((GridView)topicListView.View).Columns[1].ActualWidth;
            Properties.Settings.Default.TopicViewColumnWidth2 = ((GridView)topicListView.View).Columns[2].ActualWidth;
            Properties.Settings.Default.TopicViewColumnWidth3 = ((GridView)topicListView.View).Columns[3].ActualWidth;
            Properties.Settings.Default.TopicViewColumnWidth4 = ((GridView)topicListView.View).Columns[4].ActualWidth;
            Properties.Settings.Default.TopicViewColumnWidth5 = ((GridView)topicListView.View).Columns[5].ActualWidth;
            Properties.Settings.Default.Save();
        }
    }

    /// <summary>
    /// Compare the LastViewed of a topic against its LastModified to determine if it
    /// should be highlighted.
    /// </summary>
    public class DateTimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime lastModified = (DateTime)values[0];
            DateTime lastViewed = (DateTime)values[1];

            return DateTime.Compare(lastModified, lastViewed);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack not supported");
        }
    }
}
