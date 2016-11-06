using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using LogControl9K.Windows.Util;
using Microsoft.Win32;

namespace LogControl9K.Windows.Controls.ViewModels {
    /// <summary>
    /// Main ViewModel of Log9KControl
    /// </summary>
    internal class Log9KControlViewModel : ViewModelBase {

        
        #region Properties


        #region Core

        /// <summary>
        /// Field for property <see cref="Core"/>
        /// </summary>
        private static Log9KCore _core = Log9KCore.Instance;
        /// <summary>
        /// Log9KCore Instance
        /// </summary>
        public static Log9KCore Core { get { return _core; } }

        #endregion


        #region Tabs stuff
        
        private Dictionary<string, Log9KEntry> _selectedEntryForTab = new Dictionary<string, Log9KEntry>();

        /// <summary>
        /// Field for property <see cref="LogEntriesCollectionView"/>
        /// </summary>
        private ICollectionView _logEntriesCollectionView;
        /// <summary>
        /// CollectionView for tab, updates if CurrentTab changed 
        /// </summary>
        public ICollectionView LogEntriesCollectionView {
            get { return _logEntriesCollectionView; }
            private set {
                _logEntriesCollectionView = value;
                OnPropertyChanged("LogEntriesCollectionView");
            }
        }

        /// <summary>
        /// ObservableCollection of enabled tabs, ItemsSource for TabControl
        /// </summary>
        public ObservableCollection<Log9KTab> LogTabs { get; private set; }
        
        /// <summary>
        /// Field for property <see cref="CurrentTab"/>
        /// </summary>
        private Log9KTab _currentTab;
        // Current displayed log tab, proxy for Log9KCore.CurrentTab
        public Log9KTab CurrentTab {
            get {
                return _currentTab;
            }
            set {
                _currentTab = value;
                if (_currentTab != null) {
                    if (_currentTab.IsAllTab()) {
                        IsAllTabCurrent = true;
                    } else {
                        IsAllTabCurrent = false;
                    }
                }
                OnPropertyChanged("CurrentTab");
            }
        }
        
        /// <summary>
        /// Tab ALL
        /// </summary>
        public Log9KTabAll TabAll {
            get { return Log9KCore.Instance.TabAll; }
        }
        
        /// <summary>
        /// Field for property <see cref="IsAllTabCurrent"/>
        /// </summary>
        private bool _isAllTabCurrent;
        /// <summary>
        /// Is current tab is ALL type tab
        /// </summary>
        public bool IsAllTabCurrent {
            get {
                return _isAllTabCurrent;
            }
            set {
                _isAllTabCurrent = value; 
                OnPropertyChanged("IsAllTabCurrent");
            }
        }

        #endregion



        
            
        #region Show/hidden


        #region Settings show/hidden

        private string _settingsButtonContent;
        /// <summary>
        /// Content of button which hides/shows settings
        /// </summary>
        public string SettingsButtonContent {
            get { return _settingsButtonContent; }
            private set {
                _settingsButtonContent = value;
                OnPropertyChanged("SettingsButtonContent");
            }
        }


        private bool _showSettings;
        /// <summary>
        /// Is settings showing or not
        /// </summary>
        public bool ShowSettings {
            get { return _showSettings; }
            private set {
                SettingsButtonContent = value ? ">" : "<";
                _showSettings = value;
                OnPropertyChanged("ShowSettings");
            }
        }
        
        #endregion

        
        /// <summary>
        /// Field for property <see cref="ShowDuplicationsTree"/>
        /// </summary>
        private bool _showDuplicationsTree;
        /// <summary>
        /// Is settings showing or not
        /// </summary>
        public bool ShowDuplicationsTree {
            get { return _showDuplicationsTree; }
            set {
                _showDuplicationsTree = value;
                OnPropertyChanged("ShowDuplicationsTree");
            }
        }

