using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using LogControl9K.Annotations;

namespace LogControl9K {
    /// <summary>
    /// <para>Core of LogControl9K, perform logging and controlling through it's methods</para>
    /// <remarks>Singleton</remarks>
    /// </summary>
    public class Log9KCore : INotifyPropertyChanged {


        #region Constants
        
        /// <summary>
        /// Log file extension
        /// </summary>
        public const string LOG_EXTENSION = ".log";

        /// <summary>
        ///File extension for files, that used for temporary storage of log entries 
        /// </summary>
        public const string TEMP_LOG_EXTENSION = ".log9k";

        private const string SETTINGS_FILENAME = "LogControl9K.settings.xml";
        
        /// <summary>
        /// Max custom type name length
        /// </summary>
        public const int MAX_CUSTOM_TYPE_NAME_LENGTH = 15;
        
        /// <summary>
        /// Tab for logging LogControl9K control
        /// </summary>
        internal const string INNER_LOG_TAB = "LogControl9K";

        #endregion


        #region Enums



        
        #endregion


        #region Fields
        
        /// <summary>
        /// Is innner logs (LogControl9K) tab is visible
        /// </summary>
        private bool _isInnerLogTabVisible;

        /// <summary>
        /// Dictionary with tabs
        /// </summary>
        private Dictionary<string, Log9KTab> _log9KTabsDictionary = new Dictionary<string, Log9KTab>();
        
        /// <summary>
        /// Inner log queque for logging LogControl9K before instance created (constructor time)
        /// </summary>
        private static Queue<Log9KEntry> _preInstanceCreatedInnerLogQueue = new Queue<Log9KEntry>();

        #endregion


        #region Properties 
        
        /// <summary>
        /// Settings of LogControl
        /// </summary>
        public static readonly Log9KSettings Settings = new Log9KSettings();

        /// <summary>
        /// ALL tab
        /// </summary>
        internal Log9KTabAll TabAll { get; private set; }

        private Log9KTab _currentTab;
        /// <summary>
        /// Current selected tab
        /// </summary>
        public Log9KTab CurrentTab {
            get { return _currentTab; }
            set {
                _currentTab = value;
                OnPropertyChanged("CurrentTab");
            }
        }

        /// <summary>
        /// Observable collection for view model, dictionary is for inner use
        /// </summary>
        internal ObservableCollection<Log9KTab> Log9KTabsCollection { get; private set; }

        #endregion
       
 
        #region Constructor, Singletone, Initialization


        #region Singletone

        /// <summary>
        /// Log9KCore instance
        /// </summary>
        public static Log9KCore Instance { get { return Nested.instance; } }        
        /// <summary>
        /// Singletone helper class
        /// http://csharpindepth.com/Articles/General/Singleton.aspx (Fifth)
        /// </summary>
        // ReSharper disable once ClassNeverInstantiated.Local
        private class Nested {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested() {
            }
            internal static readonly Log9KCore instance = new Log9KCore(); 
        }

        #endregion

        
        private Log9KCore() {
            /* For using in WinForms*/
            if (null == Application.Current) {
                // ReSharper disable once ObjectCreationAsStatement
                new Application();
            }

            Log9KTabsCollection = new ObservableCollection<Log9KTab>();

            DateTime coreCreationTime = DateTime.Now;

            LoadSettings();

            InitTabs();

            InnerLog("Testin",true);
            
            RemoveTempFilesInSeparateThread(coreCreationTime);

            SwitchInnerLogTabVisibility(false);

            AddLogEntriesFromQueue();
        }

        /// <summary>
        /// Add entries from queue of inner logs that occured before Log9KCore created
        /// </summary>
        private void AddLogEntriesFromQueue() {
            if (_preInstanceCreatedInnerLogQueue.Count > 0) {
                Log9KEntry e =_preInstanceCreatedInnerLogQueue.Dequeue();
                InnerLog(e);
            }
        }

