using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using LogControl9K.Annotations;

namespace LogControl9K {

    using DuplicationNode = Tuple<Log9KEntry, ObservableCollection<Tuple<Log9KTime, uint>>>;
    using DuplicationLeaf = Tuple<Log9KTime, uint>;

    #region Enums

    /// <summary>
    /// <para>Types for tabs (is copy-paste of LogEntryTypes + ALL)</para>
    /// <remarks>To add new type add type here and here:<see cref="LogEntryTypes"/> </remarks>
    /// </summary>
    public enum TabTypes : byte {
        /// <summary>
        /// Info tab type
        /// </summary>
        INFO = 0,
        /// <summary>
        /// Error tab type
        /// </summary>
        ERROR,
        /// <summary>
        /// Critical error tab type
        /// </summary>
        CRITICAL_ERROR,
        /// <summary>
        /// Success tab type
        /// </summary>
        SUCCESS,
        /// <summary>
        /// Code flow tab type
        /// </summary>
        CODE_FLOW,
        /// <summary>
        /// User defined tab type 
        /// </summary>
        CUSTOM,
        /// <summary>
        /// Debug tab type
        /// </summary>
        DEBUG, 
        /// <summary>
        /// Warning tab type
        /// </summary>
        WARNING, 
        /// <summary>
        /// All tab type
        /// </summary>
        ALL
    }

    #endregion

        
    /// <summary>
    /// Class for one tab logic. 
    /// Includes method for adding new log entry, method for loading older logs from file.
    /// </summary>
    public class Log9KTab : INotifyPropertyChanged {


        #region Constants 
        
        /// <summary>
        /// Default size of ObservableCollection for any tab
        /// </summary>
        protected const uint DEFAULT_MAX_COLLECTION_SIZE = 20000; 

        #endregion
        

        #region Fields, properties 


        #region Files related

        /// <summary>
        /// Directory name for tab
        /// </summary>
        private string _dirname;

        private string _filenameLogFile;
        /// <summary>
        /// File name for file with human readable log */
        /// </summary>
        public string FilenameLogFile {
            get {
                return _filenameLogFile;
            }
            set {
                string fullpath = value;
                _filenameLogFile = fullpath;
                OnPropertyChanged("FilenameLogFile");
            } 
        }

 
        /// <summary>
        /// File name for file with temporary data which 
        /// is used to load older logs in CurrentLogEntryCollection */
        /// </summary>
        public string FilenameTempFile { get; protected set; }
        
        /// <summary>
        /// Current line number in temporary file for top of user view (datagrid)
        /// </summary>
        public uint LineNumberTopTempFile { get; protected set; }

        /// <summary>
        /// Current line number in temporary file for bottom of user view (datagrid)
        /// </summary>
        public uint LineNumberBottomTempFile {
            get { return (uint) (LineNumberTopTempFile + LogEntryCollection.Count - 1); }
            set { LineNumberTopTempFile = (uint) (value - LogEntryCollection.Count + 1); }
        }

        #endregion


        #region Tab info

        /// <summary>
        /// Type of current tab
        /// </summary>
        public TabTypes TabType { get; protected set; }

        /// <summary>
        /// String representation of tab type, is ToString if not CUSTOM, otherwise
        /// </summary>
        public string TabTypeString { get; protected set; }

        /// <summary>
        /// Header for using in UI, set by resources
        /// </summary>
        public string TabHeader { get; set; }

        /// <summary>
        /// Name of custom tab
        /// </summary>
        public string CustomTabType { get; private set; }

        #endregion


        #region Collection related

        /// <summary>
        /// Collection, that shows in UI */
        /// </summary>
        // ReSharper disable once InconsistentNaming
        protected Log9KTabObservableCollection _logEntryCollection;

        private bool _isAddingNewestEntries = true;
        /// <summary>
        /// Adding new entries to collection or not
        /// </summary>
        public bool IsAddingNewestEntries {
            get {
                return _isAddingNewestEntries;
            }
            set {
                _isAddingNewestEntries = value;
                if (!value) {
                    ScrollToLastLog = false;
                }
                OnPropertyChanged("IsAddingNewestEntries");
            }
        }
        
        private bool _scrollToLastLog = true;
        /// <summary>
        /// Is autoscrolling enabled for this tab?
        /// </summary>
        public bool ScrollToLastLog {
            get { return _scrollToLastLog; }
            set {
                _scrollToLastLog = value;
                OnPropertyChanged("ScrollToLastLog");
            }
        }

        #endregion

        
        #endregion
       
 
        #region Properties
        
        /// <summary>
        /// Look at DuplicationsDictionary property
        /// </summary>
        protected Dictionary<int, DuplicationNode> _duplicationsDictionary;