        /// <summary>
        /// Field for property <see cref="ShowDataGridControls"/>
        /// </summary>
        private bool _showDataGridControls;
        /// <summary>
        /// Is settings showing or not
        /// </summary>
        public bool ShowDataGridControls {
            get { return _showDataGridControls; }
            private set {
                _showDataGridControls = value;
                OnPropertyChanged("ShowDataGridControls");
            }
        }
        
        #endregion


        #region Commands
        
        
        #region DataGrid Commands
        
        /// <summary>
        /// Let the given entry be Selected
        /// </summary>
        public ICommand SelectEntryCommand { get; private set; }
        
        /// <summary>
        /// Select given log entry in DataGrid
        /// </summary>
        public ICommand SelectLogEntryCommand { get; private set; }

        /// <summary>
        /// Filter the log entries by given time period
        /// </summary>
        public ICommand FilterByTimeCommand { get; private set; }
        
        /// <summary>
        /// Cancel time filter
        /// </summary>
        public ICommand CancelFilterByTimeCommand { get; private set; }
        
        /// <summary>
        /// Copy log entries to clipboard
        /// </summary>
        public ICommand CopyEntriesCommand { get; private set; }

        /// <summary>
        /// Save log entries to file
        /// </summary>
        public ICommand SaveEntriesCommand { get; private set; }
        
        /// <summary>
        /// Show or hide data grid's controls 
        /// </summary>
        public ICommand ShowOrHideDataGridControlsCommand { get; private set; }

        /// <summary>
        /// Show or hid duplications tree
        /// </summary>
        public ICommand ShowOrHideDuplicationsTreeCommand { get; private set; }

        /// <summary>
        /// Show or hide InnerLog tab
        /// </summary>
        public ICommand IsInnerLogVisibleCommand { get; private set; }

        /// <summary>
        /// Start removing from bottom of LogEntryCollection of tab 
        /// </summary>
        public ICommand StartRemovingFromBottomCommand { get; private set; }

        /// <summary>
        /// Start removing from top of LogEntryCollection of tab 
        /// </summary>
        public ICommand StartRemovingFromTopCommand { get; private set; }
        
        /// <summary>
        /// Load one older log 
        /// </summary>
        public ICommand LoadOlderLogCommand { get; private set; }

        /// <summary>
        /// Load one newer log
        /// </summary>
        public ICommand LoadNewerLogCommand { get; private set; }
        
        /// <summary>
        /// Load bunch of older logs
        /// </summary>
        public ICommand LoadOlderLogArrayCommand { get; private set; }

        /// <summary>
        /// Load bunch of newer logs
        /// </summary>
        public ICommand LoadNewerLogArrayCommand { get; private set; }
        
        /// <summary>
        /// Sort LogEntryCollection of tab with bubble sort
        /// </summary>
        public ICommand SortLogEntriesCollectionCommand { get; private set; }
        
        /// <summary>
        /// Show last N (max size of tab collection) logs
        /// </summary>
        public ICommand ShowLastLogsCommand { get; private set; }
        
        /// <summary>
        /// Switch show last logs checkbox
        /// </summary>
        public ICommand SwitchShowLastLogsCommand { get; private set; }

        /// <summary>
        /// Switch scroll to last log checkbox
        /// </summary>
        public ICommand SwitchScrollToLastLogCommand { get; private set; }
        
        /// <summary>
        /// Load logs from temp file starting from given ID
        /// </summary>
        public ICommand LoadStartingFromIDCommand { get; private set; }
        
        /// <summary>
        /// Clear data grid log entries collection
        /// </summary>
        public ICommand ClearDataGridCommand { get; private set; }
        
        #endregion


        public ICommand FocusStartFromIDCommand { get; private set; }
        public ICommand FocusEndTimeFilterCommand { get; private set; }
        public ICommand FocusStartTimeFilterCommand { get; private set; }

        public ICommand SwitchTabToNextCommand { get; private set; }

        public ICommand SwitchTabToPreviousCommand { get; private set; }