        /// <summary>
        /// Init and fill core's collection of tabs
        /// </summary>
        private void InitTabs() {
            IEnumerable<TabTypes> tabTypes = Log9KUtil.EnumUtil.GetValues<TabTypes>();
            foreach (TabTypes type in tabTypes) {
                switch (type) {
                    case TabTypes.CUSTOM: {
                        continue;
                    }
                    case TabTypes.ALL: {
                        TabAll = new Log9KTabAll();
                        AddNewTab(TabTypes.ALL, TabAll);
                        break;
                    }
                    default: {
                        AddNewTab(type, new Log9KTab(type));
                        break;
                    }
                }
            }
            AddNewTab(INNER_LOG_TAB, new Log9KTab(INNER_LOG_TAB));
        }

        /// <summary>
        /// <para>Add new tab to Core's tab's collections, </para>
        /// <para>Use this method for adding new tabs instead of directly changing collections</para>
        /// </summary>
        /// <param name="tabType"></param>
        /// <param name="tab"></param>
        private void AddNewTab(TabTypes tabType, Log9KTab tab) {
            AddNewTab(tabType.ToString(), tab);
        }
        
        /// <summary>
        /// <para>Add new CUSTOM tab to Core's tab's collections, it also used by non-custom tab method</para>
        /// <para>Use this method for adding new CUSTOM tabs instead of directly changing collections</para>
        /// </summary>
        /// <param name="tabType"></param>
        /// <param name="tab"></param>
        private void AddNewTab(string tabType, Log9KTab tab) {
            _log9KTabsDictionary.Add(tabType, tab);
            Log9KTabsCollection.Add(tab);
        }
        

        /// <summary>
        /// Invokes RemoveTempFiles in separate thread
        /// </summary>
        /// <param name="olderThan">Remove files older than this time</param>
        private static void RemoveTempFilesInSeparateThread(DateTime olderThan) {
            /* Let's remove old temporary files (.log9k) in separate thread */
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += (sender, args) => {
                RemoveTempFiles(olderThan);
            };
            bw.RunWorkerAsync();
        }

        #endregion

        
        #region Public Methods
        

        #region Public Methods: logging 

        /// <summary>
        /// <para>Primary log method, log message with given type</para>
        /// <remarks>Not for custom type entries!!!</remarks>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Log(LogEntryTypes type, string message, Levels level=Levels.PRIMARY) {
            if (string.IsNullOrEmpty(message)) {
                InnerLog("Ошибка: в методе Log() строка message пуста либо равна null!");
                return false;
            }
            Log9KEntry logEntry = new Log9KEntry(type, message, level);
            return Log(logEntry);            
        }

