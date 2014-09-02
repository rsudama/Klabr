
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Data;
using System.Xml;

namespace Gemcom.Klabr.UserControls
{
    /// <summary>
    /// Interaction logic for $safeitemname$.xaml
    /// </summary>
    public partial class AboutBox
    {
        /// <summary>
        /// Default constructor is protected so callers must use one with a parent.
        /// </summary>
        protected AboutBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor that takes a parent for this $safeitemname$ dialog.
        /// </summary>
        /// <param name="parent">Parent window for this dialog.</param>
        public AboutBox(Window parent)
            : this()
        {
            Owner = parent;
        }

        /// <summary>
        /// Handles click navigation on the hyperlink in the About dialog.
        /// </summary>
        /// <param name="sender">Object the sent the event.</param>
        /// <param name="e">Navigation events arguments.</param>
        private void hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (e.Uri != null && string.IsNullOrEmpty(e.Uri.OriginalString) == false)
            {
                string uri = e.Uri.AbsoluteUri;
                Process.Start(new ProcessStartInfo(uri));
                e.Handled = true;
            }
        }

        #region Fields

        private XmlDocument _xmlDocument;

        private const string PropertyNameTitle = "Title";
        private const string PropertyNameDescription = "Description";
        private const string PropertyNameProduct = "Product";
        private const string PropertyNameCopyright = "Copyright";
        private const string PropertyNameCompany = "Company";
        private const string PropertyNameTrademark = "Trademark";
        private const string XPathRoot = "ApplicationInfo/";
        private const string XPathTitle = XPathRoot + PropertyNameTitle;
        private const string XPathVersion = XPathRoot + "Version";
        private const string XPathDescription = XPathRoot + PropertyNameDescription;
        private const string XPathProduct = XPathRoot + PropertyNameProduct;
        private const string XPathCopyright = XPathRoot + PropertyNameCopyright;
        private const string XPathCompany = XPathRoot + PropertyNameCompany;
        private const string XPathLinkUri = XPathRoot + "Link/@Uri";

        #endregion

        #region Properties
        /// <summary>
        /// Gets the title property, which is display in the About dialogs window title.
        /// </summary>
        public string ProductTitle
        {
            get
            {
                string result = CalculatePropertyValue<AssemblyTitleAttribute>(PropertyNameTitle, XPathTitle);
                if (string.IsNullOrEmpty(result))
                {
                    // otherwise, just get the name of the assembly itself.
                    result = Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the application's Version information to show.
        /// </summary>
        public string Version
        {
            get
            {
                string result;
                // get the version from the assembly.
                Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

                if (assemblyVersion != null)
                {
                    result = assemblyVersion.ToString();
                }
                else
                {
                    // try to get the Version from a resource in the Application.
                    result = GetLogicalResourceString(XPathVersion);
                }

                return result;
            }
        }

        /// <summary>
        /// Gets the description about the application.
        /// </summary>
        public string Description
        {
            get { return CalculatePropertyValue<AssemblyDescriptionAttribute>(PropertyNameDescription, XPathDescription); }
        }

        /// <summary>
        ///  Gets the product's full name.
        /// </summary>
        public string Product
        {
            get { return CalculatePropertyValue<AssemblyProductAttribute>(PropertyNameProduct, XPathProduct); }
        }

        /// <summary>
        /// Gets the copyright information for the product.
        /// </summary>
        public string Copyright
        {
            get { return CalculatePropertyValue<AssemblyCopyrightAttribute>(PropertyNameCopyright, XPathCopyright); }
        }

        /// <summary>
        /// Gets the product's company name.
        /// </summary>
        public string Company
        {
            get { return CalculatePropertyValue<AssemblyCompanyAttribute>(PropertyNameCompany, XPathCompany); }
        }

        /// <summary>
        /// Gets the link text to display in the About dialog.
        /// </summary>
        public static string LinkText
        {
            get { return "More Info"; }
        }

        /// <summary>
        /// Gets the link uri that is the navigation target of the link.
        /// </summary>
        public string LinkUri
        {
            // This is a hack because tehre is no link attribute in the assembly info
            get { return  CalculatePropertyValue<AssemblyTrademarkAttribute>(PropertyNameTrademark, XPathLinkUri); }
        }
        #endregion

        #region Resource location methods

        /// <summary>
        /// Gets the specified property value either from a specific attribute, or from a resource dictionary.
        /// </summary>
        /// <typeparam name="T">Attribute type that we're trying to retrieve.</typeparam>
        /// <param name="propertyName">Property name to use on the attribute.</param>
        /// <param name="xpathQuery">XPath to the element in the XML data resource.</param>
        /// <returns>The resulting string to use for a property.
        /// Returns null if no data could be retrieved.</returns>
        private string CalculatePropertyValue<T>(string propertyName, string xpathQuery)
        {
            string result = string.Empty;
            // first, try to get the property value from an attribute.
            object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);
            if (attributes.Length > 0)
            {
                T attrib = (T)attributes[0];
                PropertyInfo property = attrib.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    result = property.GetValue(attributes[0], null) as string;
                }
            }

            // if the attribute wasn't found or it did not have a value, then look in an xml resource.
            if (result == string.Empty)
            {
                // if that fails, try to get it from a resource.
                result = GetLogicalResourceString(xpathQuery);
            }
            return result;
        }

        /// <summary>
        /// Gets the XmlDataProvider's document from the resource dictionary.
        /// </summary>
        protected virtual XmlDocument ResourceXmlDocument
        {
            get
            {
                if (_xmlDocument == null)
                {
                    // if we haven't already found the resource XmlDocument, then try to find it.
                    XmlDataProvider provider = TryFindResource("aboutProvider") as XmlDataProvider;
                    if (provider != null)
                    {
                        // save away the XmlDocument, so we don't have to get it multiple times.
                        _xmlDocument = provider.Document;
                    }
                }
                return _xmlDocument;
            }
        }

        /// <summary>
        /// Gets the specified data element from the XmlDataProvider in the resource dictionary.
        /// </summary>
        /// <param name="xpathQuery">An XPath query to the XML element to retrieve.</param>
        /// <returns>The resulting string value for the specified XML element. 
        /// Returns empty string if resource element couldn't be found.</returns>
        protected virtual string GetLogicalResourceString(string xpathQuery)
        {
            string result = string.Empty;
            // get the About xml information from the resources.
            XmlDocument doc = ResourceXmlDocument;
            if (doc != null)
            {
                // if we found the XmlDocument, then look for the specified data. 
                XmlNode node = doc.SelectSingleNode(xpathQuery);
                if (node != null)
                {
                    if (node is XmlAttribute)
                    {
                        // only an XmlAttribute has a Value set.
                        result = node.Value;
                    }
                    else
                    {
                        // otherwise, need to just return the inner text.
                        result = node.InnerText;
                    }
                }
            }
            return result;
        }

        #endregion
    }
}
