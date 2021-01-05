using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Xml;

namespace ShowMeTheTemplates
{
    public partial class MainWindow : MetroWindow
    {
        #region Private Fields

        private const string BrowserApp = "explorer.exe";

        private readonly List<string> filesToDeleteOnExit = new List<string>();

        [Obsolete]
        private readonly Assembly presentationFrameworkAssembly = Assembly.LoadWithPartialName("PresentationFramework");

        private readonly Dictionary<Type, object> typeElementMap = new Dictionary<Type, object>();

        #endregion Private Fields

        #region Public Constructors

        public MainWindow()
        {
            InitializeComponent();

            Closed += MainWindow_Closed;
            themes.SelectionChanged += Themes_SelectionChanged;

            DataContext = new List<TemplatedElementInfo>(TemplatedElementInfo.GetTemplatedElements(presentationFrameworkAssembly));
            themes.SelectedIndex = 0;
        }

        #endregion Public Constructors

        #region Public Methods

        public void WebBrowser_Navigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            // Queue the files to be deleted at shutdown (otherwise, View Source doesn't work)
            if (e.Url.IsFile) { filesToDeleteOnExit.Add(e.Url.LocalPath); }
        }

        #endregion Public Methods

        #region Private Methods

        private void BookHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(
                fileName: BrowserApp,
                arguments: "http://sellsbrothers.com/writing/wpfbook/");
        }

        private void ElementHolder_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ContentControl elementHolder = (ContentControl)sender;
            TemplatedElementInfo elementInfo = (TemplatedElementInfo)elementHolder.DataContext;

            // Get element (cached)
            object element = GetElement(elementInfo.ElementType);

            // Create and show the element (some have to be shown to give up their templates...)
            ShowElement(elementHolder, element);

            // Fill the element (don't seem to need to do this, but makes it easier to see on the screen...)
            FillElement(element);
        }

        private void FillElement(object element)
        {
            if (element is ContentControl control)
            {
                control.Content = "(some content)";

                if (element is HeaderedContentControl control1)
                {
                    control1.Header = "(a header)";
                }
            }
            else if (element is ItemsControl control1)
            {
                control1.Items.Add("(an item)");
            }
            else if (element is PasswordBox box)
            {
                box.Password = "(a password)";
            }
            else if (element is System.Windows.Controls.Primitives.TextBoxBase textBoxBase)
            {
                textBoxBase.AppendText("(some text)");
            }
            else if (element is Page page)
            {
                page.Content = "(some content)";
            }
        }

        // Get the element from a cache based on the type
        // Used to avoid recreating a type twice and used so that when the WebBrowser needs to get the templates for each property, it knows where to look
        private object GetElement(Type elementType)
        {
            if (!typeElementMap.ContainsKey(elementType))
            {
                typeElementMap[elementType] = presentationFrameworkAssembly.CreateInstance(elementType.FullName);
            }

            return typeElementMap[elementType];
        }

        private void LaunchOnGitHub(object sender, RoutedEventArgs e)
        {
            Process.Start(
                fileName: BrowserApp,
                arguments: "https://github.com/punker76/Show-Me-The-Templates-v2");
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            foreach (string file in filesToDeleteOnExit)
            {
                File.Delete(file);
            }
        }

        private void OriginalHyperlink_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(
                fileName: BrowserApp,
                arguments: "http://www.sellsbrothers.com/posts/details/2091");
        }

        private void ShowElement(ContentControl elementHolder, object element)
        {
            elementHolder.Content = null;

            Type elementType = element.GetType();
            if ((elementType == typeof(ToolTip)) ||
                (elementType == typeof(Window)))
            {
                // can't be set as a child, but don't need to be shown, so do nothing
            }
            else if (elementType == typeof(NavigationWindow))
            {
                NavigationWindow wnd = (NavigationWindow)element;
                wnd.WindowState = WindowState.Minimized;
                wnd.ShowInTaskbar = false;
                wnd.Show(); // needs to be shown once to "hydrate" the template
                wnd.Hide();
            }
            else if (typeof(ContextMenu).IsAssignableFrom(elementType))
            {
                elementHolder.ContextMenu = (ContextMenu)element;
            }
            else if (typeof(Page).IsAssignableFrom(elementType))
            {
                Frame frame = new Frame
                {
                    Content = element
                };
                elementHolder.Content = frame;
            }
            else
            {
                elementHolder.Content = element;
            }
        }

        private void ShowTemplate(System.Windows.Forms.WebBrowser browser, FrameworkTemplate template)
        {
            if (template == null)
            {
                browser.DocumentText = "(no template)";
                return;
            }

            // Write the template to a file so that the browser knows to show it as XML
            string filename = System.IO.Path.GetTempFileName();
            File.Delete(filename);
            filename = System.IO.Path.ChangeExtension(filename, "xml");

            // pretty print the XAML (for View Source)
            using (XmlTextWriter writer = new XmlTextWriter(filename, System.Text.Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                XamlWriter.Save(template, writer);
            }

            // Show the template
            browser.Navigate(new Uri(@"file:///" + filename));
        }

        private void Themes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            Uri themeUri = new Uri((string)((ComboBoxItem)cb.SelectedItem).Tag, UriKind.Relative);
            ResourceDictionary themeResources = (ResourceDictionary)Application.LoadComponent(themeUri);
            templateItems.Resources = themeResources;
        }

        // When the WF host has loaded (for each property on the currently selected control),
        // tell the WebBrowser to navigate to the property's template
        private void WindowsFormsHost_Loaded(object sender, RoutedEventArgs e)
        {
            WindowsFormsHost host = (WindowsFormsHost)sender;
            PropertyInfo prop = (PropertyInfo)host.DataContext;
            System.Windows.Forms.WebBrowser browser = (System.Windows.Forms.WebBrowser)host.Child;
            Type elementType = prop.ReflectedType;
            object element = GetElement(elementType);
            FrameworkTemplate template = (FrameworkTemplate)prop.GetValue(element, null);
            ShowTemplate(browser, template);
        }

        #endregion Private Methods
    }
}