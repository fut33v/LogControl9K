using System;
using System.ComponentModel;
using System.Text;
using LogControl9K.Annotations;

namespace LogControl9K {

    #region Enums

    /// <summary>
    /// Level of entry
    /// </summary>
    public enum Levels : byte {
        /// <summary>
        /// Primary level of log entry 
        /// </summary>
        PRIMARY = 0,
        /// <summary>
        /// Secondary level of log entry
        /// </summary>
        SECONDARY,
    }


    /// <summary>
    /// <para>Types for log entries</para> 
    /// <remarks>To add new type add type here and here:<see cref="TabTypes"/></remarks>
    /// </summary>
    public enum LogEntryTypes : byte {
        /// <summary>
        /// Info log entry type
        /// </summary>
        INFO = 0,
        /// <summary>
        /// Error log entry type
        /// </summary>
        ERROR,
        /// <summary>
        /// Critical error log entry type
        /// </summary>
        CRITICAL_ERROR,
        /// <summary>
        /// Success log entry type
        /// </summary>
        SUCCESS,
        /// <summary>
        /// Code flow log entry type
        /// </summary>
        CODE_FLOW,
        /// <summary>
        /// User defined log entry type
        /// </summary>
        CUSTOM,
        /// <summary>
        /// Debug log entry type
        /// </summary>
        DEBUG,
        /// <summary>
        /// Warning log entry type
        /// </summary>
        WARNING,
    }

    #endregion


    /// <summary>
    /// Log event/entry objects class
    /// </summary>
    public class Log9KEntry : INotifyPropertyChanged, IComparable {


        #region Constants

        /// <summary>
        /// All the log entries have fixed size, that is for 
        /// possibility of getting some entry from file, when we know 
        /// number of that entry
        /// </summary>
        public const int ENTRY_SIZE = 512;
        
        /// <summary>
        /// Checkout <see cref="Log9KEntry.IsMessageLong"/>
        /// </summary>
        private const uint IS_MESSAGE_LONG = 50;
        
        private const int SIZE_ID = sizeof (int);
        private const int SIZE_TYPE = sizeof(byte);
        private const int SIZE_LEVEL = sizeof (byte);
        private const int SIZE_IS_DUPLICATE = sizeof (bool);
        private const int SIZE_UNIX_TIME = sizeof (uint);
        private const int SIZE_MILLISECONDS = sizeof (int);

        private const int LENGTH_CUSTOM_TYPE = Log9KCore.MAX_CUSTOM_TYPE_NAME_LENGTH; 
        private const int SIZE_CUSTOM_TYPE = LENGTH_CUSTOM_TYPE * sizeof (char);

        private const int LENGTH_MESSAGE = SIZE_MESSAGE/sizeof (char);
        private const int SIZE_MESSAGE = (
            ENTRY_SIZE - (SIZE_ID + SIZE_TYPE + SIZE_LEVEL + SIZE_IS_DUPLICATE + SIZE_UNIX_TIME + SIZE_MILLISECONDS + SIZE_CUSTOM_TYPE)
        );        

        private static readonly int SuccessLength = LogEntryTypes.SUCCESS.ToString().Length;

        #endregion

       
        #region Static Fields

        private uint _id;
        private static byte[] _isDuplicateBytes = new byte[SIZE_IS_DUPLICATE];
        private static byte[] _typeBytes = new byte[SIZE_TYPE];
        private static byte[] _unixTimeBytes = new byte[SIZE_UNIX_TIME];
        private static byte[] _millisecondsBytes = new byte[SIZE_MILLISECONDS];
        private static byte[] _idBytes = new byte[SIZE_ID];
        private static byte[] _levelBytes = new byte[SIZE_LEVEL];
        private static byte[] _messageBytes = new byte[SIZE_MESSAGE];
        private static byte[] _customTypeBytes = new byte[SIZE_CUSTOM_TYPE];

        #endregion


        #region Properties 
        
        private static object _counterLock = new object();
        private static uint _counter = 0;
        /// <summary>
        /// Counts all the log entries
        /// </summary>
        public static uint Counter {
            get { return _counter; }
            private set {
                lock (_counterLock) {
                    _counter = value;
                }
            }
        }
        
        /// <summary>
        /// Universal identificator for log entry
        /// </summary>
        public uint ID {
            get { return _id; }
            private set {
                _id = value;
                OnPropertyChanged("ID");
            }
        }

        /// <summary>
        /// String message of log entry
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Type of log entry (debug, info etc)
        /// </summary>
        public LogEntryTypes Type { get; private set; }
        
        /// <summary>
        /// Level of message
        /// </summary>
        public Levels Level { get; private set; }

        /// <summary>
        /// Time of log event/entry happened
        /// </summary>
        public Log9KTime Time { get; private set; }

        /// <summary>
        /// Custom type name
        /// </summary>
        public string CustomType { get; private set; }