        /// <summary>
        /// Show or hide settings groupbox
        /// </summary>
        public ICommand ShowOrHideSettingsCommand { get; private set; }

        /// <summary>
        /// Change color for log entry type
        /// </summary>
        public ICommand ChangeColorCommand { get; private set; }

        /// <summary>
        /// Set default colors for log entry types
        /// </summary>
        public ICommand DefaultColorsCommand { get; private set; }
        
        /// <summary>
        /// Save settings to file
        /// </summary>
        public ICommand SaveSettingsCommand { get; private set; }
        
        /// <summary>
        /// Load settings from file
        /// </summary>
        public ICommand LoadSettingsCommand { get; private set; }


        #endregion
        
        
        #region Selected entries stuff

        private LogEntryTypes _selectedTypeForColor;
        public LogEntryTypes SelectedTypeForColor {
            get { return _selectedTypeForColor; }
            set {
                _selectedTypeForColor = value;
                OnPropertyChanged("SelectedTypeForColor");
            }
        }
        
        private Log9KEntry _selectedEntry;
        public Log9KEntry SelectedEntry {
            get { return _selectedEntry; }
            set {

                // Save selected entry for given tab in dictionary
                if (CurrentTab != null && value != null) {
                    if (!_selectedEntryForTab.ContainsKey(CurrentTab.TabTypeString)) {
                        _selectedEntryForTab.Add(CurrentTab.TabTypeString, value);
                    } else {
                        _selectedEntryForTab[CurrentTab.TabTypeString] = value;
                    }
                }

                _selectedEntry = value;
                OnPropertyChanged("SelectedEntry");
            }
        }

        private Log9KEntry[] _selectedEntries;
        public Log9KEntry[] SelectedEntries {
            get { return _selectedEntries; }
            set {
                _selectedEntries = value;
                OnPropertyChanged("SelectedEntries");
            }
        }

        #endregion
        
        
        /// <summary>
        /// Field for property <see cref="IsStartFromIDTextBoxFocused"/>
        /// </summary>
        private bool _isStartFromIDTextBoxFocused;
        /// <summary>
        /// Is text box for loading starting from ID is focused
        /// </summary>
        public bool IsStartFromIDTextBoxFocused {
            get { return _isStartFromIDTextBoxFocused; }
            set {
                _isStartFromIDTextBoxFocused = value;
                OnPropertyChanged("IsStartFromIDTextBoxFocused");
            }
        }

        /// <summary>
        /// Field for property <see cref="IsEndTimeFilterFocused"/>
        /// </summary>
        private bool _isEndTimeFilterFocused;
        /// <summary>
        /// Is end time filter text box focused
        /// </summary>
        public bool IsEndTimeFilterFocused {
            get { return _isEndTimeFilterFocused; }
            set {
                _isEndTimeFilterFocused = value;
                OnPropertyChanged("IsEndTimeFilterFocused");
            }
        }
        
        /// <summary>
        /// Field for property <see cref="IsStartTimeFilterFocused"/>
        /// </summary>
        private bool _isStartTimeFilterFocused;
        /// <summary>
        /// Is start time filter text box focused
        /// </summary>
        public bool IsStartTimeFilterFocused {
            get { return _isStartTimeFilterFocused; }
            set {
                _isStartTimeFilterFocused = value;
                OnPropertyChanged("IsStartTimeFilterFocused");
            }
        }
        

        public Log9KSettings Settings { get { return Log9KCore.Settings; } }

        private IEnumerable _logEntryTypes = Log9KUtil.EnumUtil.GetValues<LogEntryTypes>();
        public IEnumerable LogEntryTypes { get { return _logEntryTypes; } }

        private string _pageUpDownToLoad = 10.ToString();
        public string PageUpDownToLoad {
            get { return _pageUpDownToLoad; }
            set {
                _pageUpDownToLoad = value;
                OnPropertyChanged("PageUpDownToLoad");
            }
        }

