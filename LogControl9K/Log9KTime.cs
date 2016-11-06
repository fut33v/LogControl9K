using System;

namespace LogControl9K {
    /// <summary>
    /// Class for time of log entry event happened
    /// </summary>
    public class Log9KTime : IComparable {


        #region Properties

        /// <summary>
        /// Unix timestamp
        /// </summary>
        public uint UnixTime { get; private set; }
        /// <summary>
        /// Number of milliseconds
        /// </summary>
        public int Milliseconds { get; private set; }

        #endregion


        #region Constructor

        /// <summary>
        /// Create Log9KTime instance from Unix timestamp and milliseconds
        /// </summary>
        /// <param name="unixTime"></param>
        /// <param name="milliseconds"></param>
        public Log9KTime(uint unixTime, int milliseconds) {
            UnixTime = unixTime;
            Milliseconds = milliseconds;
        }

        #endregion

        
        #region Public Methods

        /// <summary>
        /// ToString()
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            DateTime dt = Log9KUtil.UnixTimeStampToDateTime(UnixTime);
            dt = dt.AddMilliseconds(Milliseconds);
            string dateTimeFormat = Log9KCore.Settings.DateTimeFormat;
            return dt.ToString(dateTimeFormat);
        }

        /// <summary>
        /// Convert to DateTime
        /// </summary>
        /// <returns></returns>
        public DateTime ToDateTime() {
            DateTime dt = Log9KUtil.UnixTimeStampToDateTime(UnixTime);
            dt = dt.AddMilliseconds(Milliseconds);
            return dt;
        }
        
        /// <summary>
        /// CompareTo()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj) {
            Log9KTime log9KTime = obj as Log9KTime;

            if (log9KTime == null) {
                return 666;
            }

            if (log9KTime.UnixTime > UnixTime) {
                return 359;
            }
            if (log9KTime.UnixTime < UnixTime) {
                return -228;
            }
            if (log9KTime.Milliseconds > Milliseconds) {
                return 359;
            }
            if (log9KTime.Milliseconds < Milliseconds) {
                return -228;
            }
            return 0;
        }

        /// <summary>
        /// Equals()
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) {
            return CompareTo(obj) == 0;
        }
        
        /// <summary>
        /// GetHashCode()
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            unchecked {
                return ((int) UnixTime*397) ^ Milliseconds;
            }
        }

        #endregion


    }
}
