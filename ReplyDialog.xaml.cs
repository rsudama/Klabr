
using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Gemcom.Klabr.SPWebLib;
using Gemcom.Klabr.UserControls;

namespace Gemcom.Klabr
{
    public partial class ReplyDialog
    {
        private readonly SPDiscussion _discussion;
        private readonly SPTopic _topic;
        private List<SPTopic> _replies;
        private bool _sortFirstToLast;

        public ReplyDialog()
        {
            InitializeComponent();

            if (Properties.Settings.Default.ReplyWindowLeft > -1)
                Left = Properties.Settings.Default.ReplyWindowLeft;
            if (Properties.Settings.Default.ReplyWindowTop > -1)
                Top = Properties.Settings.Default.ReplyWindowTop;
            if (Properties.Settings.Default.ReplyWindowHeight > -1)
                Height = Properties.Settings.Default.ReplyWindowHeight;
            if (Properties.Settings.Default.ReplyWindowWidth > -1)
                Width = Properties.Settings.Default.ReplyWindowWidth;
        }

        public ReplyDialog(SPDiscussion discussion, SPTopic topic)
            : this()
        {
            _discussion = discussion;
            _topic = topic;

            Title = string.Format("Klabr - {0} : {1}", discussion.Title, topic.Title);            
            UpdateReplies();
        }

        private void UpdateReplies()
        {
            try
            {
                _topic.Body = MainWindow.Portal.TopicGetBody(_discussion, _topic);
                _replies = MainWindow.Portal.TopicGetReplies(_discussion, _topic);
                _replies.Insert(0, _topic);

                if (!_sortFirstToLast)
                    _replies.Reverse();

                repliesListView.Children.Clear();

                foreach (SPTopic reply in _replies)
                    AddReply(reply);
            }
            catch (Exception exception)
            {
                MainWindow.HandleException(exception);
            }
        }

        private void AddReply(SPTopic reply)
        {
            string htmlReply = reply.Body;
            Debug.WriteLine(htmlReply);

            // increase the default font size
            htmlReply = htmlReply.Replace(";font-size:12;", ";font-size:18;");
            Debug.WriteLine(htmlReply);

            RichTextEditor richTextEditor = new RichTextEditor(htmlReply, false);
            // TODO: set font size from user settings

            Label headerLabel = new Label();
            headerLabel.Content = string.Format("{0} wrote {1}", reply.Author, reply.Created);
            headerLabel.FontStyle = FontStyles.Italic;
            headerLabel.FontSize = 12;

            Expander replyExpander = new Expander();
            replyExpander.Header = headerLabel;
            replyExpander.Content = richTextEditor;
            replyExpander.IsExpanded = expander.IsExpanded;

            if (reply.LastModified > reply.LastViewed)
            {
                Label label = (Label)replyExpander.Header;
                label.FontWeight = FontWeights.Bold;
            }

            repliesListView.Children.Add(replyExpander);
        }

        private void OnReply(object sender, RoutedEventArgs e)
        {
            gridSplitter.Visibility = Visibility.Visible;
            editorTextBox.Visibility = Visibility.Visible;
            submitButton.Visibility = Visibility.Visible;
            cancelButton.Visibility = Visibility.Visible;

            replyButton.Visibility = Visibility.Hidden;
            closeButton.Visibility = Visibility.Hidden;

            editorTextBox.DelayedFocus();
        }

        private void OnSubmit(object sender, RoutedEventArgs e)
        {
            try
            {
                string body = editorTextBox.Text;
                string signature = Properties.Settings.Default.Signature;

                // append signature to body
                if (signature.Length > 0)
                    body = body.Replace("</BODY></HTML>", "<P>" + signature + "</P></BODY></HTML>");

                MainWindow.Portal.TopicAddReply(_discussion, _topic, body);
                OnCancel(sender, e);
                UpdateReplies();
            }
            catch (Exception exception)
            {
                MainWindow.HandleException(exception);
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            gridSplitter.Visibility = Visibility.Collapsed;
            editorTextBox.Visibility = Visibility.Collapsed;
            submitButton.Visibility = Visibility.Hidden;
            cancelButton.Visibility = Visibility.Hidden;

            replyButton.Visibility = Visibility.Visible;
            closeButton.Visibility = Visibility.Visible;
        }

        private void OnExpanded(object sender, RoutedEventArgs e)
        {
            if (repliesListView == null)
                return;

            foreach (UIElement reply in repliesListView.Children)
            {
                if (reply is Expander)
                    ((Expander)reply).IsExpanded = true;
            }
            expander.ToolTip = "Collapse all replies";
        }

        private void OnCollapsed(object sender, RoutedEventArgs e)
        {
            if (repliesListView == null)
                return;

            foreach (UIElement reply in repliesListView.Children)
            {
                if (reply is Expander)
                    ((Expander)reply).IsExpanded = false;
            }

            expander.ToolTip = "Expand all replies";
        }

        private void OnOrder(object sender, RoutedEventArgs e)
        {
            if (_sortFirstToLast)
            {
                orderLabel.Content = "First to Last";
            }
            else
            {
                orderLabel.Content = "Last to First";
            }

            _sortFirstToLast = !_sortFirstToLast;
            List<UIElement> replies = new List<UIElement>(repliesListView.Children.Count);

            foreach (UIElement reply in repliesListView.Children)
            {
                replies.Add(reply);
            }

            replies.Reverse();
            repliesListView.Children.Clear();

            foreach (UIElement reply in replies)
            {
                repliesListView.Children.Add(reply);
            }
        }

        private void OnClose(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.ReplyWindowLeft = Left;
            Properties.Settings.Default.ReplyWindowTop = Top;
            Properties.Settings.Default.ReplyWindowWidth = Width;
            Properties.Settings.Default.ReplyWindowHeight = Height;
            Properties.Settings.Default.Save();

            DialogResult = false;            
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    UpdateReplies();
                    break;

                case Key.F6:
                    try
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo(Properties.Settings.Default.WebUrl + _topic.Path);
                        startInfo.WindowStyle = ProcessWindowStyle.Normal;
                        Process.Start(startInfo);
                    }
                    catch (Exception exception)
                    {
                        MainWindow.HandleException(exception);
                    }
                    break;
            }
        }
    }
}