        /// <summary>
        /// Field for property <see cref="StartFromIDText"/>
        /// </summary>
        private string _startFromIDText;
        /// <summary>
        /// Start from ID text box text
        /// </summary>
        public string StartFromIDText {
            get { return _startFromIDText; }
            set {
                _startFromIDText = value;
                OnPropertyChanged("StartFromIDText");
            }
        }

        
        #region Time filter

        private string _filterTimeStart;
        public string FilterTimeStart {
            get { return _filterTimeStart; }
            set {
                _filterTimeStart = value;
                OnPropertyChanged("FilterTimeStart");
            }
        }


        private string _filterTimeEnd;
        public string FilterTimeEnd {
            get { return _filterTimeEnd; }
            set {
                _filterTimeEnd = value;
                OnPropertyChanged("FilterTimeEnd");
            }
        }

        public object IsDebug {
            get {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }


        #endregion
        

        #endregion


        #region Fields 

        private DateTime _startDateTime;
        private DateTime _endDateTime;
        private bool _timeStartSuccess;
        private bool _timeEndSuccess;

        #endregion


        #region Constructor

        public Log9KControlViewModel() {
            InitCommands();

            LogTabs = Log9KCore.Instance.Log9KTabsCollection;

            ShowSettings = false;
            ShowDataGridControls = false;
            PropertyChanged += OnPropertyChanged;
            Core.PropertyChanged += CoreOnPropertyChanged;
        }


        private void InitCommands() {
            SelectEntryCommand = new sCommand(SelectLogEntryAction);
            FilterByTimeCommand = new sCommand(FilterByTimeAction);
            CancelFilterByTimeCommand = new sCommand(CancelFilterByTimeAction);
            CopyEntriesCommand = new sCommand(CopyEntriesAction);
            SaveEntriesCommand = new sCommand(SaveEntriesAction);
            ShowOrHideSettingsCommand = new sCommand(ShowOrHideSettingsAction);
            ShowOrHideDataGridControlsCommand = new sCommand(ShowOrHideDataGridControlsAction);
            ShowOrHideDuplicationsTreeCommand = new sCommand(ShowOrHideDuplicationsTreeAction);
            StartRemovingFromBottomCommand = new sCommand(StartRemovingFromBottomAction);
            StartRemovingFromTopCommand = new sCommand(StartRemovingFromTopAction);
            LoadOlderLogCommand = new sCommand(LoadOlderLogAction);
            LoadOlderLogArrayCommand = new sCommand(LoadOlderLogArrayAction);
            LoadNewerLogCommand = new sCommand(LoadNewerLogAction);
            LoadNewerLogArrayCommand = new sCommand(LoadNewerLogArrayAction);
            SortLogEntriesCollectionCommand = new sCommand(SortLogEntriesCollectionAction);
            ShowLastLogsCommand = new sCommand(ShowLastLogsAction);
            SwitchShowLastLogsCommand = new sCommand(SwitchShowLastLogsAction);
            SwitchScrollToLastLogCommand = new sCommand(SwitchScrollToLastLogAction);
            LoadStartingFromIDCommand = new sCommand(LoadStartingFromIDAction);
            SelectLogEntryCommand = new sCommand(SelectLogEntryAction);
            IsInnerLogVisibleCommand = new sCommand(IsInnerLogVisibleAction);
            ClearDataGridCommand = new sCommand(ClearDataGridAction);
            SaveSettingsCommand = new sCommand(SaveSettingsAction);
            LoadSettingsCommand = new sCommand(LoadSettingsAction);
            ChangeColorCommand = new sCommand(ChangeColorAction);
            DefaultColorsCommand = new sCommand(DefaultColorsAction);
            SwitchTabToNextCommand = new sCommand(SwitchTabToNextAction);
            SwitchTabToPreviousCommand = new sCommand(SwitchTabToPreviousAction);
            FocusEndTimeFilterCommand = new sCommand(FocusEndTimeFilterAction);
            FocusStartTimeFilterCommand = new sCommand(FocusStartTimeFilterAction);
            FocusStartFromIDCommand = new sCommand(FocusStartFromIDAction);
        }