        /// <summary>
        /// <para>Keys of dictionary are hashes of log entries </para>
        /// <para>and values are tuples of one log entry (original) and list of log entries (duplications)</para>
        /// </summary>
        public Dictionary<int, DuplicationNode> DuplicationsDictionary
        {
            get { return _duplicationsDictionary;}
            protected set { _duplicationsDictionary = value; }
        }

        /// <summary>
        /// Collection which stores log entries to show
        /// </summary>
        public Log9KTabObservableCollection LogEntryCollection {
            get { return _logEntryCollection; }
        }

        private ObservableCollection<DuplicationNode> _duplicationsCollection = new ObservableCollection<DuplicationNode>();
        private bool _directoryNotCreated = true;

        /// <summary>
        /// <para>Collection of duplications for binding</para>
        /// <para>(represents duplications dictionary, but have only duplications with 2 or more occurencies)</para>
        /// </summary>
        public ObservableCollection<DuplicationNode> DuplicationsCollection { 
            get { return _duplicationsCollection; }
        }

        #endregion


        #region Constructors and Initialization

        /// <summary>
        /// Main constructor 
        /// </summary>
        /// <param name="type"></param>
        public Log9KTab(TabTypes type) {
            TabType = type;
            Init(TabType);
        }

        /// <summary>
        /// <para>Constructor for CUSTOM tabs</para>
        /// <para>Конструктор для создания КАСТОМНОЙ вкладки (пользовательской), тип LogEntryType.CUSTOM</para>
        /// </summary>
        /// <param name="type">Название пользовательской вкладки</param>
        public Log9KTab(string type) {
            TabType = TabTypes.CUSTOM;
            CustomTabType = type;
            Init(TabType);
        }

        /// <summary>
        /// <para>Initialization method, used by constructors, creates folder for tab, inits filenames</para>
        /// <para>Метод инициализации, используемый всеми конструкторами. 
        /// Создаёт папку под вкладку, задаёт имена файлов.</para>
        /// </summary>
        /// <param name="type"></param>
        protected void Init(TabTypes type) {
            TabType = type;

            if (TabType != TabTypes.CUSTOM) {
                TabTypeString = type.ToString();
            } else {
                TabTypeString = CustomTabType;
            }

            if (IsAllTab()) {
                _dirname = Log9KCore.Settings.Folder + "/" + Log9KUtil.GetDayMonthYearString() + "/";
            } else if (IsInnerLogTab()) {
                _dirname = Log9KCore.Settings.Folder + "/" + Log9KUtil.GetDayMonthYearString() + "/" + "." + Log9KCore.INNER_LOG_TAB;
            } else {
                _dirname = Log9KCore.Settings.Folder + "/" + Log9KUtil.GetDayMonthYearString() + "/" + TabTypeString;
            }

            if (IsAllTab()) {
                Log9KUtil.CreateDirIfNotExists(_dirname);
                _directoryNotCreated = false;
            }
            
            FilenameLogFile = _dirname + "/" + Log9KUtil.GetDayMonthYearHourMinuteSecondString() + Log9KCore.LOG_EXTENSION;

            try {
                // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
                Path.GetFullPath(FilenameLogFile);
            } catch (Exception e) {
                Log9KCore.InnerLog("Ошибка: что-то не так с именем файла — " + e.Message, true); 
            }

            FilenameTempFile = FilenameLogFile + Log9KCore.TEMP_LOG_EXTENSION;

            DuplicationsDictionary = new Dictionary<int, DuplicationNode>();

            InitCollection();
            
            InitTabHeader();
        }
        
        /// <summary>
        /// Initialize tab header with default values from resources
        /// </summary>
        private void InitTabHeader() {
            if (TabType != TabTypes.CUSTOM) {
                string tabHeader = Log9KResources.ResourceManager.GetString(TabType.ToString());
                if (tabHeader != null) {
                    TabHeader = tabHeader;
                } else {
                    TabHeader = TabTypeString;
                }
            } else {
                TabHeader = CustomTabType;
            }
        }


        #endregion


        #region Methods for working with temporary file 

        /// <summary>
        /// Load entries that in given time period from temp file and add to collection
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public virtual void FilterByTime(DateTime start, DateTime end) {
            Log9KCore.Instance.InnerLog("Предупреждение: Метод FilterByTime не реализован для обычных вкладок");
        }
        
        /// <summary>
        /// Read log entry from temp file on given linenumber
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        protected Log9KEntry ReadEntry(uint lineNumber) {
            byte[] buffer;
            if (!Log9KUtil.ReadFixedSizeByteArrayEntry(FilenameTempFile, Log9KEntry.ENTRY_SIZE, lineNumber, out buffer)) {
                Log9KCore.Instance.InnerLog("Ошибка: Log9KUtil.ReadFixedSizeByteArrayEntry вернул false");
                return null;
            }
            Log9KEntry entry = EntryFromBytes(buffer);
            return entry;
        }


