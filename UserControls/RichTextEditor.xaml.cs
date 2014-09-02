
using System;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Xps;

namespace Gemcom.Klabr.UserControls
{
    public partial class RichTextEditor
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(RichTextEditor),
            new PropertyMetadata(string.Empty));

        public RichTextEditor()
        {
            InitializeComponent();

            mainRTB.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            mainRTB.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            mainRTB.SetValue(Block.LineHeightProperty, 3.0);
            mainRTB.SetValue(TextElement.FontSizeProperty, 12.0);

            // For handling hyperlinks...
            mainRTB.SetValue(RichTextBox.IsDocumentEnabledProperty, true);
            mainRTB.AddHandler(Hyperlink.RequestNavigateEvent, new RoutedEventHandler(HyperlinkClick));

            GotFocus += OnGotFocus;
        }

        public RichTextEditor(string htmlContent, Boolean isEditable)
            : this()
        {
            Text = htmlContent;

            mainRTB.IsReadOnly = !isEditable;
            if (!isEditable)
            {
                mainPanel.Children.Remove(toolbar);
                mainRTB.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                mainRTB.Background = Brushes.Moccasin;
            }
        }

        // This little piece of magic, used in conjunction with the extension method DelayedFocus,
        // will actually cause the editor to start accepting keyboard input when it receives the focus.
        //
        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (Visibility == Visibility.Visible && !mainRTB.IsReadOnly)
            {
                FocusManager.SetFocusedElement(this, mainRTB);
                Keyboard.Focus(mainRTB);
                mainRTB.CaretPosition = mainRTB.Document.ContentEnd;
            }
        }

        private static void HyperlinkClick(object sender, RoutedEventArgs args)
        {
            Hyperlink link = args.Source as Hyperlink;

            if (link == null)
                return;

            if (link.NavigateUri != null)
            {
                // Try to accomodate SharePoint relative URLs
                string uri = link.NavigateUri.ToString();
                if (uri.StartsWith("/site"))
                    uri = "https://www.gemcomportal.com" + uri;

                try
                {
                    Process.Start(uri);
                }
                catch (Exception exception)
                {
                    MainWindow.HandleException(exception);
                }
            }
        }

        private void ToggleHyperlink(object sender, RoutedEventArgs e)
        {
            DependencyObject element = mainRTB.Selection.Start.GetAdjacentElement(LogicalDirection.Forward);

            if (element is Hyperlink)
            {
                // All this nonsense just to remove the hyperlink attribute without changing other text attributes...
                Hyperlink hyperlink = (Hyperlink)element;

                // 1. Copy its children Inline to a temporary array
                InlineCollection hyperlinkChildren = hyperlink.Inlines;
                Inline[] inlines = new Inline[hyperlinkChildren.Count];
                hyperlinkChildren.CopyTo(inlines, 0);

                // 2. Remove each child from parent hyperlink element and insert it after the hyperlink
                for (int i = inlines.Length - 1; i >= 0; i--)
                {
                    hyperlinkChildren.Remove(inlines[i]);
                    hyperlink.SiblingInlines.InsertAfter(hyperlink, inlines[i]);
                }

                // 3. Apply hyperlink's local formatting properties to inlines (which are now outside hyperlink scope)
                LocalValueEnumerator localProperties = hyperlink.GetLocalValueEnumerator();
                TextRange inlineRange = new TextRange(inlines[0].ContentStart, inlines[inlines.Length - 1].ContentEnd);

                while (localProperties.MoveNext())
                {
                    LocalValueEntry property = localProperties.Current;
                    DependencyProperty dependencyProperty = property.Property;
                    object value = property.Value;

                    if (!dependencyProperty.ReadOnly &&
                        dependencyProperty != Inline.TextDecorationsProperty && // Ignore hyperlink defaults
                        dependencyProperty != TextElement.ForegroundProperty &&
                        !IsHyperlinkProperty(dependencyProperty))
                    {
                        inlineRange.ApplyPropertyValue(dependencyProperty, value);
                    }
                }

                // 4. Delete the (empty) hyperlink element
                hyperlink.SiblingInlines.Remove(hyperlink);
            }
            else
            {
                Hyperlink hyperlink = new Hyperlink(mainRTB.Selection.Start, mainRTB.Selection.End);
                string link = mainRTB.Selection.Text;
                if (!link.Contains("://"))
                    link = "http://" + link;

                hyperlink.NavigateUri = new Uri(link);
                // De-select selection
                //mainRTB.Selection.Select(mainRTB.Selection.End, mainRTB.Selection.End);
            }
        }

        // Helper that returns true when passed property applies to Hyperlink only.
        private static bool IsHyperlinkProperty(DependencyProperty dependencyProperty)
        {
            return dependencyProperty == Hyperlink.CommandProperty ||
                dependencyProperty == Hyperlink.CommandParameterProperty ||
                dependencyProperty == Hyperlink.CommandTargetProperty ||
                dependencyProperty == Hyperlink.NavigateUriProperty ||
                dependencyProperty == Hyperlink.TargetNameProperty;
        }

        public string Text
        {
            get { return GetValue(TextProperty) as string; }
            set { SetValue(TextProperty, value); }
        }

        // Print RichTextBox content
        private void PrintCommand(object sender, RoutedEventArgs e)
        {
            /*
            PrintDialog printDialog = new PrintDialog();
            if ((printDialog.ShowDialog() == true))
            {
                //use either one of the below      
                //printDialog.PrintVisual(mainRTB as Visual, "printing as visual");
                printDialog.PrintDocument((((IDocumentPaginatorSource)mainRTB.Document).DocumentPaginator), "printing as paginator");
            }
            */

            // Serialize RichTextBox content into a stream in Xaml or XamlPackage format. (Note: XamlPackage format isn't supported in partial trust.)
            TextRange sourceDocument = new TextRange(mainRTB.Document.ContentStart, mainRTB.Document.ContentEnd);
            MemoryStream stream = new MemoryStream();
            sourceDocument.Save(stream, DataFormats.Xaml);

            // Clone the source document's content into a new FlowDocument.
            FlowDocument flowDocumentCopy = new FlowDocument();
            TextRange copyDocumentRange = new TextRange(flowDocumentCopy.ContentStart, flowDocumentCopy.ContentEnd);
            copyDocumentRange.Load(stream, DataFormats.Xaml);

            // Create a XpsDocumentWriter object, open a Windows common print dialog.
            // This methods returns a ref parameter that represents information about the dimensions of the printer media. 
            PrintDocumentImageableArea ia = null;
            XpsDocumentWriter docWriter = PrintQueue.CreateXpsDocumentWriter(ref ia);

            if (docWriter != null && ia != null)
            {
                DocumentPaginator paginator = ((IDocumentPaginatorSource)flowDocumentCopy).DocumentPaginator;

                // Change the PageSize and PagePadding for the document to match the CanvasSize for the printer device.
                paginator.PageSize = new Size(ia.MediaSizeWidth, ia.MediaSizeHeight);
                Thickness pagePadding = flowDocumentCopy.PagePadding;
                flowDocumentCopy.PagePadding = new Thickness(
                        Math.Max(ia.OriginWidth, pagePadding.Left),
                        Math.Max(ia.OriginHeight, pagePadding.Top),
                        Math.Max(ia.MediaSizeWidth - (ia.OriginWidth + ia.ExtentWidth), pagePadding.Right),
                        Math.Max(ia.MediaSizeHeight - (ia.OriginHeight + ia.ExtentHeight), pagePadding.Bottom));
                flowDocumentCopy.ColumnWidth = double.PositiveInfinity;

                // Send DocumentPaginator to the printer.
                docWriter.Write(paginator);
            }
        }

 /*
        private static void Key_Down(object sender, KeyEventArgs e)
        {
            RichTextBox myRichTextBox = (RichTextBox)sender;

            if (e.Key != Key.Space && e.Key != Key.Back && e.Key != Key.Return && e.Key != Key.H)
            {
                return;
            }

            if (!myRichTextBox.Selection.IsEmpty)
            {
                myRichTextBox.Selection.Text = String.Empty;
            }

            TextPointer caretPosition = myRichTextBox.Selection.Start;

            //if (e.Key == Key.Space || e.Key == Key.Return)
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.H)
            {
                TextPointer wordStartPosition;
                string word = GetPreceedingWordInParagraph(caretPosition, out wordStartPosition);

                //if (word == "www.microsoft.com") // A real app would need a more sophisticated RegEx match expression for hyperlinks.
                {
                    // Insert hyperlink element at word boundaries.
                    new Hyperlink(
                        wordStartPosition.GetPositionAtOffset(0, LogicalDirection.Backward),
                        caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward));

                    // No need to update RichTextBox caret position, 
                    // since we only inserted a Hyperlink ElementEnd following current caretPosition.
                    // Subsequent handling of space input by base RichTextBox will update selection.
                }
            }
            else // Key.Back
            {
                TextPointer backspacePosition = caretPosition.GetNextInsertionPosition(LogicalDirection.Backward);
                Hyperlink hyperlink;
                if (backspacePosition != null && IsHyperlinkBoundaryCrossed(caretPosition, backspacePosition, out hyperlink))
                {
                    // Remember caretPosition with forward gravity. This is necessary since we are going to delete 
                    // the hyperlink element preceeding caretPosition and after deletion current caretPosition 
                    // (with backward gravity) will follow content preceeding the hyperlink. 
                    // We want to remember content following the hyperlink to set new caret position at.

                    TextPointer newCaretPosition = caretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);

                    // Deleting the hyperlink is done using logic below.

                    // 1. Copy its children Inline to a temporary array.
                    InlineCollection hyperlinkChildren = hyperlink.Inlines;
                    Inline[] inlines = new Inline[hyperlinkChildren.Count];
                    hyperlinkChildren.CopyTo(inlines, 0);

                    // 2. Remove each child from parent hyperlink element and insert it after the hyperlink.
                    for (int i = inlines.Length - 1; i >= 0; i--)
                    {
                        hyperlinkChildren.Remove(inlines[i]);
                        hyperlink.SiblingInlines.InsertAfter(hyperlink, inlines[i]);
                    }

                    // 3. Apply hyperlink's local formatting properties to inlines (which are now outside hyperlink scope).
                    LocalValueEnumerator localProperties = hyperlink.GetLocalValueEnumerator();
                    TextRange inlineRange = new TextRange(inlines[0].ContentStart, inlines[inlines.Length - 1].ContentEnd);

                    while (localProperties.MoveNext())
                    {
                        LocalValueEntry property = localProperties.Current;
                        DependencyProperty dp = property.Property;
                        object value = property.Value;

                        if (!dp.ReadOnly &&
                            dp != Inline.TextDecorationsProperty && // Ignore hyperlink defaults.
                            dp != TextElement.ForegroundProperty &&
                            !IsHyperlinkProperty(dp))
                        {
                            inlineRange.ApplyPropertyValue(dp, value);
                        }
                    }

                    // 4. Delete the (empty) hyperlink element.
                    hyperlink.SiblingInlines.Remove(hyperlink);

                    // 5. Update selection, since we deleted Hyperlink element and caretPosition was at that Hyperlink's end boundary.
                    myRichTextBox.Selection.Select(newCaretPosition, newCaretPosition);
                }
            }
        }

        // Helper that returns true when passed property applies to Hyperlink only.
        private static bool IsHyperlinkProperty(DependencyProperty dp)
        {
            return dp == Hyperlink.CommandProperty ||
                dp == Hyperlink.CommandParameterProperty ||
                dp == Hyperlink.CommandTargetProperty ||
                dp == Hyperlink.NavigateUriProperty ||
                dp == Hyperlink.TargetNameProperty;
        }

        // Helper that returns true if passed caretPosition and backspacePosition cross a hyperlink end boundary
        // (under the assumption that caretPosition and backSpacePosition are adjacent insertion positions).
        private static bool IsHyperlinkBoundaryCrossed(TextPointer caretPosition, TextPointer backspacePosition, out Hyperlink backspacePositionHyperlink)
        {
            Hyperlink caretPositionHyperlink = GetHyperlinkAncestor(caretPosition);
            backspacePositionHyperlink = GetHyperlinkAncestor(backspacePosition);

            return (caretPositionHyperlink == null && backspacePositionHyperlink != null) ||
                (caretPositionHyperlink != null && backspacePositionHyperlink != null && caretPositionHyperlink != backspacePositionHyperlink);
        }

        // Helper that returns a hyperlink ancestor of passed position.
        private static Hyperlink GetHyperlinkAncestor(TextPointer position)
        {
            Inline parent = position.Parent as Inline;
            while (parent != null && !(parent is Hyperlink))
            {
                parent = parent.Parent as Inline;
            }

            return parent as Hyperlink;
        }

        // Helper that returns a word preceeding the passed position in its paragraph, 
        // wordStartPosition points to the start position of word.
        private static string GetPreceedingWordInParagraph(TextPointer position, out TextPointer wordStartPosition)
        {
            wordStartPosition = null;
            string word = String.Empty;

            Paragraph paragraph = position.Paragraph;
            if (paragraph != null)
            {
                TextPointer navigator = position;
                while (navigator.CompareTo(paragraph.ContentStart) > 0)
                {
                    string runText = navigator.GetTextInRun(LogicalDirection.Backward);

                    if (runText.Contains(" ")) // Any globalized application would need more sophisticated word break testing.
                    {
                        int index = runText.LastIndexOf(" ");
                        word = runText.Substring(index + 1, runText.Length - index - 1) + word;
                        wordStartPosition = navigator.GetPositionAtOffset(-1 * (runText.Length - index - 1));
                        break;
                    }
                    else
                    {
                        wordStartPosition = navigator;
                        word = runText + word;
                    }
                    navigator = navigator.GetNextContextPosition(LogicalDirection.Backward);
                }
            }

            return word;
        }
*/
    }

    public static class MyExtensions
    {
        ///
        /// REALLY focuses the specified UI element.
        ///
        public static void DelayedFocus(this UIElement uiElement)
        {
            uiElement.Dispatcher.BeginInvoke(
            new Action(() =>
                           {
                               uiElement.Focusable = true;
                               uiElement.Focus();
                               Keyboard.Focus(uiElement);
                           }),
            DispatcherPriority.Render);
        }
    }
}