        /// <summary>
        /// Log method for custom type entries
        /// </summary>
        /// <param name="logCustomType"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Log(string logCustomType, string message, Levels level=Levels.PRIMARY) {
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(logCustomType)) {
                return false;
            }
            Log9KEntry logEntry = new Log9KEntry(logCustomType, message, level);
            return Log(logEntry);
        }

        /// <summary>
        /// Log with Format
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Log_f(LogEntryTypes type, string format, params object[] args) {
            return Log(type, string.Format(format, args));
        }

        /// <summary>
        /// Log with Format, secondary level
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool LogSecondary_f(LogEntryTypes type, string format, params object[] args) {
            return Log(type, string.Format(format, args), Levels.SECONDARY);
        }

        /// <summary>
        /// Helper method used by public loggin methods
        /// </summary>
        /// <param name="logEntry"></param>
        private bool Log(Log9KEntry logEntry) {
            if (logEntry == null) {
                InnerLog("Ошибка: в методе Log() переданный объект класса Log9KEntry равен null!");
                return false;
            }
            AddLogEntryToTabs(logEntry);
            return true;
        }

        /// <summary>
        /// Inner LogControl9K control logging
        /// </summary>
        /// <param name="message"></param>
        internal void InnerLog(string message) {
            Log(INNER_LOG_TAB, BuildLogMessageWithClassAndMethod(message));
        }
        
        /// <summary>
        /// Helper method for InnerLoggin 
        /// </summary>
        /// <param name="e"></param>
        private void InnerLog(Log9KEntry e) {
            Log(e);
        }
        
        /// <summary>
        /// Inner LogControl9K control logging BEFORE created Log9KCore Instance
        /// </summary>
        /// <param name="message"></param>
        /// <param name="a"></param>
        internal static void InnerLog(string message, bool a=true) {
            _preInstanceCreatedInnerLogQueue.Enqueue(
                new Log9KEntry(INNER_LOG_TAB, BuildLogMessageWithClassAndMethod(message))
            );
        }

        /// <summary>
        /// Append info about method that invokes Debug() and others
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string BuildLogMessageWithClassAndMethod(string message) {
            MethodBase mb;
            GetMethodBase(out mb);
            Type type = mb.DeclaringType;
            if (type == null)
            {
                return message;
            }
            if (string.IsNullOrEmpty(message))
            {
                return string.Format("{0} :: {1}", type.Name, mb);
            }
            return string.Format("{0} :: {1} :: {2}", type.Name, mb, message);
        }
            
        /// <summary>
        /// Get MethodBase for method which was invoking Debug() or InnerLog()
        /// </summary>
        /// <param name="mb"></param>
        private static void GetMethodBase(out MethodBase mb) {
            StackFrame frame = new StackFrame(3);
            MethodBase method = frame.GetMethod();
            mb = method;
        }


        #endregion


        #region Public Methods: helpers

        
        #region INFO
        
        /// <summary>
        /// Log info event
        /// </summary>
        /// <param name="messages"></param>
        public bool Info(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.INFO, message);
        }
        
        /// <summary>
        /// Log info event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Info(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.INFO, message, level);
        }
        
        /// <summary>
        /// Log info event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Info(int message, Levels level=Levels.PRIMARY) {
            return Info(message.ToString(), level); 
        }

        /// <summary>
        /// Log info event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Info(double message, Levels level=Levels.PRIMARY) {
            return Info(message.ToString(CultureInfo.InvariantCulture), level); 
        }
        
        /// <summary>
        /// Log info event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Info_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.INFO, format, args);
        }

        /// <summary>
        /// Log secondary info event with format 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool InfoSecondary_f(string format, params object[] args) {
            return LogSecondary_f(LogEntryTypes.INFO, format, args); 
        }

        #endregion
       
 
        #region ERROR
        
        /// <summary>
        /// Log error event
        /// </summary>
        /// <param name="messages"></param>
        public bool Error(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.ERROR, message);
        }
        
        /// <summary>
        /// Log error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Error(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.ERROR, message, level);
        }
        
        /// <summary>
        /// Log error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Error(int message, Levels level=Levels.PRIMARY) {
            return Error(message.ToString(), level); 
        }
        
        /// <summary>
        /// Log error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Error(double message, Levels level=Levels.PRIMARY) {
            return Error(message.ToString(CultureInfo.InvariantCulture), level); 
        }
        
        /// <summary>
        /// Log error event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Error_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.ERROR, format, args);
        }

        /// <summary>
        /// Log secondary error event with format 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool ErrorSecondary_f(string format, params object[] args) {
            return LogSecondary_f(LogEntryTypes.ERROR, format, args); 
        }

        #endregion


        #region CRITICAL_ERROR 

        /// <summary>
        /// Log critical error event
        /// </summary>
        /// <param name="messages"></param>
        public bool CriticalError(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.CRITICAL_ERROR, message);
        }

        /// <summary>
        /// Log critical error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CriticalError(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.CRITICAL_ERROR, message, level);
        }
        
        /// <summary>
        /// Log critical error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CriticalError(int message, Levels level=Levels.PRIMARY) {
            return CriticalError(message.ToString(), level); 
        }
        
        /// <summary>
        /// Log critical error event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CriticalError(double message, Levels level=Levels.PRIMARY) {
            return CriticalError(message.ToString(CultureInfo.InvariantCulture), level); 
        }

        /// <summary>
        /// Log critical erro event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CriticalError_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.CRITICAL_ERROR, format, args);
        }

        /// <summary>
        /// Log secondary critical error event with format 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CriticalErrorSecondary_f(string format, params object[] args) {
            return LogSecondary_f(LogEntryTypes.CRITICAL_ERROR, format, args); 
        }

        #endregion


        #region SUCCESS

        /// <summary>
        /// Log success event
        /// </summary>
        /// <param name="messages"></param>
        public bool Success(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.SUCCESS, message);
        }
        
        /// <summary>
        /// Log success event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Success(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.SUCCESS, message, level);
        }
        
        /// <summary>
        /// Log success event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Success(int message, Levels level=Levels.PRIMARY) {
            return Success(message.ToString(), level); 
        }
        
        /// <summary>
        /// Log success event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Success(double message, Levels level=Levels.PRIMARY) {
            return Success(message.ToString(CultureInfo.InvariantCulture), level); 
        }

        /// <summary>
        /// Log success event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Success_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.SUCCESS, format, args);
        }

        /// <summary>
        /// Log secondary success event with format 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool SuccessSecondary_f(string format, params object[] args) {
            return LogSecondary_f(LogEntryTypes.SUCCESS, format, args); 
        }


        #endregion
        
        
        #region CODE_FLOW

        /// <summary>
        /// Log code flow event
        /// </summary>
        /// <param name="messages"></param>
        public bool CodeFlow(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.CODE_FLOW, message);
        }
        
        /// <summary>
        /// Log code flow event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CodeFlow(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.CODE_FLOW, message, level);
        }

        /// <summary>
        /// Log code flow event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CodeFlow(int message, Levels level=Levels.PRIMARY) {
            return CodeFlow(message.ToString(), level); 
        }
        
        /// <summary>
        /// Log code flow event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool CodeFlow(double message, Levels level=Levels.PRIMARY) {
            return CodeFlow(message.ToString(CultureInfo.InvariantCulture), level); 
        }

        /// <summary>
        /// Log code flow event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CodeFlow_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.CODE_FLOW, format, args);
        }

        /// <summary>
        /// Log secondary code flow event with format 
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool CodeFlowSecondary_f(string format, params object[] args) {
            return LogSecondary_f(LogEntryTypes.CODE_FLOW, format, args); 
        }

        #endregion
        
        
        #region DEBUG

        /// <summary>
        /// <para>Log debug message[s] (message will be with method name)</para>
        /// <para>(message will be with method name)</para>
        /// </summary>
        /// <param name="messages"></param>
        public bool Debug(params string[] messages)
        {
            string message = string.Empty;
            if (messages.Length == 0)
            {
                message = string.Empty;
            } 
            else
            {
                message = messages.Aggregate("", (current, s) => current + (" " + s));
            }
            return Log(LogEntryTypes.DEBUG, BuildLogMessageWithClassAndMethod(message));
        }
        
        /// <summary>
        /// <para>Log debug message with string.Format</para>
        /// <para>(message will be with method name)</para>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool Debug_f(string format, params object[] args)
        {
            return Log(LogEntryTypes.DEBUG, BuildLogMessageWithClassAndMethod(string.Format(format, args)));
        }
        
        /// <summary>
        /// Log secondary debug event with format 
        /// <para>(message will be with method name)</para>
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool DebugSecondary_f(string format, params object[] args) {
            return Log(LogEntryTypes.DEBUG, BuildLogMessageWithClassAndMethod(string.Format(format, args)), Levels.SECONDARY);
        }

        #endregion
       
 
        #region WARNING

        /// <summary>
        /// Log warning event
        /// </summary>
        /// <param name="messages"></param>
        public bool Warning(params string[] messages) {
            string message = messages.Aggregate("", (current, s) => current + (" " + s));
            return Log(LogEntryTypes.WARNING, message);
        }
        
        /// <summary>
        /// Log warning event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Warning(string message, Levels level=Levels.PRIMARY) {
            return Log(LogEntryTypes.WARNING, message, level);
        }
        
        /// <summary>
        /// Log warning event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Warning(int message, Levels level=Levels.PRIMARY) {
            return Warning(message.ToString(), level); 
        }

        /// <summary>
        /// Log warning event
        /// </summary>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public bool Warning(double message, Levels level=Levels.PRIMARY) {
            return Warning(message.ToString(CultureInfo.InvariantCulture), level); 
        }

        /// <summary>
        /// Log warning event with Format
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public bool Warning_f(string format, params object[] args) {
            return Log_f(LogEntryTypes.WARNING, format, args);
        }

        #endregion


        #endregion


        #region Public Methods: Customization


        /// <summary>
        /// Changes visibility of InnerLog tab
        /// </summary>
        /// <param name="b"></param>
        internal void SwitchInnerLogTabVisibility(bool b) {
            Log9KTab t = GetTab(INNER_LOG_TAB);
            if (t == null) {
                Error("Не найдена вкладка для внутреннего логирования контрола логов");
                return;
            }
            if (b) {
                if (!Log9KTabsCollection.Contains(t)) {
                    Log9KTabsCollection.Add(t);
                    _isInnerLogTabVisible = true;
                }
            } else {

                if (Log9KTabsCollection.Contains(t)) {
                    Log9KTabsCollection.Remove(t);
                    _isInnerLogTabVisible = false;
                }
            }
        }

        /// <summary>
        /// Add new type of log entries and new tab for it
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="ArgumentException"></exception>
        public bool AddCustomType(string type) {
            if (type.Length > MAX_CUSTOM_TYPE_NAME_LENGTH) {
                InnerLog("Ошибка: Максимальный размер имени пользовательской вкладки: " + MAX_CUSTOM_TYPE_NAME_LENGTH);
                return false;
            }
            IEnumerable<TabTypes> typesEnumerable = Log9KUtil.EnumUtil.GetValues<TabTypes>();
            List<string> tabTypesStringsEnumerable = typesEnumerable.Select(tabType => tabType.ToString()).ToList();
            tabTypesStringsEnumerable.Add(INNER_LOG_TAB);
            if (tabTypesStringsEnumerable.Any(s => string.Equals(s, type, StringComparison.CurrentCultureIgnoreCase))) {
                InnerLog("Ошибка: нельзя добавить пользовательский тип такой же как и типы по умолчанию: " + type);
                return false;
            }

            if (_log9KTabsDictionary.ContainsKey(type)) {
                InnerLog("Ошибка: такой пользовательский тип уже зарегистрирован: " + type);
                return false;
            }

            AddNewTab(type, new Log9KTab(type));
            return true;
        }
        
        /// <summary>
        /// <para>Set order of tabs in tabs collection</para>
        /// <para>Arguments should be TabTypes and strings (for custom tabs)</para>
        /// </summary>
        /// <param name="types">Types of tabs in desired order (TabTypes and strings)</param>
        public bool SetTabOrder(params object[] types) {
            if (types.Length == 0) {
                InnerLog("Ошибка: аргумент функции SetTabOrder — длина массива равна нулю");
                return false;
            }
            List<Log9KTab> tabsList = new List<Log9KTab>();
            foreach (object type in types) {
                bool isTabTypes = type is TabTypes;
                bool isString = type is string;
                if (!(isTabTypes || isString)) {
                    InnerLog("Ошибка: в метод SetTabOrder был передан объект не являющийся строкой/TabTypes");
                    return false;
                }
                if (isTabTypes) {
                    TabTypes tabType = type as TabTypes? ?? TabTypes.INFO;
                    if (tabType == TabTypes.CUSTOM) {
                        InnerLog("Для установки положения пользовательской вкладки передавайте имя вкладки в метод, а не тип CUSTOM");
                        continue;
                    }
                    Log9KTab log9Ktab = GetTab(tabType);
                    if (log9Ktab == null) {
                        InnerLog("Не найдено вкладки с типом: " + tabType);
                    } else {
                        tabsList.Add(log9Ktab);
                    }
                } 
                if (isString) {
                    string tabType = type as string;
                    Log9KTab log9Ktab = GetTab(tabType);
                    if (log9Ktab != null) {
                        tabsList.Add(log9Ktab);
                    } else {
                        InnerLog("Ошибка: вкладки с типом: " + tabType + " не найдено");
                    }
                }
            }
            if (tabsList.Count == 0) {
                InnerLog("Ошибка: в методе SetTabOrder при формировании списка вкладок был получен пустой список");
                return false;
            } else {
                Log9KTabsCollection.Clear();
                foreach (Log9KTab log9KTab in tabsList) {
                    Log9KTabsCollection.Add(log9KTab);
                }
                if (_isInnerLogTabVisible) {
                    Log9KTab innerTab = GetTab(INNER_LOG_TAB);
                    if (innerTab != null) {
                        Log9KTabsCollection.Add(innerTab);
                    } else {
                        Error("Не найдена вкладка для логирования LogControl9K:" + INNER_LOG_TAB);
                    }
                    
                }
                return true;
            }
        }



        /// <summary>
        /// Create mixed tab
        /// </summary>
        /// <param name="types"></param>
        /// <returns></returns>
