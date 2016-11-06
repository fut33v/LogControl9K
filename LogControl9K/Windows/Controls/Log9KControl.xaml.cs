using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LogControl9K.Annotations;
using LogControl9K.Windows.Util;

namespace LogControl9K.Windows.Controls {
    /// <summary>
    /// Interaction logic for Log9KControl.xaml
    /// </summary>
    public partial class Log9KControl {

        /// <summary>
        /// LogControl9K log control
        /// </summary>
        public Log9KControl() {
            InitializeComponent();
        }

        private void FolderTextBox_OnLostFocus(object sender, RoutedEventArgs e) {
            MessageBox.Show("Папка будет изменена после перезапуска, если настройки будут сохранены");
        }
        
        /// <summary>
        /// Show Debug Window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Control_OnMouse(object sender, MouseButtonEventArgs e) {
#if DEBUG
            if (e.ClickCount == 3) {
                DebugWindow dw = new DebugWindow();
                dw.Show();
            }
#endif
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show(
                "Ctrl+PageDown — перейти к следующей вкладке" + Environment.NewLine +
                "Ctrl+PageUp — перейти к предыдущей вкладке" + Environment.NewLine +
                Environment.NewLine +
                "Shift+PageUp — подгрузить более старые логи" + Environment.NewLine +
                "Shift+PageDown — подгрузить более новые логи" + Environment.NewLine + 
                Environment.NewLine +
                "Ctrl+G — перейти к логу с заданным ID" + Environment.NewLine +
                "Ctrl+H — показать/скрыть меню таблицы логов" + Environment.NewLine +
                "Ctrl+T — фильтрация по времени" + Environment.NewLine +
                "Ctrl+D — показывать дерево повторов" + Environment.NewLine +
                "Ctrl+L — очистить таблицу" + Environment.NewLine +
                "Ctrl+N — показывать новые логи" + Environment.NewLine +
                "Ctrl+R — автопрокрутка"
            );
        }
        

        #region GridSplitter issue

        /// <summary>
        /// Index of TreeView column in grid's column definitions collection
        /// </summary>
        private const int TREE_VIEW_INDEX = 1;

        /// <summary>
        /// Have user dragged GridSplitter between
        /// </summary>
        private bool _isGridSplitterUserDragged = false;

        /// <summary>
        /// User dragged with GridSplitter width for TreeView column
        /// </summary>
        private GridLength _treeViewColumnWidth;

        private void Thumb_OnDragCompleted(object sender, DragCompletedEventArgs e) {
            _isGridSplitterUserDragged = true;
            GridSplitter gridSplitter = sender as GridSplitter;
            if (gridSplitter == null) {
                return;
            }
            Grid grid = gridSplitter.Parent as Grid;
            if (grid == null) {
                return;
            }
            ColumnDefinitionCollection c = grid.ColumnDefinitions;
            _treeViewColumnWidth = c[TREE_VIEW_INDEX].Width;
        }

        private void UIElement_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (!_isGridSplitterUserDragged) {
                return;
            }
            TreeView treeView = sender as TreeView;
            if (treeView == null) {
                return;
            }
            Grid grid = treeView.Parent as Grid;
            if (grid == null) {
                return;
            }

