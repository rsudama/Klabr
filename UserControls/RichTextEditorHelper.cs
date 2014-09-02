
using System;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.IO;
using System.Windows.Markup;
using System.Windows.Data;
using HTMLConverter;

namespace Gemcom.Klabr.UserControls
{
    public static class RichTextEditorHelper
    {
        public static readonly DependencyProperty BoundDocument =
           DependencyProperty.RegisterAttached("BoundDocument", typeof(string), typeof(RichTextEditorHelper),
           new FrameworkPropertyMetadata(null,
               FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
               OnBoundDocumentChanged)
               );

        private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            RichTextBox richTextBox = d as RichTextBox;

            if (richTextBox == null)
                return;

            RemoveEventHandler(richTextBox);

            string newXaml = GetBoundDocument(d);

            richTextBox.Document.Blocks.Clear();

            if (!string.IsNullOrEmpty(newXaml))
            {
                using (MemoryStream xamlMemoryStream = new MemoryStream(Encoding.ASCII.GetBytes(newXaml)))
                {
                    ParserContext parser = new ParserContext();
                    
                    parser.XmlnsDictionary.Add("", "http://schemas.microsoft.com/winfx/2006/xaml/presentation");
                    parser.XmlnsDictionary.Add("x", "http://schemas.microsoft.com/winfx/2006/xaml");
                    //FlowDocument doc = new FlowDocument();
                    Section section = XamlReader.Load(xamlMemoryStream, parser) as Section;
                    if (section == null)
                        return;

                    richTextBox.Document.Blocks.Add(section);

                }
            }

            AttachEventHandler(richTextBox);
        }

        private static void RemoveEventHandler(RichTextBox richTextBox)
        {
            Binding binding = BindingOperations.GetBinding(richTextBox, BoundDocument);

            if (binding != null)
            {
                if (binding.UpdateSourceTrigger == UpdateSourceTrigger.Default ||
                    binding.UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
                {

                    richTextBox.LostFocus -= HandleLostFocus;
                }
                else
                {
                    richTextBox.TextChanged -= HandleTextChanged;
                }
            }
        }

        private static void AttachEventHandler(RichTextBox box)
        {
            Binding binding = BindingOperations.GetBinding(box, BoundDocument);

            if (binding != null)
            {
                if (binding.UpdateSourceTrigger == UpdateSourceTrigger.Default ||
                    binding.UpdateSourceTrigger == UpdateSourceTrigger.LostFocus)
                {

                    box.LostFocus += HandleLostFocus;
                }
                else
                {
                    box.TextChanged += HandleTextChanged;
                }
            }
        }

        private static void HandleLostFocus(object sender, RoutedEventArgs e)
        {
            RichTextBox box = sender as RichTextBox;

            if (box == null)
                return;

            TextRange textRange = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                textRange.Save(memoryStream, DataFormats.Xaml);
                string xamlText = Encoding.Default.GetString(memoryStream.ToArray());
                SetBoundDocument(box, xamlText);
            }
        }

        private static void HandleTextChanged(object sender, RoutedEventArgs e)
        {
            // TODO: TextChanged is currently not working!
            RichTextBox box = sender as RichTextBox;
            if (box == null)
                return;

            TextRange textRange = new TextRange(box.Document.ContentStart, box.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                textRange.Save(ms, DataFormats.Xaml);
                string xamlText = Encoding.Default.GetString(ms.ToArray());
                SetBoundDocument(box, xamlText);
            }
        }

        public static string GetBoundDocument(DependencyObject dp)
        {
            var html = dp.GetValue(BoundDocument) as string;
            var xaml = string.Empty;

            if (!string.IsNullOrEmpty(html))
            {
                try
                {
                    xaml = HtmlToXamlConverter.ConvertHtmlToXaml(html, false);
                }
                catch (Exception exception)
                {
                    xaml = "Error in HTML document conversion: " + exception.Message;
                }
            }

            return xaml;
        }

        public static void SetBoundDocument(DependencyObject dependencyProperty, string value)
        {
            var xaml = value;
            string html;

            try
            {
                html = HtmlFromXamlConverter.ConvertXamlToHtml(xaml);
            }
            catch (Exception exception)
            {
                html = "Error in HTML document conversion: " + exception.Message;
            }

            dependencyProperty.SetValue(BoundDocument, html);
        }
    }
}