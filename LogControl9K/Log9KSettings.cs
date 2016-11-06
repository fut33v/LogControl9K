using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Xml;
using LogControl9K.Annotations;

// ReSharper disable InconsistentNaming

namespace LogControl9K {
    /// <summary>
    /// Settings for LogControl9K control
    /// </summary>
    public class Log9KSettings : INotifyPropertyChanged {


        #region Constants

        private const string DEFAULT_FOLDER = "logs";
        private const string DEFAULT_DATE_TIME_FORMAT = "HH:mm:ss.fff";

        private static readonly Brush DEFAULT_INFO_BRUSH = Brushes.Black;
        private static readonly Brush DEFAULT_ERROR_BRUSH = Brushes.DarkRed;
        private static readonly Brush DEFAULT_CRITICAL_ERROR_BRUSH = Brushes.Red;
        private static readonly Brush DEFAULT_SUCCESS_BRUSH = Brushes.Green;
        private static readonly Brush DEFAULT_CODE_FLOW_BRUSH = (SolidColorBrush) new BrushConverter().ConvertFromString("#666666");
        private static readonly Brush DEFAULT_DEFAULT_BRUSH = Brushes.Black;
        private static readonly Brush DEFAULT_DEBUG_BRUSH = Brushes.Black;
        private static readonly Brush DEFAULT_CUSTOM_BRUSH = Brushes.Purple;
        private static readonly Brush DEFAULT_INNER_BRUSH = Brushes.SteelBlue;

        #endregion
        
        
        #region Properties


        #region Folder

        /// <summary>
        /// Field for property <see cref="Folder"/>
        /// </summary>
        private string _folder;
        /// <summary>
        /// Root folder for logs
        /// </summary>
        public string Folder {
            get { return _folder; }
            set {
                _folder = value;
                OnPropertyChanged("Folder");
            }
        }

        #endregion

        
        #region DateTimeFormat

        /// Date time format in files and UI, USE PROPERTY
        private string _dateTimeFormat;
        /// <summary>
        /// Date time format in files and UI
        /// </summary>
        public string DateTimeFormat {
            get {
                return _dateTimeFormat;
                
            }
            set {
                _dateTimeFormat = value;
                OnPropertyChanged("DateTimeFormat");
            }
        }

        #endregion

        
        #region IsWritingToFileEnabled

        /// <summary>
        /// Field for property <see cref="IsWritingToFileEnabled"/>
        /// </summary>
        private bool _isWritingToFileEnabled;
        /// <summary>
        /// Are we appending to .log file? 
        /// </summary>
        public bool IsWritingToFileEnabled {
            get { return _isWritingToFileEnabled; }
            set {
                _isWritingToFileEnabled = value;
                OnPropertyChanged("IsWritingToFileEnabled");
            }
        }

        #endregion

        
        #region IsWritingEachTabEnabled
        
        /// <summary>
        /// Field for property <see cref="IsWritingEachTabEnabled"/> 
        /// </summary>
        private bool _isWritingEachTabEnabled;
        /// <summary>
        /// Are we appending to .log file for each separate tab?
        /// </summary>
        public bool IsWritingEachTabEnabled {
            get { return _isWritingEachTabEnabled; }
            set {
                _isWritingEachTabEnabled = value;
                OnPropertyChanged("IsWritingEachTabEnabled");
            }
        }

        #endregion


        #region Log/Tab type Brushes

        /// <summary>
        /// Field for property <see cref="InfoBrush"/>
        /// </summary>
        private Brush _infoBrush;
        /// <summary>
        /// Brush for INFO type logs/tabs
        /// </summary>
        public Brush InfoBrush {
            get { return _infoBrush; }
            set {
                _infoBrush = value;
                OnPropertyChanged("InfoBrush");
            }
        }
        
        /// <summary>
        /// Field for property <see cref="ErrorBrush"/>
        /// </summary>
        private Brush _errorBrush;
        /// <summary>
        /// Brush for ERROR type logs/tabs
        /// </summary>
        public Brush ErrorBrush {
            get { return _errorBrush; }
            set {
                _errorBrush = value;
                OnPropertyChanged("ErrorBrush");
            }
        }
        
        /// <summary>
        /// Field for property <see cref="CriticalErrorBrush"/>
        /// </summary>
        private Brush _criticalErrorBrush;
        /// <summary>
        /// Brush for CRITICAL_ERROR type logs/tabs
        /// </summary>
        public Brush CriticalErrorBrush {
            get { return _criticalErrorBrush; }
            set {
                _criticalErrorBrush = value;
                OnPropertyChanged("CriticalErrorBrush");
            }
        }
        