        /// <summary>
        /// Type of entry in string variant (for custom tabs is custom type name)
        /// </summary>
        public string TypeString { get; private set; }

        /// <summary>
        /// Is current entry duplicate or have duplicates
        /// </summary>
        public bool IsDuplicate { get; set; }
    
        /// <summary>
        /// Is message size long enough for showing ToolTip or something else 
        /// </summary>
        public bool IsMessageLong { get { return Message.Length > IS_MESSAGE_LONG; } }

        #endregion


        #region Constructors 

        /// <summary>
        /// 
        /// <remark>
        /// <para>Do not create entry with type CUSTOM using this constructor, ArgumentException will be raised</para>
        /// <para>Use constructor for custom type instead</para>
        /// </remark>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public Log9KEntry(LogEntryTypes type, string message, Levels level=Levels.PRIMARY) {
            if (type == LogEntryTypes.CUSTOM) {
                throw new ArgumentException(
                    "Данный конструктор не предназначен для создания пользовательских логов"
                );
            }
            Message = message;
            Type = type;
            TypeString = type.ToString();
            Time = GetCurrentLog9KTime();
            Level = level;
            if (!IsInnerLog()) {
                Counter++;
                ID = Counter;
            }
        }

        /// <summary>
        /// Constructor for custom type entries
        /// </summary>
        /// <param name="customType"></param>
        /// <param name="message"></param>
        /// <param name="level"></param>
        public Log9KEntry(string customType, string message, Levels level=Levels.PRIMARY) {
            CustomType = customType;
            TypeString = CustomType;
            Message = message;
            Type = LogEntryTypes.CUSTOM;
            Time = GetCurrentLog9KTime();
            Level = level;
            if (!IsInnerLog()) {
                Counter++;
                ID = Counter;
            }
        }
        