            ColumnDefinitionCollection c = grid.ColumnDefinitions;
            bool isVisible = (bool)e.NewValue;
            if (isVisible) {
                c[TREE_VIEW_INDEX].Width = _treeViewColumnWidth;
            } else {
                c[TREE_VIEW_INDEX].Width = new GridLength(0);
            }
        }

        #endregion


    }

        
    #region BindingProxy

    /// <summary>
    /// <para>Used to get data context out of logical tree</para>
    /// <remarks> http://stackoverflow.com/questions/15494226/cannot-find-source-for-binding-with-reference-relativesource-findancestor </remarks>
    /// </summary>
    public class BindingProxy : Freezable {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override Freezable CreateInstanceCore() {
            return new BindingProxy();
        }

        /// <summary>
        /// Proxy object
        /// </summary>
        public object Data {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for Data.
        /// This enables animation, styling, binding, etc...
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object),
            typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    #endregion


    #region Log9KDataGrid

    class Log9KDataGrid : DataGrid {


        #region Fields

        /// <summary>
        /// Is ItemsSource just reloaded
        /// </summary>
        private bool _justReloaded;

        #endregion


        #region Properties 

        public bool ScrollToLastLog { get; set; }
        public Log9KTab CurrentTab { get; set; }

        public ICommand StartRemovingFromBottomCommand { get; set; }
        public ICommand StartRemovingFromTopCommand { get; set; }
        public ICommand LoadOlderLogCommand { get; set; }
        public ICommand LoadNewerLogCommand { get; set; }

        public ICommand SelectLogEntryCommand { get; set; }
        public ICommand CopyEntriesCommand { get; set; }

        public static readonly DependencyProperty StartRemovingFromBottomCommandProperty =
        DependencyProperty.Register(
           "StartRemovingFromBottomCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );

        public static readonly DependencyProperty StartRemovingFromTopCommandProperty =
        DependencyProperty.Register(
           "StartRemovingFromTopCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );
        
        public static readonly DependencyProperty LoadOlderLogCommandProperty =
        DependencyProperty.Register(
           "LoadOlderLogCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );
        
        public static readonly DependencyProperty LoadNewerLogCommandProperty =
        DependencyProperty.Register(
           "LoadNewerLogCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );
        
        public static readonly DependencyProperty SelectLogEntryCommandProperty =
        DependencyProperty.Register(
           "SelectLogEntryCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );
        
        public static readonly DependencyProperty CopyEntriesCommandProperty =
        DependencyProperty.Register(
           "CopyEntriesCommand",
           typeof(ICommand),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );

        public static readonly DependencyProperty ScrollToLastLogProperty =
        DependencyProperty.Register(
           "ScrollToLastLog",
           typeof(bool),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(bool), OnDependencyPropertyChanged)
        );

        public static readonly DependencyProperty CurrentTabProperty =
        DependencyProperty.Register(
           "CurrentTab",
           typeof(Log9KTab),
           typeof(Log9KDataGrid),
           new PropertyMetadata(default(ICommand), OnDependencyPropertyChanged)
        );

        private static void OnDependencyPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            Log9KDataGrid log9KDataGrid = d as Log9KDataGrid;
            if (log9KDataGrid == null) {
                return;
            }
            if (e.Property.Name == "StartRemovingFromBottomCommand") {
                log9KDataGrid.StartRemovingFromBottomCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "StartRemovingFromTopCommand") {
                log9KDataGrid.StartRemovingFromTopCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "LoadOlderLogCommand") {
                log9KDataGrid.LoadOlderLogCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "LoadNewerLogCommand") {
                log9KDataGrid.LoadNewerLogCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "SelectLogEntryCommand") {
                log9KDataGrid.SelectLogEntryCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "ScrollToLastLog") {
                log9KDataGrid.ScrollToLastLog = e.NewValue as bool? ?? false;
            }
            if (e.Property.Name == "CopyEntriesCommand") {
                log9KDataGrid.CopyEntriesCommand = e.NewValue as ICommand;
            }
            if (e.Property.Name == "CurrentTab") {
                log9KDataGrid.CurrentTab = e.NewValue as Log9KTab;
                log9KDataGrid.OnCurrentTabChanged();
            }

        }

        private void OnCurrentTabChanged() {
            if (CurrentTab != null) {
                if (SelectedItem != null) {
                    ScrollIntoView(SelectedItem);
                    this.ScrollToCenterOfView(SelectedItem);
                    Focus();
                    _justReloaded = false;
                }
            }
        }

        #endregion


        #region Constructor

        public Log9KDataGrid() {
            Loaded += OnLoaded;

            CommandManager.RegisterClassCommandBinding(
                typeof(Log9KDataGrid),
                new CommandBinding(ApplicationCommands.Copy, CopyExecutedAction, null)
            );
        }

        private void CopyExecutedAction(object sender, ExecutedRoutedEventArgs executedRoutedEventArgs) {
            var dataGrid = sender as Log9KDataGrid;
            if (dataGrid == null) {
                return;
            }
            if (CopyEntriesCommand != null) {
                CopyEntriesCommand.Execute(SelectedItems);
            }
        }

        #endregion


        #region Private methods
        
        /// <summary>
        /// <para>Attaching handler for event of loading row in which attaching handler to double click</para>
        /// <para>(attaching handler for 2click on row event)</para>
        /// <remarks>Not used yet</remarks>
        /// </summary>
        private void AttachHandlerToDoubleClickOnDataGridRow() {
            if (RowStyle == null) {
                RowStyle = new Style{TargetType = typeof(DataGridRow)};
            }
            RowStyle.Setters.Add(
                new EventSetter(LoadedEvent, new RoutedEventHandler(RowLoaded))
            );
        }

        /// <summary>
        /// Attaching SelectLogEntryCommand to row double click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RowLoaded(object sender, RoutedEventArgs e) {
            DataGridRow row = sender as DataGridRow;
            if (row == null) {
                return;
            }
            if (SelectLogEntryCommand == null) {
                return;
            }
            row.InputBindings.Add(new MouseBinding(SelectLogEntryCommand,
                new MouseGesture() {MouseAction = MouseAction.LeftDoubleClick}));
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs) {
            PreviewMouseWheel += OnPreviewMouseWheel;

            // let's add Focus when we loading older or newer logs (bug fix)
            foreach (InputBinding ib in InputBindings) {
                string s = ib.CommandParameter as string;
                if (s != null) {
                    if (s == "LoadOlderLogArrayCommand" || s =="LoadNewerLogArrayCommand") {
                        ICommand logic = ib.Command;
                        sCommand c = new sCommand(() => {
                            logic.Execute(null);
                            Focus();
                            ScrollIntoView(SelectedItem);
                        });
                        ib.Command = c;
                    }
                }
            }

            ((INotifyCollectionChanged)Items).CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs) {
            switch (notifyCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Reset: {
                    if (notifyCollectionChangedEventArgs.OldItems == null && notifyCollectionChangedEventArgs.NewItems == null) {
                        _justReloaded = true;
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Add: {
                    if (_justReloaded) {
                        if (Items.Count > 0 && Items[0] != null) {
                            ScrollIntoView(Items[0]);
                            Focus();
                            _justReloaded = false;
                            
                        }
                    }
                    if (notifyCollectionChangedEventArgs.NewStartingIndex == 0) {
                        if (Items.Count > 0 && Items[0] != null) {
                            ScrollIntoView(Items[0]);
                        }
                    }
                    if (ScrollToLastLog)
                    {
                        int lastIndex = Items.Count - 1;
//                        if (_meatspin%10 == 0)
//                        {
                            ScrollIntoView(Items[lastIndex]);
//                        }
//                        _meatspin ++;
                    }
                    break;
                }
                case NotifyCollectionChangedAction.Remove: {

                    break;
                }
            }
        }
//        private static int _meatspin = 0;
        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs mouseWheelEventArgs) {
            int top, bottom;
            bool success = GetViewPortIndexes(this, out top, out bottom);
            if (!success) {
                return;
            }

            if (top == 0 && mouseWheelEventArgs.Delta > 0) {
                Log9KUtil.ExecuteCommand(LoadOlderLogCommand);
                Log9KUtil.ExecuteCommand(StartRemovingFromBottomCommand);
                return;
            }
            if (bottom != Items.Count || mouseWheelEventArgs.Delta >= 0) {
                return;
            }
            Log9KUtil.ExecuteCommand(LoadNewerLogCommand);
            Log9KUtil.ExecuteCommand(StartRemovingFromTopCommand);
        }

        private static bool GetViewPortIndexes(DataGrid dataGrid, out int indexTop, out int indexBottom) {
            ScrollViewer scrollViewer = GetVisualChild<ScrollViewer>(dataGrid);
            if (scrollViewer == null) {
                indexTop = 0;
                indexBottom = 0;
                return false;
            }
            indexTop = (int)scrollViewer.VerticalOffset;
            indexBottom = (int)scrollViewer.ViewportHeight + indexTop;
            return true;
        }
        
        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++) {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null) {
                    child = GetVisualChild<T>(v);
                }
                if (child != null) {
                    break;
                }
            }
            return child;
        }
        
        #endregion


    }
    
    #endregion


    #region Value Converters

    /// <summary>
    /// Converts TabTypes to corresponding icon uri
    /// </summary>
    class TypeToIconSourceConverter : IValueConverter {
        const string ICON_URI_PREFIX = "pack://application:,,,/LogControl9K;component/Windows/Controls/img/";
        const string ICON_EXTENSION = ".png";
        const string DEFAULT_ICON_URI_STRING = ICON_URI_PREFIX + "default" + ICON_EXTENSION;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            Uri defaultIconUri = new Uri(DEFAULT_ICON_URI_STRING);

            if (!(value is TabTypes)) {
                return defaultIconUri;
            }

            TabTypes tabType = (TabTypes) value;
            string tabTypeString = tabType.ToString();
            tabTypeString = tabTypeString.ToLower();
            string iconUriString = ICON_URI_PREFIX + tabTypeString + ICON_EXTENSION;
            return new Uri(iconUriString);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts TabTypes or TabTypes to corresponding color
    /// </summary>
    class TypeToColorConverter : IValueConverter, INotifyPropertyChanged {

        private const string COLORS_FILENAME = "Colors.txt";


        
        #region Constructor

        public TypeToColorConverter() {
        }

        #endregion


        #region Public methods
        
        public static Brush FromColorString(string colorString) {
            object o;
            try {
                o = ColorConverter.ConvertFromString(colorString);
            } catch (FormatException) {
                return null;
            }
            if (o == null) {
                return null;
            }
            Color color = (Color)o;
            Brush b = new SolidColorBrush(color);
            return b;
        }
            
        #region IValueConverter

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

            TabTypes type;
            string customType;
            if (!GetTabType(value, out type, out customType)) {
                return Brushes.Tomato;
            }

            switch (type) {
                case TabTypes.ALL:
                    return Log9KCore.Settings.DefaultBrush;

                case TabTypes.INFO:
                    return Log9KCore.Settings.InfoBrush;
                case TabTypes.ERROR:
                    return Log9KCore.Settings.ErrorBrush;
                case TabTypes.CRITICAL_ERROR:
                    return Log9KCore.Settings.CriticalErrorBrush;
                case TabTypes.SUCCESS:
                    return Log9KCore.Settings.SuccessBrush;
                case TabTypes.CODE_FLOW:
                    return Log9KCore.Settings.CodeFlowBrush;
                case TabTypes.DEBUG:
                    return Log9KCore.Settings.DebugBrush;

                case TabTypes.CUSTOM:
                    if (customType == Log9KCore.INNER_LOG_TAB) {
                        return Log9KCore.Settings.InnerBrush;
                    }
                    return Log9KCore.Settings.CustomBrush;

                default:
                    return Log9KCore.Settings.DefaultBrush;
            }
        }

        static bool GetTabType(object value, out TabTypes type, out string customType) {
            type = TabTypes.ALL;
            customType = "";

            if (value == null || (!(value is TabTypes) && !(value is LogEntryTypes) && !(value is Log9KEntry) && !(value is Log9KTab))) {
                return false;
            }

            if (value is TabTypes) {
                type = (TabTypes) value;
            } 
            if (value is LogEntryTypes) {
                bool success = Enum.TryParse(value.ToString(), out type);
                if (!success) {
                    return false;
                }
            }

            if (value is Log9KEntry) {
                Log9KEntry e = (Log9KEntry) value;
                if (e.Type == LogEntryTypes.CUSTOM) {
                    customType = e.CustomType;
                }
                bool success = Enum.TryParse(e.Type.ToString(), out type);
                if (!success) {
                    return false;
                }
            }

            if (value is Log9KTab) {
                Log9KTab t = (Log9KTab) value;
                if (t.TabType == TabTypes.CUSTOM) {
                    customType = t.CustomTabType;
                }
                type = t.TabType;
            }

            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotImplementedException();
        }

        #endregion
       
 
        #endregion


        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


    }

    #endregion


}