        /// <summary>
        /// Field for property <see cref="SuccessBrush"/>
        /// </summary>
        private Brush _successBrush;
        /// <summary>
        /// Brush for SUCCESS type logs/tabs
        /// </summary>
        public Brush SuccessBrush {
            get { return _successBrush; }
            set {
                _successBrush = value;
                OnPropertyChanged("SuccessBrush");
            }
        }

        private Brush _codeFlowBrush;
        /// <summary>
        /// Brush for CODE_FLOW type logs/tabs
        /// </summary>
        public Brush CodeFlowBrush {
            get { return _codeFlowBrush; }
            set {
                _codeFlowBrush = value;
                OnPropertyChanged("CodeFlowBrush");
            }
        }

        private Brush _defaultBrush;
        /// <summary>
        /// Default brush for logs/tabs
        /// </summary>
        public Brush DefaultBrush {
            get { return _defaultBrush; }
            set {
                _defaultBrush = value;
                OnPropertyChanged("DefaultBrush");
            }
        }

        private Brush _debugBrush;
        /// <summary>
        /// Brush for DEBUG type logs/tabs
        /// </summary>
        public Brush DebugBrush {
            get { return _debugBrush; }
            set {
                _debugBrush = value;
                OnPropertyChanged("DebugBrush");
            }
        }
        
        private Brush _customBrush;
        /// <summary>
        /// Brush for DEBUG type logs/tabs
        /// </summary>
        public Brush CustomBrush {
            get { return _customBrush; }
            set {
                _customBrush = value;
                OnPropertyChanged("CustomBrush");
            }
        }

        private Brush _innerBrush;
        /// <summary>
        /// Brush for DEBUG type logs/tabs
        /// </summary>
        public Brush InnerBrush {
            get { return _innerBrush; }
            set {
                _innerBrush = value;
                OnPropertyChanged("InnerBrush");
            }
        }
        
        #endregion


        #endregion
        
        
        #region Constructor
        
        /// <summary>
        /// Constructor of Log9KSettings, sets default values for control's settings
        /// </summary>
        public Log9KSettings() {
            IsWritingToFileEnabled = true;
            IsWritingEachTabEnabled = false;
            DateTimeFormat = DEFAULT_DATE_TIME_FORMAT;
            Folder = DEFAULT_FOLDER;
            SetDefaultBrushes();
        }