//        public bool CreateMixedTab(params object[] types) {
//            if (types.Length == 0) {
//                InnerLog("Ошибка: аргумента — длина массива равна нулю");
//                return false;
//            }
//            List<Log9KTab> tabsList = new List<Log9KTab>();
//            foreach (object type in types) {
//                bool isTabTypes = type is TabTypes;
//                bool isString = type is string;
//                if (!(isTabTypes || isString)) {
//                    InnerLog("Ошибка: в метод был передан объект не являющийся строкой/TabTypes");
//                    return false;
//                }
//                if (isTabTypes) {
//                    TabTypes tabType = (TabTypes) (type as TabTypes?);
//                    if (tabType == TabTypes.CUSTOM) {
//                        InnerLog("Для установки положения пользовательской вкладки передавайте имя вкладки в метод, а не тип CUSTOM");
//                        continue;
//                    }
//                    Log9KTab log9Ktab = GetTab(tabType);
//                    if (log9Ktab == null) {
//                        InnerLog("Не найдено вкладки с типом: " + tabType);
//                    } else {
//                        tabsList.Add(log9Ktab);
//                    }
//                }
//                if (isString) {
//                    string tabType = type as string;
//                    Log9KTab log9Ktab = GetTab(tabType);
//                    if (log9Ktab != null) {
//                        tabsList.Add(log9Ktab);
//                    } else {
//                        InnerLog("Ошибка: вкладки с типом: " + tabType + " не найдено");
//                    }
//                }
//            }
//
//            if (tabsList.Count == 0) {
//                InnerLog("Ошибка: при формировании списка вкладок был получен пустой список");
//                return false;
//            }
//
//            StringBuilder mixedTabType = new StringBuilder();
//            foreach (Log9KTab tab in tabsList) {
//                mixedTabType.Append(tab.TabTypeString);
//            }
//            string mixedTabTypeName =  mixedTabType.ToString();
//            Log9KTab t = new Log9KTab(mixedTabType.ToString());
//            if (_log9KTabsDictionary.ContainsKey(mixedTabTypeName)) {
//                InnerLog("Уже есть такая миксованая вкладка: " + mixedTabTypeName);
//                return false;
//            }
//
//            _log9KTabsDictionary.Add(mixedTabTypeName, t);
//
//            return true;
//        }


        /// <summary>
        /// Set header for tab which will be displayed in control
        /// </summary>
        /// <param name="tabType"></param>
        /// <param name="header"></param>
        public void SetTabHeader(TabTypes tabType, string header) {
            Log9KTab tab = GetTab(tabType);
            if (tab != null) {
                tab.TabHeader = header;
            }
        }
        
        /// <summary>
        /// Make the tab with given type active
        /// </summary>
        public void SetActiveTab(TabTypes tabType) {
            if (tabType == TabTypes.CUSTOM) {
                return;
            }
            Log9KTab tab = GetTab(tabType);
            if (tab != null) {
                if (!Log9KTabsCollection.Contains(tab)) {
                    return;
                }
                Log9KUtil.BeginInvokeInUiThread(() => {
                    CurrentTab = tab;
                });
            }
        }
        
        /// <summary>
        /// Make the tab with given type active
        /// </summary>
        public void SetActiveTab(string tabType) {
            Log9KTab tab = GetTab(tabType);
            if (tab != null) {
                if (!Log9KTabsCollection.Contains(tab)) {
                    return;
                }
                Log9KUtil.BeginInvokeInUiThread(() => {
                    CurrentTab = tab;
                });
            }
        }

        /// <summary>
        /// Set maximal size for observable collection (which is displayed in DataGrid) of given tab
        /// </summary>
        /// <param name="tabType"></param>
        /// <param name="maxCollectionSize"></param>
        public void SetMaxCollectionSizeForTab(TabTypes tabType, uint maxCollectionSize) {
            Log9KTab log9KTab = GetTab(tabType);
            if (log9KTab == null) {
                return;
            }
            log9KTab.LogEntryCollection.MaxCollectionSize = maxCollectionSize;
        }

        #endregion



 
        #region Public Methods: Save/Load Settings

        /// <summary>
        /// Load settings from file
        /// </summary>
        internal void LoadSettings() {
            bool success = Settings.LoadFromFile(SETTINGS_FILENAME);
            if (!success) {
                InnerLog("Не удалось загрузить настройки из файла, будут использованы значения по умолчанию", true); 
            }
        }
        
        /// <summary>
        /// Save settings from file
        /// </summary>
        internal void SaveSettings() {
            bool success = Settings.SaveToFile(SETTINGS_FILENAME);
            if (!success) {
                InnerLog("Ошибка: не удалось сохранить настройки в файл"); 
            }
        }

        #endregion


        #endregion

        
        #region Private Methods


        #region Private Methods: Log9KTabs stuff

        /// <summary>
        /// Get tab by given entry type
        /// </summary>
        /// <param name="tabType"></param>
        /// <returns></returns>
        private Log9KTab GetTab(LogEntryTypes tabType) {
            string tabTypeString = tabType.ToString();
            return GetTab(tabTypeString);
        }

        /// <summary>
        /// Get tab by given tab type
        /// </summary>
        /// <param name="tabType"></param>
        /// <returns></returns>
        private Log9KTab GetTab(TabTypes tabType) {
            string tabTypeString = tabType.ToString();
            return GetTab(tabTypeString);
        }
        
        /// <summary>
        /// Get tab by given string with tab type
        /// </summary>
        /// <param name="tabType"></param>
        /// <returns></returns>
        private Log9KTab GetTab(string tabType) {
            if (_log9KTabsDictionary.ContainsKey(tabType)) {
                return _log9KTabsDictionary[tabType];
            }
            return null;
        }

        #endregion
        
        
        #region Private Methods: util methods for logging

        /// <summary>
        /// Add log entry to ALL tab an tab with corresponding type
        /// </summary>
        /// <param name="entry"></param>
        private void AddLogEntryToTabs(Log9KEntry entry) {
            AddToAllTab(entry);
            AddToConcreteTypeTab(entry);
        }
        
        /// <summary>
        /// Add log entry to ALL tab
        /// </summary>
        /// <param name="logEntry"></param>
        private void AddToAllTab(Log9KEntry logEntry) {
            if (TabAll == null) {
                InnerLog("Ошибка: при добавлении лога — вкладка ALL равна null");
                return;
            } 
            TabAll.AddLogEntry(logEntry);
        }
        
        /// <summary>
        /// Add entry to tab of it's type
        /// </summary>
        /// <param name="entry"></param>
        private void AddToConcreteTypeTab(Log9KEntry entry) {
            Log9KTab entryTypeTab = GetTab(entry.TypeString);
            if (entryTypeTab == null) {
                InnerLog("Ошибка: при добавлении лога — вкладка " + entry.TypeString + " равна null");
                return;
            }
            entryTypeTab.AddLogEntry(entry);
        }

        #endregion

        
        #region Private Methods: removing temporary files

        /// <summary>
        /// Remove temporary files (.log9k) older than given time
        /// </summary>
        /// <param name="olderThan"></param>
        private static void RemoveTempFiles(DateTime olderThan) {
            const bool removeOlderThanNow = true;
            string path = Settings.Folder;
            if (!Directory.Exists(path)) {
                return;
            }
            /* In Settings.Folder directory should be dirs named with date (year, month, day) */
            IEnumerable<string> dateDirsEnumerable = Directory.EnumerateDirectories(path);
            foreach (string dateDir in dateDirsEnumerable) {
                RemoveTempFilesOlderThanNow(dateDir, olderThan, removeOlderThanNow);

                IEnumerable<string> tabDirsEnumerable = Directory.EnumerateDirectories(dateDir);
                foreach (string tabDir in tabDirsEnumerable) {
                    RemoveTempFilesOlderThanNow(tabDir, olderThan, removeOlderThanNow);
                }
            }
        }

        /// <summary>
        /// Method removes files in given dir older that given time
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="now"></param>
        /// <param name="removeOlderThanNow"></param>
        private static void RemoveTempFilesOlderThanNow(string dirPath, DateTime now, bool removeOlderThanNow) {
            IEnumerable<string> files = Directory.EnumerateFiles(dirPath);
            foreach (string f in files) {
                string extension = Path.GetExtension(f);
                if (extension != TEMP_LOG_EXTENSION) {
                    continue;
                }
                if (removeOlderThanNow) {
                    DateTime creationTime = File.GetCreationTime(f);
                    if (creationTime.CompareTo(now) >= 0) {
                        continue;
                    }
                }
                File.Delete(f);
//                Debug.WriteLine(f + " was deleted");
            }
            
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
}