        /// <summary>
        /// Is inner log entry for loggin control LogControl9K
        /// </summary>
        /// <returns></returns>
        internal bool IsInnerLog() {
            if (Type == LogEntryTypes.CUSTOM && CustomType == Log9KCore.INNER_LOG_TAB) {
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// private Constructor for deserialization 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="time"></param>
        private Log9KEntry(LogEntryTypes type, string message, Log9KTime time) {
            Message = message;
            Type = type;
            Time = time;
            TypeString = type.ToString();
        }

        static Log9KEntry() {
            Counter = 0;
        }

        #endregion


        #region Public Methods


        #region Public Methods: serialization

        /// <summary>
        /// Serialization
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray() {

            byte[] idBytes = BitConverter.GetBytes(ID);

            byte[] typeBytes = new byte[1];
            typeBytes[0] = (byte)Type;

            byte[] levelBytes = new byte[1];
            levelBytes[0] = (byte)Level;
            
            byte[] isDuplicateBytes = new byte[1];
            isDuplicateBytes[0] = IsDuplicate ? (byte)1 : (byte)0;

            byte[] unixTimeBytes = BitConverter.GetBytes(Time.UnixTime);
            byte[] millisecondsBytes = BitConverter.GetBytes(Time.Milliseconds);

            byte[] customTypeBytes = new byte[SIZE_CUSTOM_TYPE];

            if (Type == LogEntryTypes.CUSTOM) {
                if (CustomType.Length > LENGTH_CUSTOM_TYPE) {
                    Log9KCore.Instance.InnerLog("Ошибка: длина имени пользовательского типа больше чем " + LENGTH_CUSTOM_TYPE);
                    return null;
                }
                byte[] customType = Log9KUtil.GetBytes(CustomType);
                Buffer.BlockCopy(customType, 0, customTypeBytes, 0, customType.Length);
            }

            byte[] messageBytes = new byte[SIZE_MESSAGE];

            string message = Message;
            if (Message.Length > LENGTH_MESSAGE) {
                message = Log9KUtil.TrimString(Message, LENGTH_MESSAGE);
            }

            byte[] messageBytesTmp = Log9KUtil.GetBytes(message);
            Buffer.BlockCopy(messageBytesTmp, 0, messageBytes, 0, messageBytesTmp.Length);

            int lengthSummary = (
                idBytes.Length + 
                typeBytes.Length + 
                levelBytes.Length +
                isDuplicateBytes.Length +
                unixTimeBytes.Length + 
                millisecondsBytes.Length + 
                customTypeBytes.Length +
                messageBytes.Length 
            );

            if (lengthSummary != ENTRY_SIZE) {
                return null;
            }
            
            byte[] entry = new byte[ENTRY_SIZE];
            Log9KUtil.SumByteArrays(
                ref entry, 
                idBytes, typeBytes, levelBytes, isDuplicateBytes, unixTimeBytes, millisecondsBytes, customTypeBytes, messageBytes 
            );
            
            return entry;
        }

        /// <summary>
        /// Deserialization
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        public static Log9KEntry FromByteArray(byte[] entry) {
            int i = 0;

            Buffer.BlockCopy(entry, i, _idBytes, 0, SIZE_ID);
            i += SIZE_ID;

            Buffer.BlockCopy(entry, i, _typeBytes, 0, SIZE_TYPE);
            i += SIZE_TYPE;
            LogEntryTypes type = (LogEntryTypes) _typeBytes[0];

            Buffer.BlockCopy(entry, i, _levelBytes, 0, SIZE_LEVEL);
            i += SIZE_LEVEL;

            Buffer.BlockCopy(entry, i, _isDuplicateBytes, 0, SIZE_IS_DUPLICATE);
            i += SIZE_IS_DUPLICATE;

            Buffer.BlockCopy(entry, i, _unixTimeBytes, 0, SIZE_UNIX_TIME);
            i += SIZE_UNIX_TIME;

            Buffer.BlockCopy(entry, i, _millisecondsBytes, 0, SIZE_MILLISECONDS);
            i += SIZE_MILLISECONDS;

            if (type == LogEntryTypes.CUSTOM) {
                Buffer.BlockCopy(entry, i, _customTypeBytes, 0, SIZE_CUSTOM_TYPE);
            }
            i += SIZE_CUSTOM_TYPE;

            Buffer.BlockCopy(entry, i, _messageBytes, 0, SIZE_MESSAGE);

            
            uint id = BitConverter.ToUInt32(_idBytes, 0);
            Levels level = (Levels) _levelBytes[0];

            bool isDuplicate = BitConverter.ToBoolean(_isDuplicateBytes, 0);
            uint unixTime = BitConverter.ToUInt32(_unixTimeBytes, 0);
            int milliseconds = BitConverter.ToInt32(_millisecondsBytes, 0);

            string message = Log9KUtil.GetString(_messageBytes);
            message = Log9KUtil.SplitStringWithNullEnding(message);

            string customType = Log9KUtil.GetString(_customTypeBytes);
            customType = Log9KUtil.SplitStringWithNullEnding(customType);

            Log9KEntry e = new Log9KEntry(type, message, new Log9KTime(unixTime, milliseconds)) {
                ID = id,
                CustomType = customType,
                IsDuplicate = isDuplicate,
                Level = level
            };
            if (type == LogEntryTypes.CUSTOM) {
                e.TypeString = customType;
            } 

            return e;
        }

        #endregion


        #region Public Overriden: Equals, GetHashCode, ToString
        
        /// <summary>
        /// Equals()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            Log9KEntry entryObj = obj as Log9KEntry;
            if (entryObj == null) {
                return false;
            }
            if (!IsInnerLog()) {
                return ID == entryObj.ID;
            }
            // All inner log entries have ID == 0
            return entryObj.GetHashCode() == GetHashCode();
        }

        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            unchecked {
                int hashCode = (Message != null ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int)Type;
                hashCode = (hashCode * 397) ^ (Time != null ? Time.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (IsDuplicate.GetHashCode());
                hashCode = (hashCode * 397) ^ (CustomType != null ? CustomType.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// CompareTo()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns>
        /// <para>- 1 if given entry is more than current</para>
        /// <para>  1 if given entry is less than current</para>
        /// <para>  1 otherwise</para>
        /// </returns>
        public int CompareTo(object obj) {
            Log9KEntry entry = obj as Log9KEntry;
            if (entry == null) {
                return 1;
            }
            if (entry.ID > ID) {
                return -1;
            } 
            return 1;
        }

        /// <summary>
        /// Method returns hash which is composed of message and type not including time
        /// Used for checking for duplicates of log entry
        /// </summary>
        /// <returns></returns>
        public int GetDuplicationHash() {
            int hashCode = (Message != null ? Message.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (int)Type;
            hashCode = (hashCode * 397) ^ (CustomType != null ? CustomType.GetHashCode() : 0);
            return hashCode;
        }

        /// <summary>
        /// This method is used when writing to log file */
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            string idString = ID.ToString();
            string typeString = Type.ToString();

            if (IsInnerLog()) {
                idString = "";
            }
            if (Type == LogEntryTypes.CUSTOM) {
                typeString = CustomType;
            }

            if (typeString.Length <= SuccessLength) {
                typeString = typeString + "\t\t\t";
            } else {
                typeString = typeString + "\t\t";
            }

            /* idString + "\t" + typeString + Time + "\t" + Message; */
            sb.Append(idString);
            sb.Append("\t");
            sb.Append(typeString);
            sb.Append(Time);
            sb.Append("\t");
            sb.Append(Message);
            return sb.ToString();
        }

        #endregion


        #endregion

    
        #region Private methods 

        /// <summary>
        /// Get current time in Log9KTime format
        /// </summary>
        /// <returns></returns>
        private static Log9KTime GetCurrentLog9KTime() {
            uint unixTimestamp = (uint)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
            int millisecond = DateTime.UtcNow.Millisecond;
            return new Log9KTime(unixTimestamp, millisecond);
        }

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