        #region LoadEntries method and related

        /// <summary>
        /// Load entries starting with given ID  
        /// <returns>
        /// <para>Returns DispatcherOperation which loads entries</para>
        /// <para>(for using RunWorkerCompleted</para>
        /// </returns>
        /// </summary>
        public virtual DispatcherOperation LoadEntries(uint startFromID) {
            Log9KCore.Instance.InnerLog("Предупреждение: Метод LoadEntries не реализован для обычных вкладок");
            return null;
        }


        /// <summary>
        /// Find line number in temporary file for given entry id
        /// </summary>
        /// <param name="startFromID">given entry id</param>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        protected bool FindLineNumber(uint startFromID, out uint lineNumber) {
            lineNumber = startFromID;
            Log9KEntry entry = ReadEntry(lineNumber);
            if (entry == null) {
                Log9KCore.Instance.InnerLog("Ошибка: метод ReadEntry(" + lineNumber + ") вернул null ");
                return false;
            }
            if (entry.ID != startFromID) {

                uint lineNumberUp = lineNumber;
                uint lineNumberDown = lineNumber;
                bool movingUp = true, movingDown = true;

                for (int i = 0; i < 500; i++) {
                    if (lineNumberUp > 0) {
                        lineNumberUp--;
                    } else {
                        movingUp = false;
                    }
                    lineNumberDown++;

                    if (movingUp) {
                        entry = ReadEntry(lineNumberUp);
                        if (entry == null) {
                            movingUp = false;
                        } else {
                            if (entry.ID == startFromID) {
                                lineNumber = lineNumberUp;
                                break;
                            }
                        }
                    }

                    if (movingDown) {
                        entry = ReadEntry(lineNumberDown);
                        if (entry == null) {
                            movingDown = false;
                        } else {
                            if (entry.ID == startFromID) {
                                lineNumber = lineNumberDown;
                                break;
                            }
                        }
                    }

                }                
            }

            return true;
        }
        
        #endregion



        /// <summary>
        /// Load one older log entry (previous for last in log entry collection)
        /// </summary>
        /// <returns></returns>
        public virtual bool LoadOlderLogEntry() {
            Log9KCore.Instance.InnerLog("Предупреждение: метод LoadOlderLogEntry не реализован для обычных вкладок");
            return false;
        }

        /// <summary>
        /// Load one newer log entry (next for last in log entry collection)
        /// </summary>
        /// <returns></returns>
        public virtual bool LoadNewerLogEntry() {
            Log9KCore.Instance.InnerLog("Предупреждение: метод LoadNewerLogEntry не реализован для обычных вкладок");
            return false;
        }

        #endregion

        
        #region Virtual methods
        
        /// <summary>
        /// Show last logs in collection
        /// </summary>
        public virtual void LoadLastLogs() { }
        
        /// <summary>
        /// For ALL tab and normal tab serialzation differs
        /// </summary>
        /// <param name="entryBytes"></param>
        /// <returns></returns>
        protected virtual Log9KEntry EntryFromBytes(byte [] entryBytes) {
            return Log9KEntry.FromByteArray(entryBytes);
        }

        /// <summary>
        /// Some tabs could have another collection type (derived from Log9KTabObservableCollection)
        /// </summary>
        protected virtual void InitCollection() {
            _logEntryCollection = new Log9KTabObservableCollection(DEFAULT_MAX_COLLECTION_SIZE); 
        }

        /// <summary>
        /// Serialization differs for ALL tab and normal tab
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        protected virtual byte[] EntryToBytes(Log9KEntry entry) {
            return entry.ToByteArray();
        }

        /// <summary>
        /// Is current tab is ALL tab
        /// </summary>
        /// <returns></returns>
        public virtual bool IsAllTab() {
            return false;
        }

        /// <summary>
        /// Is current tab is inner log purposes tab
        /// </summary>
        /// <returns></returns>
        internal bool IsInnerLogTab() {
            if (TabType == TabTypes.CUSTOM && CustomTabType == Log9KCore.INNER_LOG_TAB) {
                return true;
            }
            return false;
        }

        #endregion


        #region Public Methods: adding new log entry to tab