        #endregion
        

        #region Private methods


        #region PropertyChanged

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
            if (propertyChangedEventArgs.PropertyName == "CurrentTab") {
                if (CurrentTab == null) {
                    return;
                }
                if (_selectedEntryForTab.ContainsKey(CurrentTab.TabTypeString)) {
                    SelectedEntry = _selectedEntryForTab[CurrentTab.TabTypeString];
                }
                LogEntriesCollectionView = CollectionViewSource.GetDefaultView(CurrentTab.LogEntryCollection);
            }
        }
        
        private void CoreOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs) {
            Log9KCore core = sender as Log9KCore;
            if (core == null) {
                return;
            }
            if (propertyChangedEventArgs.PropertyName == "CurrentTab") {
                if (core.CurrentTab == null) {
                    return;
                }
                CurrentTab = core.CurrentTab;
            }
        }

        #endregion

        
        private void SwitchScrollToLastLogAction() {
            CurrentTab.ScrollToLastLog = !CurrentTab.ScrollToLastLog;
        }

        private void SwitchShowLastLogsAction() {
            if (CurrentTab.IsAddingNewestEntries) {
                CurrentTab.IsAddingNewestEntries = false;
            } else {
                CurrentTab.IsAddingNewestEntries = true;
                ShowLastLogsAction(true);
            }
        }

        private void ShowOrHideDuplicationsTreeAction() {
            if (ShowDuplicationsTree) {
                ShowDuplicationsTree = false;
            } else {
                ShowDuplicationsTree = true;
            }
        }
       
        private void SwitchTabToNextAction() {
            if (LogTabs.Count < 2) {
                return;
            }
            int i = LogTabs.IndexOf(CurrentTab);
            if (i == -1) {
                return;
            }
            i++;
            if (i >= LogTabs.Count) {
                i = 0;
            }
            CurrentTab = LogTabs[i];
        }
        
        private void SwitchTabToPreviousAction() {
            if (LogTabs.Count < 2) {
                return;
            }
            int i = LogTabs.IndexOf(CurrentTab);
            if (i == -1) {
                return;
            }
            i--;
            if (i < 0) {
                i = LogTabs.Count-1;
            }
            CurrentTab = LogTabs[i];
        }

        private void FocusEndTimeFilterAction() {
            IsEndTimeFilterFocused = false;
            IsEndTimeFilterFocused = true;
        }
        
        private void FocusStartTimeFilterAction() {
            if (!ShowDataGridControls) {
                ShowDataGridControls = true;
            }
            IsStartTimeFilterFocused = false;
            IsStartTimeFilterFocused = true;
        }

        private void FocusStartFromIDAction() {
            if (!ShowDataGridControls) {
                ShowDataGridControls = true;
            }
            IsStartFromIDTextBoxFocused = false;
            IsStartFromIDTextBoxFocused = true;
            StartFromIDText = "";
        }

        #region Settings

        private void LoadSettingsAction() {
            Core.LoadSettings();
            UpdateLogEntriesCollectionView();
        }

        private void SaveSettingsAction() {
            Core.SaveSettings();
        }
        
        private void DefaultColorsAction() {
            Settings.SetDefaultBrushes();
            UpdateLogEntriesCollectionView();
        }
        
        private void ChangeColorAction(object colorString) {
            object convertFromString = ColorConverter.ConvertFromString((string)colorString);
            if (convertFromString != null) {
                Color color = (Color)convertFromString;
                SolidColorBrush brush = new SolidColorBrush(color);
             
                Settings.ChangeTypeColor(SelectedTypeForColor, brush);
            }

            UpdateLogEntriesCollectionView();
        }

