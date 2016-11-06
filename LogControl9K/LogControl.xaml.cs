using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace LogControl9K {
    /// <summary>
    /// Interaction logic for LogControl.xaml
    /// </summary>
    public partial class LogControl : UserControl {

        #region Constructor

        public LogControl() {
            InitializeComponent();

            /* Creating tabs for predefined types, custom types */
            InitTabs(null);
            
            /* Events */
            // I think the reason of -= += is Singleton
            // http://stackoverflow.com/questions/25190270/c-sharp-event-handler-is-called-multiple-times-when-event-is-raised-once
            _log9KCore.CustomTypeAdded -= c_CustomTypeAdded; // unregister
            _log9KCore.CustomTypeAdded += c_CustomTypeAdded; // register

            _log9KCore.TabOrderChanged -= c_TabOrderChanged;
            _log9KCore.TabOrderChanged += c_TabOrderChanged;

            string filename =_log9KCore.Log9KTabsDictionary[Log9KCore.ALL_LOG_TAB_S].FilenameLogFile;
            FileNameTextBox.Text = filename;

            DataContext = this;
        }

        #endregion

        #region Icons for default tabs methods

        private static Uri IconForDefaultTabs(Log9KTab log9KTab) {
            if (log9KTab.IsAllTab()) {
                return new Uri("pack://application:,,,/img/" + Log9KCore.ALL_LOG_TAB_S + ".png");
            }
            return new Uri("pack://application:,,,/img/" + log9KTab.TabType.ToString().ToLower() + ".png");
        }

        #endregion

        #region Callbacks
        
        /// <summary>
        /// Callback, that adds new tab of custom (user's) type to TabControl. 
        /// </summary>
        /// <param name="e"></param>
        private void c_CustomTypeAdded(Log9KCore.CustomTypeAddedEventArgs e) {
            if (_log9KCore.Log9KTabsDictionary.ContainsKey(e.Name)) {
                Log9KTab customTab = _log9KCore.Log9KTabsDictionary[e.Name];
                AddTab(customTab);
            }
        }

        /// <summary>
        /// Callback for TabOrderChanged, 
        /// removes all the TabItems in control and reinits them in given order.
        /// </summary>
        /// <param name="e"></param>
        private void c_TabOrderChanged(Log9KCore.TabOrderChangedEventArgs e) {
            ClearTabs();
            InitTabs(e.TabTypes);
        }

        /// <summary>
        /// <para>Callback for event when log entry duplication occured.</para>
        /// <para>Adds new TreeViewItem to TreeView, if that duplication is new.</para>
        /// <para>Otherwise it refreshes number of occurs in TreeViewItem header 
        /// (number in parenthesis).</para>
        /// </summary>
        /// <param name="e"></param>
        private void c_DuplicationEntryAdded(Log9KTab.DuplicationEntryAddedEventArgs e) {
            if (e.TabName == null || e.Message == null) {
                return;
            }

            if (!_tabItemDictionary.ContainsKey(e.TabName)) {
                return;
            }

            Util9K.InvokeInUiThread(() => {
                Log9KTab log9KTab = _log9KCore.Log9KTabsDictionary[e.TabName];
                Log9KEntry log9KEntry = log9KTab.DuplicationsDictionary[e.DuplicationKey].Item1;
                ObservableCollection<Log9KTime> timesList = log9KTab.DuplicationsDictionary[e.DuplicationKey].Item2;

                TabItem tabItem = _tabItemDictionary[e.TabName];

                try {
                    Grid tabGrid = (Grid) tabItem.Content;
                    TreeView treeView = (TreeView) tabGrid.Children[_treeViewIndex];

                    if (e.IsNewDuplication) {
                    /* Adding new TreeViewItem */
                        StackPanel headerStackPanel = new StackPanel {Orientation = Orientation.Horizontal};
                        
                        /* --- TYPE Message (number of duplications) --- */
                        /* Foreground color for TYPE */
                        Brush foreground = _typeToColorConverter.Convert(e.TabType);
                        /* Type TextBlock */
                        string type;
                        if (e.TabType == Log9KCore.LogEntryType.CUSTOM) {
                            type = e.EntryCustomType;
                        } else {
                            type = e.TabType.ToString();
                        }
                        headerStackPanel.Children.Add(new TextBlock {Text = type, Foreground = foreground, FontWeight = FontWeights.Bold});
                        /* Message TextBlock */
                        headerStackPanel.Children.Add(new TextBlock {Text = e.Message, Margin = new Thickness(5, 0, 0, 0)});
                        /* Number of duplications TextBlock */
                        headerStackPanel.Children.Add(new TextBlock {Text = "(2)", FontWeight = FontWeights.Bold, Margin = new Thickness(5, 0, 0, 0)});

                        TreeViewItem treeViewItem = new TreeViewItem {
                            Header = headerStackPanel,
                            ItemsSource = timesList 
                        };
                        treeView.Items.Add(treeViewItem);
                    } else {
                    /* Updating number of duplications in parenthesis */
                        foreach (TreeViewItem treeViewItem in treeView.Items) {
                            StackPanel headerStackPanel = (StackPanel) treeViewItem.Header;
                            TextBlock typeTextBlock = (TextBlock) headerStackPanel.Children[0];
                            TextBlock messageTextBlock = (TextBlock) headerStackPanel.Children[1];

                            if (messageTextBlock.Text.Equals(e.Message) && typeTextBlock.Text.Equals(log9KEntry.TypeString)) {
                                TextBlock numberOfDuplicationTextBlock = (TextBlock) headerStackPanel.Children[2];
                                int numberOfDuplication = timesList.Count;
                                numberOfDuplicationTextBlock.Text = "(" + numberOfDuplication + ")";
                            }
                        }
                    }
                } catch (InvalidCastException) { }
            });
        }

        static TypeToColorConverter _typeToColorConverter = new TypeToColorConverter();

        #endregion

        #region Scroll down related stuff, is redundant now
        // @TODO: CLEAR!!!
        private static void ScrollDownTabItem(TabItem tabItem) {
            ScrollViewer scroll = GetScrollViewer(tabItem);
            if (scroll != null) {
                scroll.ScrollToEnd();
            }
        }

        // @TODO: CLEAR!!!
        private static ScrollViewer GetScrollViewer(TabItem tabItem) {
            Grid grid = tabItem.Content as Grid;
            try {
                if (grid != null) {
                    DataGrid dataGrid = grid.Children[0] as DataGrid;
                    if (dataGrid != null && dataGrid.Items.Count > 0) {
                        Decorator border = VisualTreeHelper.GetChild(dataGrid, 0) as Decorator;
                        if (border != null) {
                            ScrollViewer scroll = border.Child as ScrollViewer;
                            if (scroll != null) return scroll;
                        }
                    }
                }
            } catch (ArgumentOutOfRangeException) {
                 
            }
            return null;
        }

        #endregion

        #region Tab creation, clear, initialization methods 
        
        /// <summary>
        /// Initializes tabs by given tab order (adding tabs to TabControl, binding with Log9KTabs)
        /// </summary>
        /// <param name="types"></param>
        private void InitTabs(Log9KCore.LogEntryType[] types) {
            /* Creating tabs for predefined types, custom types */
            Log9KTabAll tabAll = (Log9KTabAll)_log9KCore.Log9KTabsDictionary[Log9KCore.ALL_LOG_TAB_S];
            AddTab(tabAll);

            /* Generate list of strings, which are names of tabs*/
            List<string> logEntryTypeStrings;
            if (types == null) {
                logEntryTypeStrings = new List<string>(Enum.GetNames(typeof(Log9KCore.LogEntryType)));
            } else {
                logEntryTypeStrings = new List<string>();
                foreach (Log9KCore.LogEntryType t in types) {
                    logEntryTypeStrings.Add(t.ToString());
                }
            }
            /* Adding list of custom types to tab names */ 
            logEntryTypeStrings.AddRange(_log9KCore.CustomTypesList);

            foreach (string logEntryType in logEntryTypeStrings) {
                if (logEntryType == Log9KCore.LogEntryType.CUSTOM.ToString() || logEntryType == Log9KCore.ALL_LOG_TAB_S) {
                    continue;
                }
                Log9KTab currentTab = _log9KCore.Log9KTabsDictionary[logEntryType];
                AddTab(currentTab);
            }
        }
        
        /// <summary>
        /// Adds new TabItem to the TabControl (by given Log9KTab)
        /// </summary>
        /// <param name="log9KTab"></param>
        private void AddTab(Log9KTab log9KTab) {
            AddEventsToTab(log9KTab);

            string name = log9KTab.TabName;
            TabItem tabItem = CreateTabItem(log9KTab);
            Log9KTabGrid l = new Log9KTabGrid();
            l.DataGrid9K.ItemsSource = log9KTab.LogEntryCollection;
            l.Width = 1000;
            tabItem.Content = l;

            _tabItemDictionary.Add(name, tabItem);
            TabControl.Items.Add(tabItem);
        }

        private void AddEventsToTab(Log9KTab log9KTab) {
            log9KTab.DuplicationEntryAdded -= c_DuplicationEntryAdded;
            log9KTab.DuplicationEntryAdded += c_DuplicationEntryAdded;
        }
        
        /// <summary>
        /// Removes tabs from tab control 
        /// (method for reordering tabs) 
        /// </summary>
        private void ClearTabs() {
            TabControl.Items.Clear();
            _tabItemDictionary.Clear();
        }

        /// <summary>
        /// Method that creates TabItem for Log9KTab and filling it with ui elements.
        /// </summary>
        /// <param name="log9KTab"></param>
        /// <returns></returns>
        private static TabItem CreateTabItem(Log9KTab log9KTab) {
            string name = log9KTab.TabName;
            Log9KCore.LogEntryType type = log9KTab.TabType;
            ObservableCollection<Log9KEntry> logEntryCollection = log9KTab.LogEntryCollection;

            TabItem tabItem = new TabItem {Header = name};


            /* Header of Tab: text and icon */ 
            StackPanel headerStackPanel = new StackPanel {Orientation = Orientation.Horizontal};
            headerStackPanel.Children.Add(new TextBlock {
                Text=name, 
                HorizontalAlignment = HorizontalAlignment.Left, 
                Foreground = _typeToColorConverter.Convert(type)
            });
            try {
                BitmapImage iconBitmapImage = new BitmapImage(IconForDefaultTabs(log9KTab));
                Image icon = new Image {
                    Source = iconBitmapImage,
                    Width = ICON_WIDTH,
                    Height = ICON_HEIGHT,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                headerStackPanel.Children.Add(icon);
            } catch (IOException) {
                Debug.WriteLine("Can't find icon for: " + name);
            }
            tabItem.Header = headerStackPanel;
            /*------------------------------------------------------------ */

            
            /*------------------------------------------------------------ */
            /* 
             * <Style TargetType="DataGridRow">
   <Style.Triggers>
     <DataTrigger Binding="{Binding Path=City_name}" Value="SomeCity">
       <Setter Property="Background" Value="{StaticResource SomeCityBackground}"/>
     </DataTrigger>
   </Style.Triggers>
 </Style>
             */

            return tabItem;
        }

        #endregion

        #region private fields

        private static int _treeViewIndex;

        private readonly Dictionary<string, TabItem> _tabItemDictionary = new Dictionary<string, TabItem>();

        private Log9KCore _log9KCore = Log9KCore.Instance;

        #endregion
        
        #region UI size and stuff constants

        private const int CONTROL_WIDTH = 1000;
        private const int CONTROL_HEIGHT = 400;
        private const int DATA_GRID_WIDTH = 500;
        private const int DATA_GRID_HEIGHT = CONTROL_HEIGHT - 50;
        private const int TREE_VIEW_WIDTH = CONTROL_WIDTH - DATA_GRID_WIDTH;
        private const int TREE_VIEW_HEIGHT = DATA_GRID_HEIGHT;
        private const int ICON_WIDTH = 15;
        private const int ICON_HEIGHT = 15;

        #endregion

        private void CheckBox_Click(object sender, RoutedEventArgs e) {
            if (WritingToFileEnabledCheckBox.IsChecked == null) {
                return;
            }
            if ((bool) WritingToFileEnabledCheckBox.IsChecked) {
                _log9KCore.Settings.EnableFileWriting();
            } else {
                _log9KCore.Settings.DisableFileWriting();
            }
        }
    }


    class TypeToColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (!(value is Log9KCore.LogEntryType)) {
                // Value is not an LogEntryType. Do not throw an exception
                // in the converter, but return something that is obviously wrong
                return Brushes.Purple;
            }
            Log9KCore.LogEntryType type = (Log9KCore.LogEntryType)value;
            switch (type) {
                case Log9KCore.LogEntryType.INFO:
                    return Brushes.Black;
                case Log9KCore.LogEntryType.ERROR:
                    return Brushes.DarkRed;
                case Log9KCore.LogEntryType.CRITICAL_ERROR:
                    return Brushes.Red;
                case Log9KCore.LogEntryType.SUCCESS:
                    return Brushes.Green;
                case Log9KCore.LogEntryType.CODE_FLOW:
                    return Brushes.DarkGray;
                case Log9KCore.LogEntryType.CUSTOM:
                    return Brushes.CornflowerBlue;
                default:
                    return Brushes.Black;
            }
        }

        public Brush Convert(Log9KCore.LogEntryType type) {
            return (Brush)Convert(type, null, null, null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }  
}