        /// <summary>
        /// Main Logging method in tab. Adds log entry to tab, checks duplications and other stuff
        /// </summary>
        /// <param name="entry"></param>
        public virtual void AddLogEntry(Log9KEntry entry) {

            // if type of tab/log entry not equal and we are not in ALL tab then return
            if (!AreLogEntryAndTabTypesEqual(entry) && !IsAllTab()) {
                Log9KCore.Instance.InnerLog("Ошибка: лог " + entry + " почему-то попал во вкладку " + TabType);
                return;
            }

            // Don't add inner logs to ALL tab
            if (IsAllTab() && 
                entry.Type == LogEntryTypes.CUSTOM && 
                entry.CustomType == Log9KCore.INNER_LOG_TAB) {

                return;
            }

            // Adding new log entries if it is enabled,
            // Adding to duplications dictionary in any way
            if (IsAddingNewestEntries) {
                AddToCollectionAndToDuplicationsDictionary(entry);
            } else {
                AddToDuplicationsDictionary(entry);
            }
            
            // Writing to temp file only for ALL tab
            if (IsAllTab()) {
                WriteToTempFile(entry);
            }
            
            WriteToLogFile(entry);

        }


        #endregion


        #region Protected Methods
        
        /// <summary>
        /// Find duplication entry for given entry
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        protected DuplicationNode LookupForDuplication(Log9KEntry entry) {
            int duplicationKey = KeyForDuplicationDictionary(entry);
            if (!DuplicationsDictionary.ContainsKey(duplicationKey)) {
                return null;
            }
            return DuplicationsDictionary[duplicationKey];
        }

        /// <summary>
        /// Writes ToString of entry to log file (logging)
        /// </summary>
        /// <param name="entry"></param>
        protected void WriteToLogFile(Log9KEntry entry) {
            if (IsInnerLogTab()) {
                AppendEntryToFile(entry);
                return;
            }

            if (!IsAllTab()) {
                if (Log9KCore.Settings.IsWritingEachTabEnabled) {
                    AppendEntryToFile(entry);
                }
            } else {
                if (Log9KCore.Settings.IsWritingToFileEnabled) {
                    AppendEntryToFile(entry);
                }
            }
        }
        