        private void UpdateLogEntriesCollectionView() {
            ICollectionView a = LogEntriesCollectionView;
            LogEntriesCollectionView = null;
            LogEntriesCollectionView = a;
        }
        
        #endregion


        #region DataGrid

        private void ClearDataGridAction() {
            CurrentTab.LogEntryCollection.Clear();
        }

        private void LoadStartingFromIDAction(object id) {
            if (!CurrentTab.IsAllTab()) {
                return;
            }
            uint startFromID;
            uint i = id is uint ? (uint) id : 0;
            if (i == 0) {
                string s = id as string;
                if (s == null) {
                    return;
                }
                if (!uint.TryParse(s, out startFromID)) {
                    return;
                }
            } else {
                startFromID = i;
            }

            var da = CurrentTab.LoadEntries(startFromID);
            // let's select loaded entry
            if (da != null) {
                da.Completed += (sender, args) => {
                    foreach (Log9KEntry entry in LogEntriesCollectionView.Cast<Log9KEntry>().Where(entry => entry.ID == startFromID)) {
                        SelectedEntry = entry;
                    }
                };
                
            }
        }

        private void LoadOlderLogArrayAction() {
            Log9KUtil.ExecuteCommand(StartRemovingFromBottomCommand);
            int pageUpDownToLoad;
            bool success = int.TryParse(PageUpDownToLoad, out pageUpDownToLoad);
            if (!success) {
                pageUpDownToLoad = 10;
            }
            for (int i = 0; i < pageUpDownToLoad; i++) {
                CurrentTab.LoadOlderLogEntry();
            }
            SelectedEntry = CurrentTab.LogEntryCollection[0];
        }
        
        private void LoadNewerLogArrayAction() {
            Log9KUtil.ExecuteCommand(StartRemovingFromTopCommand);
            int pageUpDownToLoad;
            bool success = int.TryParse(PageUpDownToLoad, out pageUpDownToLoad);
            if (!success) {
                pageUpDownToLoad = 10;
            }
            for (int i = 0; i < pageUpDownToLoad; i++) {
                CurrentTab.LoadNewerLogEntry();
            }
            SelectedEntry = CurrentTab.LogEntryCollection[CurrentTab.LogEntryCollection.Count - 1];
        }

        private void SortLogEntriesCollectionAction() {
            CurrentTab.LogEntryCollection.BubbleSort();
        }

        private void StartRemovingFromBottomAction() {
            CurrentTab.LogEntryCollection.StartRemovingFromBottom();
        }

        private void StartRemovingFromTopAction() {
            CurrentTab.LogEntryCollection.StartRemovingFromTop();
        }
        
        private const int TRIES = 25;
        private void LoadOlderLogAction() {
            for (int i = 0; i < TRIES; i++) {
                bool s = CurrentTab.LoadOlderLogEntry();
                if (s) {
                    break;
                }
            }
        }

        private void LoadNewerLogAction() {
            CurrentTab.LoadNewerLogEntry();
        }

        #endregion


        #region Show/hide
        
        private void IsInnerLogVisibleAction(object obj) {
            if (!(obj is bool)) {
                return;
            }
            bool b = (bool) obj;
            Log9KCore.Instance.SwitchInnerLogTabVisibility(b);
        }

        private void ShowOrHideDataGridControlsAction() {
            ShowDataGridControls = !ShowDataGridControls;
        }

        private void ShowLastLogsAction(object o) {
            if (!(o is bool)) {
                return;
            }
            bool b = (bool) o;
            if (b) {
                CurrentTab.LoadLastLogs();
            }
            CurrentTab.IsAddingNewestEntries = b;
        }

        private void SelectLogEntryAction(object entry) {
            Log9KEntry log9KEntry = entry as Log9KEntry;
            if (log9KEntry != null) {
                int d = log9KEntry.GetDuplicationHash();
                var a = CurrentTab.DuplicationsDictionary[d];
//                SelectedEntry = log9KEntry;
            }
        }