        /// <summary>
        /// Set default values for brushes
        /// </summary>
        public void SetDefaultBrushes() {
            InfoBrush = DEFAULT_INFO_BRUSH;
            ErrorBrush = DEFAULT_ERROR_BRUSH;
            CriticalErrorBrush = DEFAULT_CRITICAL_ERROR_BRUSH;
            SuccessBrush = DEFAULT_SUCCESS_BRUSH;
            DefaultBrush = DEFAULT_DEFAULT_BRUSH;
            CustomBrush = DEFAULT_CUSTOM_BRUSH;
            CodeFlowBrush = DEFAULT_CODE_FLOW_BRUSH;
            DebugBrush = DEFAULT_DEBUG_BRUSH;
            InnerBrush = DEFAULT_INNER_BRUSH;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// <para>Method for loading settings from xml file.</para>
        /// <para>This file should have this structure: some root element with child elements named 'Setting'</para>
        /// <para>Elements 'Setting' should have attribute 'Id', which corresponds to name of Log9KSetting auto property</para>
        /// <para>and attribute 'Value' with value of the property.</para>
        /// </summary>
        /// <param name="path"></param>
        public bool LoadFromFile(string path) {
            XmlTextReader textReader;
            try {
                if (!File.Exists(path)) {
                    Log9KCore.InnerLog("Не найден файл настроек, будут использованы настройки по умолчанию ", true);
                    return false;
                }
                textReader = new XmlTextReader(path);
                textReader.Read();
            } catch (Exception e) {
                Log9KCore.InnerLog("При загрузке настроек произошла ошибка: " + e.Message, true);
                return false;
            }

            Type t = GetType();
            PropertyInfo[] properties = t.GetProperties();

            try {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(textReader);
                XmlNodeList settings = xmlDocument.GetElementsByTagName("Setting");
                foreach (XmlNode setting in settings) {
                    if (setting.Attributes != null) {
                        XmlAttribute id = setting.Attributes["Id"];
                        if (id == null) {
                            continue;
                        }
                        string settingName = id.Value;
                        if (setting.Attributes["Value"] == null) {
                            continue;
                        }
                        string settingValueString = setting.Attributes["Value"].Value;
                        foreach (PropertyInfo propertyInfo in properties) {
                            if (settingName == propertyInfo.Name) {
                                object settingValue = settingValueString;
                                if (propertyInfo.PropertyType == typeof (bool)) {
                                    settingValue = Convert.ToBoolean(settingValueString);
                                } else if (propertyInfo.PropertyType == typeof (Brush)) {
                                    object color = ColorConverter.ConvertFromString(settingValueString);
                                    if (color == null) {
                                        continue;
                                    }
                                    settingValue = new SolidColorBrush((Color)color);
                                }

                                propertyInfo.SetValue(this, settingValue, null);
                                break;
                            }
                        }
                    }
                }
            } catch (XmlException e) {
                Log9KCore.InnerLog("Ошибка: при загрузке настроек произошла ошибка: " + e.Message, true);
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// Save Settings to XML file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool SaveToFile(string path) {
            try {
                using (XmlWriter writer = XmlWriter.Create(path)) {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Settings");
                    
                    Type t = GetType();
                    PropertyInfo[] properties = t.GetProperties();
                    foreach (PropertyInfo propertyInfo in properties) {
                        string name = propertyInfo.Name;
                        object value = propertyInfo.GetValue(this, null);
                        writer.WriteStartElement("Setting", "");
                        writer.WriteAttributeString("Id", name);
                        writer.WriteAttributeString("Value", value.ToString());
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            } catch (Exception e) {
                Log9KCore.Instance.InnerLog("Ошибка: при сохранении настроек произошла ошибка: " + e.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Change brush/color for given log entry type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="brush"></param>
        public bool ChangeTypeColor(LogEntryTypes type, Brush brush) {
            if (brush == null) {
                return false;
            }
            string typeNameWithBrush = type.ToString().ToLower().Trim('_') + "brush";
            PropertyInfo[] properties = GetType().GetProperties();
            foreach (PropertyInfo propertyInfo in properties) {
                string propertyNameString = propertyInfo.Name.ToLower();
                if (propertyNameString == typeNameWithBrush) {
                    if (propertyInfo.PropertyType == typeof (Brush)) {
                        propertyInfo.SetValue(this, brush, null);
                        return true;
                    }
                }
            }
            return false;
        }


        #region Equals

        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }
            if (ReferenceEquals(this, obj)) {
                return true;
            }
            if (obj.GetType() != GetType()) {
                return false;
            }
            return Equals((Log9KSettings) obj);
        }
        
        /// <summary>
        /// Equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(Log9KSettings other) {
            return 
                   string.Equals(_folder, other._folder) && 
                   string.Equals(_dateTimeFormat, other._dateTimeFormat) &&
                   _isWritingToFileEnabled == other._isWritingToFileEnabled &&
                   _isWritingEachTabEnabled == other._isWritingEachTabEnabled && 
                   Equals(_infoBrush.ToString(), other._infoBrush.ToString()) &&
                   Equals(_errorBrush.ToString(), other._errorBrush.ToString()) && 
                   Equals(_criticalErrorBrush.ToString(), other._criticalErrorBrush.ToString()) &&
                   Equals(_successBrush.ToString(), other._successBrush.ToString()) && 
                   Equals(_codeFlowBrush.ToString(), other._codeFlowBrush.ToString()) &&
                   Equals(_defaultBrush.ToString(), other._defaultBrush.ToString()) && 
                   Equals(_debugBrush.ToString(), other._debugBrush.ToString()) &&
                   Equals(_customBrush.ToString(), other._customBrush.ToString());
        }
        
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {

            unchecked {
                int hashCode = (Folder != null ? Folder.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ DateTimeFormat.GetHashCode();

                hashCode = (hashCode*397) ^ IsWritingEachTabEnabled.GetHashCode();
                hashCode = (hashCode*397) ^ IsWritingToFileEnabled.GetHashCode();

                hashCode = (hashCode*397) ^ InfoBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ ErrorBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ CriticalErrorBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ SuccessBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ CodeFlowBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ DefaultBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ DebugBrush.ToString().GetHashCode();
                hashCode = (hashCode*397) ^ CustomBrush.ToString().GetHashCode();
                return hashCode;
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