        /// <summary>
        /// Append log entry ToString to current tab's log file
        /// </summary>
        /// <param name="entry"></param>
        private void AppendEntryToFile(Log9KEntry entry) {
            if (_directoryNotCreated) {
                Log9KUtil.CreateDirIfNotExists(_dirname);
                _directoryNotCreated = false;
            }
            if (Application.Current == null) {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => {
                    Log9KUtil.AppendStringToFile(FilenameLogFile, entry.ToString());
                }), 
                DispatcherPriority.Background
            );
        }
        
        /// <summary>
        /// Writes bytes of entry to temporary file (serialization)
        /// </summary>
        /// <param name="entry">entry to write</param>
        protected void WriteToTempFile(Log9KEntry entry) {
            if (entry.Type == LogEntryTypes.CUSTOM && entry.CustomType == Log9KCore.INNER_LOG_TAB) {
                return;
            }
            byte[] entryBytes = EntryToBytes(entry);
            Log9KUtil.InvokeInUiThread(() => {
                Log9KUtil.AppendByteArrayToFile(FilenameTempFile, entryBytes);
            });
        }

        #endregion

        
        #region Private Methods


        #region Private Methods: Collection and Dictionary adding stuff

        /// <summary>
        /// First - add to collection and only after add to duplications dict
        /// </summary>
        /// <param name="entry"></param>
        private void AddToCollectionAndToDuplicationsDictionary(Log9KEntry entry) {
            if (LogEntryCollection.IsMaxSizeReached) {
                LineNumberTopTempFile++;
            }
            CollectionAdd(entry); 
            AddToDuplicationsDictionary(entry);
        }

        /// <summary>
        /// Function returns key, that used to be key in Dictionary in which
        /// duplications of log entries stored,
        /// in all tabs, which are not ALL tab key is log entry's message
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        protected int KeyForDuplicationDictionary(Log9KEntry entry) {
            return entry.GetDuplicationHash();
        } 

        /// <summary>
        /// Adds entry to duplications dictionary: 
        /// <para>if it is not met before creates new DuplicationEntry, 
        /// else adds time of given entry in list of existing DuplicationEntry</para>
        /// </summary>
        /// <param name="entry"></param>
        private void AddToDuplicationsDictionary(Log9KEntry entry) {
            if (entry.Level == Levels.SECONDARY) {
                return;
            }
            DuplicationNode duplicationEntry = LookupForDuplication(entry);
            if (duplicationEntry == null) {
                AddNewDuplicationEntry(entry, null);
            } else {
                entry.IsDuplicate = true;
                duplicationEntry.Item1.IsDuplicate = true;
                AddToDuplicationEntry(entry);
            }
        }

        #endregion


        #region Private Methods: methods-wrappers for UI thread invocation
        
        /// <summary>
        /// Remove at given index of log entry collection in UI thread
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private bool CollectionRemoveAt(int i) {
            if (Application.Current == null) {
                return false;
            }
            if (_logEntryCollection == null) {
                return false;
            }
            if ((i < 0) || (i >= _logEntryCollection.Count)) {
                return false;
            }
            Log9KUtil.InvokeInUiThread(() => {
                if ((i < 0) || (i >= _logEntryCollection.Count)) {
                    return;
                }
                _logEntryCollection.RemoveAt(i);
            });
            return true;
        }
        
        /// <summary>
        /// Add log entry to collection in UI thread
        /// </summary>
        /// <param name="entry"></param>
        protected void CollectionAdd(Log9KEntry entry) {
            if (Application.Current == null) {
                return;
            }
            if (_logEntryCollection == null) {
                return;
            }
            Log9KUtil.InvokeInUiThread(() => {
                _logEntryCollection.Add(entry);
            });
        }
        
        /// <summary>
        /// Insert log entry to given position, in UI thread
        /// </summary>
        /// <param name="i"></param>
        /// <param name="entry"></param>
        protected void CollectionInsert(int i, Log9KEntry entry) {
            if (Application.Current == null) {
                return;
            }
            if (_logEntryCollection == null) {
                return;
            }
            Log9KUtil.InvokeInUiThread(() => {
                _logEntryCollection.Insert(i, entry);
            });
        }
        
        /// <summary>
        /// Add duplicate to existing entry, for adding new duplication entry use AddNewDuplicationEntry
        /// </summary>
        /// <param name="entry"></param>
        private void AddToDuplicationEntry(Log9KEntry entry) {
            if (!CheckForDuplicationMethods()) {
                return;
            }

            int duplicationKey = KeyForDuplicationDictionary(entry);
            Log9KUtil.InvokeInUiThread(() => {
                DuplicationNode duplicationEntry = DuplicationsDictionary[duplicationKey];
                duplicationEntry.Item2.Add(new DuplicationLeaf(entry.Time, entry.ID));
                if (duplicationEntry.Item2.Count == 2) {
                    DuplicationsCollection.Add(duplicationEntry); 
                }
            });
        }

        /// <summary>
        /// Add new duplication entry (Tuple of log entry (original) and collection of log entries (duplications))
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="timeOfOriginal"></param>
        private void AddNewDuplicationEntry(Log9KEntry entry, Log9KTime timeOfOriginal) {
            if (!CheckForDuplicationMethods()) {
                return;
            }
            int duplicationKey = KeyForDuplicationDictionary(entry);
            Log9KUtil.InvokeInUiThread(() => {
                if (DuplicationsDictionary.ContainsKey(duplicationKey)) {
                    return;
                }

                ObservableCollection<DuplicationLeaf> times = new ObservableCollection<DuplicationLeaf>();
                if (timeOfOriginal == null) {
                    times = new ObservableCollection<Tuple<Log9KTime,uint>> {
                        new DuplicationLeaf(entry.Time, entry.ID)
                    }; 
                }

                DuplicationNode de = new DuplicationNode(
                    entry, times
                );
                DuplicationsDictionary.Add(duplicationKey, de);
           }); 
        }
        
        #endregion 


        #region Private Methods: Check methods

        /// <summary>
        /// Checks is current tab type and given entry type is equal
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        private bool AreLogEntryAndTabTypesEqual(Log9KEntry entry) {
            return entry.TypeString == TabTypeString;
        }

        /// <summary>
        /// Is Application.Current != null
        /// </summary>
        /// <returns></returns>
        private static bool CheckForApplicationRunning() {
            if (Application.Current == null) {
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Is app running and DuplicationsDictionary != null
        /// </summary>
        /// <returns></returns>
        private bool CheckForDuplicationMethods() {
            if (!CheckForApplicationRunning()) {
                return false;
            }
            if (DuplicationsDictionary == null) {
                return false;
            }
            return true;
        }

        #endregion


        #endregion

        
        #region INotifyPropertyChanged

        /// <summary>
        /// INotifyPropertyChanged
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion


    }

    /// <summary>
    /// Class for ALL tab logic (tab that contains all the logs)
    /// </summary>
    public class Log9KTabAll : Log9KTab {


        #region Constants

        private const uint DEFAULT_MAX_COLLECTION_SIZE_FOR_ALL_TAB = 2500;

        #endregion
    
        
        #region Constructor

        /// <summary>
        /// Constructor for ALL tab (tab that contains all the logs)
        /// </summary>
        public Log9KTabAll() : base(TabTypes.ALL) { }

        #endregion


        #region Public Overriden Methods


        #region LoadNewerLogEntry and LoadOlderLogEntry

        /// <summary>
        /// Load one newer log entry from temp file
        /// </summary>
        /// <returns></returns>
        public override bool LoadNewerLogEntry() {
            if (IsAddingNewestEntries) {
                return false;
            }

            Log9KEntry entry = null;
            bool success = false;
            uint tries = 0;

            while (!success) {
                if (tries > 100) {
                    break;
                }
                byte[] entryBytes;
                if (!Log9KUtil.ReadFixedSizeByteArrayEntry(FilenameTempFile, Log9KEntry.ENTRY_SIZE, LineNumberBottomTempFile, out entryBytes)) {
                    return false;
                }
                entry = EntryFromBytes(entryBytes);
                uint lastLogEntryInCollectionID = LogEntryCollection[LogEntryCollection.Count-1].ID;
                uint entryIDshoulBe = lastLogEntryInCollectionID + 1;
                if (entry.ID != entryIDshoulBe) {
                    if (entry.ID < entryIDshoulBe) {
                        LineNumberBottomTempFile++;
                    } else if (entry.ID > entryIDshoulBe) {
                        LineNumberBottomTempFile--;
                    }
                } else {
                    success = true;
                }
                tries++;
            }
            if (tries > 100) {
                uint lineNumber = LineNumberBottomTempFile;
                for (int i = 0; i < 200; i++) {
                    lineNumber--;
                    if (ReadNewerLogEntryAndCheckIt(out entry, lineNumber)) {
                        LineNumberBottomTempFile = lineNumber;
                        break;
                    }
                }
                for (int i = 0; i < 200; i++) {
                    lineNumber++;
                    if (ReadNewerLogEntryAndCheckIt(out entry, lineNumber)) {
                        LineNumberBottomTempFile = lineNumber;
                        break;
                    }
                }
            }
            if (entry != null) {
                CollectionAdd(entry);
            }
            return true;
        }

        /// <summary>
        /// Load one older log entry from temp file
        /// </summary>
        /// <returns></returns>
        public override bool LoadOlderLogEntry() {
            if (IsAddingNewestEntries) {
                return false;
            }
            if (LineNumberTopTempFile == 0 && LogEntryCollection[0].ID < 1) {
                return false;
            }
            Log9KEntry entry = null;
            bool success = false;
            uint tries = 0;

            while (!success) {
                if (tries > 100) {
                    break;
                }

                entry = ReadEntry(LineNumberTopTempFile);
                if (entry == null) {
                    return false;
                }

                uint firstLogEntryInCollectionID = LogEntryCollection[0].ID;
                uint entryIDshoulBe = firstLogEntryInCollectionID - 1;
                if (entry.ID != entryIDshoulBe) {
                    if (entry.ID < entryIDshoulBe) {
                        LineNumberTopTempFile++;
                    } else if (entry.ID > entryIDshoulBe) {
                        LineNumberTopTempFile--;
                    }
                } else {
                    success = true;
                }

                tries++;
            }
            if (tries > 100) {
                uint lineNumber = LineNumberTopTempFile;
                for (int i = 0; i < 30; i++) {
                    lineNumber--;
                    if (ReadOlderLogEntryAndCheckIt(out entry, lineNumber)) {
                        LineNumberTopTempFile = lineNumber;
                        break;
                    }
                }
                for (int i = 0; i < 30; i++) {
                    lineNumber++;
                    if (ReadOlderLogEntryAndCheckIt(out entry, lineNumber)) {
                        LineNumberTopTempFile = lineNumber;
                        break;
                    }
                }
            }
            if (entry == null || LogEntryCollection.Contains(entry)) {
                uint lineNumber;
                if (FindLineNumber(LogEntryCollection[0].ID-1, out lineNumber)) {
                    entry = ReadEntry(lineNumber);
                }
            }
            if (entry != null) {
                CollectionInsert(0, entry);
            }
            return true;
        }

        private bool ReadOlderLogEntryAndCheckIt(out Log9KEntry entry, uint lineNumber) {
            entry = ReadEntry(lineNumber);
            if (entry == null) {
                return false;
            }

            uint firstLogEntryInCollectionID = LogEntryCollection[0].ID;
            uint entryIDshoulBe = firstLogEntryInCollectionID - 1;
            if (entry.ID == entryIDshoulBe) {
                return true;
            }
            return false;
        }
        
        private bool ReadNewerLogEntryAndCheckIt(out Log9KEntry entry, uint lineNumber) {
            entry = ReadEntry(lineNumber);
            if (entry == null) {
                return false;
            }

            uint lastLogEntryInCollectionID = LogEntryCollection[LogEntryCollection.Count-1].ID;
            uint entryIDshoulBe = lastLogEntryInCollectionID + 1;
            if (entry.ID == entryIDshoulBe) {
                return true;
            }
            return false;
        }
        
        #endregion


        #region FilterByTime

        /// <summary>
        /// Get entries within given time period from temp file 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public override void FilterByTime(DateTime start, DateTime end) {
            if (start > end) {
                return;
            }

            if (Application.Current == null) {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(
                new FilterByTimeDelegate(FilterByTimeHandler), 
                DispatcherPriority.Background, 
                start, 
                end
            );
        }

        delegate void FilterByTimeDelegate(DateTime a, DateTime b);
        private void FilterByTimeHandler(DateTime start, DateTime end) {
            uint lineStart, lineEnd;
            FilterByTime(start, end, out lineStart, out lineEnd);
            FilterApplyToCollection(lineStart, lineEnd);
        }

        private void FilterByTime(DateTime start, DateTime end, out uint lineStart, out uint lineEnd) {
            lineStart = 0;
            lineEnd = 0;

            bool foundStart = false, foundEnd = false;
            uint i = 0;
            while (!foundStart) {
                if (i >= Log9KEntry.Counter-1) {
                    break;
                }
                Log9KEntry entry = ReadEntry(i);
                if (entry == null) {
                    break;
                }
                DateTime entryTime = entry.Time.ToDateTime();
                if (entryTime > start) {
                    foundStart = true;
                    lineStart = i;
                }
                i++;
            }

            while (!foundEnd) {
                if (i >= Log9KEntry.Counter-1) {
                    if (foundStart) {
                        lineEnd = i;
                    }
                    break;
                }
                Log9KEntry entry = ReadEntry(i);
                if (entry == null) {
                    lineEnd = i;
                    break;
                }
                DateTime entryTime = entry.Time.ToDateTime();
                if (entryTime > end) {
                    foundEnd = true;
                    lineEnd = i;
                }
                i++;
            }
        }

        private void FilterApplyToCollection(uint lineStart, uint lineEnd) {
            IsAddingNewestEntries = false;
            LogEntryCollection.Clear();
            uint i = 0; 
            for (uint j = lineStart; j < lineEnd; j++) {
                Log9KEntry entry = ReadEntry(j);
                if (entry != null) {
                    CollectionAdd(entry);
                    i++;
                }
                if (i >= LogEntryCollection.MaxCollectionSize) {
                    break;
                }
            }
            LineNumberTopTempFile = lineStart;
            
        }

        #endregion


        /// <summary>
        /// Load most recent logs from temporary file
        /// </summary>
        public override void LoadLastLogs() {
            LogEntryCollection.StartRemovingFromTop();
            LogEntryCollection.Clear();

            uint entriesNumber;
            bool success = Log9KUtil.GetEntriesNumberInFile(
                FilenameTempFile, Log9KEntry.ENTRY_SIZE, out entriesNumber
            );
            if (!success) {
                Log9KCore.Instance.InnerLog("Ошибка: метод Log9KUtil.GetEntriesNumberInFile вернул false");
                return;
            }

            if (Application.Current == null) {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => {
                    bool notEnd = true;
                    uint z = 0;
                    for (uint i = entriesNumber; notEnd; i--) {
                        Log9KEntry e = ReadEntry(i);
                        if (e != null) {
                            LogEntryCollection.Insert(0, e);
                        }
                        if (i == 0 || z >= LogEntryCollection.MaxCollectionSize) {
                            notEnd = false;
                        }
                        z++;
                    }
                }),
                DispatcherPriority.Background
            );

        }

        public override DispatcherOperation LoadEntries(uint startFromID) {
            if (startFromID > Log9KEntry.Counter) {
                return null;
            }
            startFromID--;
            if (IsAddingNewestEntries) {
                IsAddingNewestEntries = false;
            }
            if (ScrollToLastLog) {
                ScrollToLastLog = false;
            }

            uint lineNumber;
            if (startFromID == 0) {
                return null;
            }

            if (Application.Current == null) {
                return null;
            }

            DispatcherOperation a = Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                
                if (!FindLineNumber(startFromID, out lineNumber)) {
                    Log9KCore.Instance.InnerLog("Ошибка: при загрузке логов метод FindLineNumber вернул false");
                    return;
                }

                LineNumberTopTempFile = lineNumber;
                LogEntryCollection.Clear();
                for (int i = 0; i < LogEntryCollection.MaxCollectionSize; i++) {
                    Log9KEntry entry = ReadEntry(lineNumber);
                    if (entry == null) {
                        break;
                    }
                    LogEntryCollection.Add(entry);
                    lineNumber++;
                }

            }), DispatcherPriority.Background);

            return a;
        }

        /// <summary>
        /// For ALL tab there is no need to give type to serialization method
        /// </summary>
        /// <param name="entryBytes"></param>
        /// <returns></returns>
        protected override Log9KEntry EntryFromBytes(byte[] entryBytes) {
            return Log9KEntry.FromByteArray(entryBytes);
        }
        
        /// <summary>
        /// Serialization for ALL tab not needs tab type
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        protected override byte[] EntryToBytes(Log9KEntry entry) {
            return entry.ToByteArray();
        }
        
        /// <summary>
        /// Is current tab ALL, it is true
        /// </summary>
        /// <returns></returns>
        public override bool IsAllTab() {
            return true;
        }

        #endregion

        
        #region Protected Overriden Methods

        /// <summary>
        /// Init LogEntryCollection for current tab
        /// </summary>
        protected override void InitCollection() {
            _logEntryCollection = new Log9KTabObservableCollection(DEFAULT_MAX_COLLECTION_SIZE_FOR_ALL_TAB); 
        }

        #endregion


    }


    #region ObservableCollection for Log9KTabs


    /// <summary>
    /// Observable collection with restricted max collection size 
    /// <remarks>http://stackoverflow.com/questions/4305623/how-to-resize-observablecollection </remarks>
    /// </summary>
    public class Log9KTabObservableCollection : ObservableCollection<Log9KEntry> {
        

        #region Enums

        /// <summary>
        /// What to remove if too much entries, older entries of newer
        /// </summary>
        public enum WhatToRemoveEnum {
            /// <summary>
            /// Remove older entries
            /// </summary>
            OLDER_ENTRIES,
            /// <summary>
            /// Remove newer entries
            /// </summary>
            NEWER_ENTRIES
        }

        #endregion

        
        #region Fields

        private bool _isMaxSizeReached;
        /// <summary>
        /// Check is collection size reached maximum
        /// </summary>
        public bool IsMaxSizeReached {
            get { return _isMaxSizeReached; }
            set {
                _isMaxSizeReached = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMaxSizeReached"));
            }
        }
        
        #endregion


        #region Properties

        /// <summary>
        /// Remove from top or from bottom of collection if oversized?
        /// </summary>
        public WhatToRemoveEnum WhatToRemove { get; private set; }

        /// <summary>
        /// Which size is oversized?
        /// </summary>
        public uint MaxCollectionSize { get; set; }

        #endregion


        /// <summary>
        /// If max size is reached, entries will be removed from top of collection
        /// </summary>
        public void StartRemovingFromTop() {
            WhatToRemove = WhatToRemoveEnum.OLDER_ENTRIES;
        } 
        
        /// <summary>
        /// If max size is reached, entries will be removed from bottom of collection
        /// </summary>
        public void StartRemovingFromBottom() {
            WhatToRemove = WhatToRemoveEnum.NEWER_ENTRIES;
        } 


        #region Constructor

        /// <summary>
        /// Creates log entry collection for Log9KTab
        /// </summary>
        /// <param name="maxCollectionSize"></param>
        public Log9KTabObservableCollection(uint maxCollectionSize = 0) : base() {
            MaxCollectionSize = maxCollectionSize;
        }

        #endregion


        #region Protected methods

        /// <summary>
        /// <para>removes N (diff between count and Max)
        /// first elements from top or bottom</para>
        /// <para>if current collection size more than MaxCollectionSize</para>
        /// </summary>
        protected void RemoveItemFromTopOrBottomIfMaxSizeIsOverrun() {
            if (MaxCollectionSize > 0 && MaxCollectionSize < Count) {
                uint trimCount = (uint)Count - MaxCollectionSize;
                for (int i = 0; i < trimCount; i++) {
                    if (WhatToRemove == WhatToRemoveEnum.OLDER_ENTRIES) {
                        RemoveAt(0);
                    } else {
                        RemoveAt(Count - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Sets IsMaxSizeReached if so
        /// </summary>
        private void CheckMaxSizeReached() {
            if (Count >= MaxCollectionSize) {
                IsMaxSizeReached = true;
            } else {
                IsMaxSizeReached = false;
            }
        }

        /// <summary>
        /// Insert and delete if more than max
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        protected override void InsertItem(int index, Log9KEntry item) {
            // No duplications in collection
            if (Contains(item)) {
                return;
            }

            // Collection is sorted
            if (Count!= 0 && Count == index) {
                for (int i = 0; i < Items.Count; i++) {
                    Log9KEntry e = Items[i];
                    if (item.ID == e.ID - 1) {
                        index = i;
                        break;
                    }
                } 
            }
            if (Count != 0 && 0 == index) {
                for (int i = 0; i < Items.Count; i++) {
                    Log9KEntry e = Items[i];
                    if (item.ID == e.ID+1) {
                        index = i+1;
                        break;
                    }
                }
            }

            base.InsertItem(index, item);
            
            // Collection size is restricted
            CheckMaxSizeReached();
            RemoveItemFromTopOrBottomIfMaxSizeIsOverrun();
        }

        #endregion


    }
        
    #endregion


}