        private void ShowOrHideSettingsAction() {
            ShowSettings = !ShowSettings;
        }

        #endregion


        #region Copy and save entries
        
        /// <summary>
        /// Copy selected entries to clipboard
        /// </summary>
        /// <param name="o"></param>
        private void CopyEntriesAction(object o) {
            string clipboard = GetStringOfSelectedEntries(o);
            Clipboard.SetText(clipboard);            
        }
        
        /// <summary>
        /// Manual save selected entries to file
        /// </summary>
        /// <param name="o"></param>
        private void SaveEntriesAction(object o) {
            string s = GetStringOfSelectedEntries(o);
            SaveFileDialog sfd = new SaveFileDialog {
                AddExtension = true, 
                DefaultExt = ".log", 
                CheckPathExists = true, 
                Filter = "Log Files|*.log", 
                InitialDirectory = Path.GetFullPath(Settings.Folder)
            };
            sfd.ShowDialog();
            string filename = sfd.FileName;
            if (filename == "") {
                return;
            }
            if (File.Exists(filename)) {
                File.Delete(filename); 
            }
            File.WriteAllText(filename, s);
        }

        /// <summary>
        /// Get selected entries as one string
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private string GetStringOfSelectedEntries(object o) {
            string s = "";
            IList selectedEntries = GetSelectedEntries(o);
            if (selectedEntries == null) {
                return "";
            }
            foreach (Log9KEntry e in selectedEntries) {
                s += e + Environment.NewLine;
            }
            return s;
        }
        
        /// <summary>
        /// Checks is given object a list and returns it casted if so
        /// </summary>
        /// <param name="o"></param>
        /// <returns>returns list of selected log entries</returns>
        private IList GetSelectedEntries(object o) {
            if (o == null) {
                return null;
            }
            IList selectedEntries = o as IList;

            if (selectedEntries == null) {
                return null;
            }
            return selectedEntries;
        }

        #endregion


        #region Time filter

        private void CancelFilterByTimeAction() {
            if (CurrentTab.IsAllTab()) {
                // do nothing
            } else {
                LogEntriesCollectionView.Filter = null;
            }
        }

        /// <summary>
        /// Set time filter for collection view
        /// </summary>
        private void FilterByTimeAction(object timeEnd) {
            bool a = false;
            if (timeEnd != null) {
                a = timeEnd is string;
            }

            if (!a) {
                _timeEndSuccess = DateTime.TryParse(FilterTimeEnd, out _endDateTime);
            } else {
                _timeEndSuccess = DateTime.TryParse((string)timeEnd, out _endDateTime);
            }

            _timeStartSuccess = DateTime.TryParse(FilterTimeStart, out _startDateTime);
            if (!(_timeStartSuccess || _timeEndSuccess)) {
                LogEntriesCollectionView.Filter = null;
                return;
            }
            if (!_timeEndSuccess) {
                _endDateTime = DateTime.MaxValue;
            }
            if (CurrentTab.IsAllTab()) {
                CurrentTab.FilterByTime(_startDateTime, _endDateTime);
            } else {
                LogEntriesCollectionView.Filter = Filter;    
            }
        }
        

        /// <summary>
        /// Time filter for collection view
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private bool Filter(object o) {
            if (!(o is Log9KEntry)) {
                return false;
            }
            Log9KEntry entry = (Log9KEntry) o;
            DateTime entryTime= entry.Time.ToDateTime();
            if (!_timeStartSuccess) {
                if (entryTime.CompareTo(_endDateTime) <= 0) {
                    return true;
                }
            }
            if (!_timeEndSuccess) {
                if (entryTime.CompareTo(_startDateTime) >= 0) {
                    return true;
                }
            }
            if (entryTime.CompareTo(_startDateTime) >= 0 && (entryTime.CompareTo(_endDateTime) <= 0)) {
                return true;
            }
            return false;
        }

        #endregion


        #endregion


    }
}