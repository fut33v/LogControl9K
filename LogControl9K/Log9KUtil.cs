using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LogControl9K {
    /// <summary>
    /// Class with method-utilities that used by LogControl9K classes
    /// </summary>
    public class Log9KUtil {
        

        #region Directories
        
        /// <summary>
        /// Create directory if it not exists
        /// </summary>
        /// <param name="dirname"></param>
        public static void CreateDirIfNotExists(string dirname) {
            bool exists = Directory.Exists(dirname);
            if (!exists) {
                Directory.CreateDirectory(dirname);
            }
        }
        
        /// <summary>
        /// Creates dirs in given path with filename in end
        /// </summary>
        /// <param name="path">Path in format: blah/blah/file.txt </param>
        public static void CreateDirsInPath(string path) {
            string f = Path.GetFileName(path);
            if (f == null) {
                return;
            }
            string dirsPath = path.Remove(path.Length - f.Length);
            Directory.CreateDirectory(dirsPath);
        }

        #endregion


        #region Path

        /// <summary>
        /// Check path for valid
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        static public bool IsPathValid(string path) {
           FileInfo fi = null;
            try {
              fi = new FileInfo(path);
            }
            catch (ArgumentException) { }
            catch (PathTooLongException) { }
            catch (NotSupportedException) { }

            if (ReferenceEquals(fi, null)) {
                return false; // file name is not valid
            } 
            return true;  // file name is valid... May check for existence by calling fi.Exists.
        }

        #endregion


        #region Datetime methods

        /// <summary>
        /// Get current day month year string 
        /// </summary>
        /// <returns></returns>
        public static string GetDayMonthYearString() {
            return DateTime.Now.ToString("dd_MM_yy");
        }

        /// <summary>
        /// Get current day month year hour minute second string 
        /// </summary>
        /// <returns></returns>
        public static string GetDayMonthYearHourMinuteSecondString() {
            return DateTime.Now.ToString("dd_MM_yy__HH_mm_ss");
        }

        /// <summary>
        /// Convert unixtime to DateTime
        /// </summary>
        /// <param name="unixTimeStamp"></param>
        /// <returns></returns>
        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp) {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        #endregion

        
        #region Strings stuff

        /// <summary>
        /// Append string to file
        /// </summary>
        /// <param name="filename">path to file</param>
        /// <param name="stroka">string to append</param>
        /// <returns></returns>
        public static bool AppendStringToFile(string filename, string stroka) {
            int tryTimes = 0;
            while (true) {
                try {
                    using (StreamWriter sw = File.AppendText(filename)) {
                        sw.WriteLine(stroka);
                    }
                    return true;
                } catch (DirectoryNotFoundException) {
                    CreateDirsInPath(filename);
                }
                catch (IOException) {
                    tryTimes++;
                    if (tryTimes >= MAX_TRY_COUNT) {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Max number of attempts to write in file 
        /// </summary>
        private const int MAX_TRY_COUNT = 10;

        
        /// <summary>
        /// Get first part of string which consists of some content and ending parts with nulls
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string SplitStringWithNullEnding(string s) {
            string[] splitted = s.Split(char.MinValue);
            if (splitted.Length > 0) {
                return splitted[0];
            } else {
                return s;
            }
        }

        #endregion


        #region Byte arrays stuff 
        
        /// <summary>
        /// Method takes first given bytes of given byte array
        /// </summary>
        /// <param name="bytes">Byte array to take bytes</param>
        /// <param name="size">Number of bytes to take</param>
        public static void SliceByteArray(ref byte[] bytes, int size) {
            IEnumerable<byte> sliced = bytes.Take(size);
            bytes = sliced.ToArray();
        }

        /// <summary>
        /// Appends byte array to end of file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static bool AppendByteArrayToFile(string filename, byte[] bytes) {
            if (bytes == null) {
                return false;
            }

            try {
                using (FileStream stream = new FileStream(filename, FileMode.Append)) {
                    stream.Write(bytes, 0, bytes.Length);
                }
            } catch (IOException) {
                return false;
            }

            return true;
        }

        /// <summary>
        /// <para>Read byte array from file (file of same sized arrays)</para>
        /// <para>with given offset which is defined by size and number of array in that file</para>
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="size"></param>
        /// <param name="entryNumber"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static bool ReadFixedSizeByteArrayEntry(string filename, int size, uint entryNumber, out byte[] buffer) {
            buffer = new byte[size];
            if (filename == null || size == 0) {
                return false;
            }
            try {
                using (Stream stream = File.Open(filename, FileMode.Open)) {
                    stream.Seek(size*entryNumber, SeekOrigin.Begin);
                    int numberOfBytes = stream.Read(buffer, 0, size);
                    if (numberOfBytes < size) {
                        return false;
                    }
                }
            } catch (IOException) {
                return false;
            }
            return true;
        }
        
        /// <summary>
        /// Get number of fixed size byte entries in given file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="entrySize">size of one entry</param>
        /// <param name="entriesNumber">how many entries in file</param>
        /// <returns>Returns true if everything is OK</returns>
        public static bool GetEntriesNumberInFile(string filename, int entrySize, out uint entriesNumber) {
            entriesNumber = 0;
            if (!File.Exists(filename)) {
                return false;
            }
            FileInfo fi = new FileInfo(filename);
            if (fi.Length%entrySize != 0) {
                return false;
            }
            entriesNumber = (uint) (fi.Length/entrySize);
            return true;
        }

        /// <summary>
        /// Concatenate byte arrays in one
        /// </summary>
        /// <param name="sumBytes"></param>
        /// <param name="args"></param>
        public static void SumByteArrays(ref byte[] sumBytes, params byte[][] args) {
            int length = 0;
            foreach (byte[] ba in args) {
                length += ba.Length;
            }
            if (sumBytes.Length < length) {
                throw new ArgumentException("Byte array is too small!"); 
            }
            int offset = 0;
            foreach (byte[] ba in args) {
                Buffer.BlockCopy(ba, 0, sumBytes, offset, ba.Length);
                offset += ba.Length;
            }
        }
        
        /// <summary>
        /// Get bytes of string (serialization)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] GetBytes(string str) {
            if (string.IsNullOrEmpty(str)) {
                return null;
            }
            byte[] bytes = new byte[str.Length * sizeof(char)];
            Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        
        /// <summary>
        /// Trims string to startofstring...endofstring if it is more than given argument
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="trimTo">Size of result string</param>
        /// <returns></returns>
        public static string TrimString(string str, int trimTo) {
            if (str.Length <= trimTo) {
                return str;
            }
            if (trimTo < 8) {
                return str.Substring(0, trimTo);
            }

            const string middleStringPart = "...";
            const int lastStringPartSize = 4;

            int firstStringPartSize = trimTo - middleStringPart.Length - lastStringPartSize;
            string firstStringPart = str.Substring(0, firstStringPartSize);
            string lastStringPart = str.Substring(str.Length - lastStringPartSize, lastStringPartSize);

            return firstStringPart + middleStringPart + lastStringPart;
        }


        /// <summary>
        /// Get string from bytes array (deserialization)
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string GetString(byte[] bytes) {
            /* If bytes was cropped to odd length, there will be out of bound error */
            int charsNumber;
            if (bytes.Length%2 != 0) {
                charsNumber = bytes.Length + 1/sizeof (char);
            } else {
                charsNumber = bytes.Length / sizeof (char);
            }

            char[] chars = new char[charsNumber];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        #endregion


        /// <summary>
        /// Invoke code in UI thread (Main)
        /// </summary>
        /// <param name="a"></param>
        public static void InvokeInUiThread(Action a) {
            if (Application.Current == null) {
                return;
            }
            Application.Current.Dispatcher.Invoke(a);
        }
        
        /// <summary>
        /// Invoke code in UI thread (Main) asynchronously
        /// </summary>
        /// <param name="a"></param>
        public static void BeginInvokeInUiThread(Action a) {
            if (Application.Current == null) {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(a);
        }

        
        /// <summary>
        /// <para>Checks command for null and executes it</para>
        /// <para>(not checks CanExecute)</para>
        /// </summary>
        /// <param name="command"></param>
        public static void ExecuteCommand(ICommand command) {
            if (command != null) {
                command.Execute(null);
            } else {
                Log9KCore.Instance.InnerLog("Ошибка: команда (MVVM) равна null");
            }
        }


        #region Enums

        /// <summary>
        /// Util class for iterating through enums 
        /// http://stackoverflow.com/questions/972307/can-you-loop-through-all-enum-values
        /// </summary>
        public static class EnumUtil {
            /// <summary>
            /// Get IEnumerable of enum values to iterate through
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            public static IEnumerable<T> GetValues<T>() {
                return Enum.GetValues(typeof(T)).Cast<T>();
            }
        }

        #endregion

    }


    #region Extensions

    /// <summary>
    /// <para>This class is used for scrolling to center of TreeView, source:</para>
    /// <para>http://stackoverflow.com/questions/2946954/make-listview-scrollintoview-scroll-the-item-into-the-center-of-the-listview-c </para>
    /// </summary>
    public static class ItemsControlExtensions {

        /// <summary>
        /// Scroll item in center of view of items control
        /// </summary>
        /// <param name="itemsControl"></param>
        /// <param name="item"></param>
        public static void ScrollToCenterOfView(this ItemsControl itemsControl, object item) {
            // Scroll immediately if possible
            if (!itemsControl.TryScrollToCenterOfView(item)) {
                // Otherwise wait until everything is loaded, then scroll
                if (itemsControl is ListBox) ((ListBox)itemsControl).ScrollIntoView(item);
                itemsControl.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
                      itemsControl.TryScrollToCenterOfView(item);
                  }));
            }
        }

        private static bool TryScrollToCenterOfView(this ItemsControl itemsControl, object item) {
            // Find the container
            UIElement container = itemsControl.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
            if (container == null) return false;

            // Find the ScrollContentPresenter
            ScrollContentPresenter presenter = null;
            for (Visual vis = container; vis != null && vis != itemsControl; vis = VisualTreeHelper.GetParent(vis) as Visual)
                if ((presenter = vis as ScrollContentPresenter) != null)
                    break;
            if (presenter == null) return false;

            // Find the IScrollInfo
            IScrollInfo scrollInfo =
                !presenter.CanContentScroll ? presenter :
                presenter.Content as IScrollInfo ??
                FirstVisualChild(presenter.Content as ItemsPresenter) as IScrollInfo ??
                presenter;

            // Compute the center point of the container relative to the scrollInfo
            Size size = container.RenderSize;
            Point center = container.TransformToAncestor((Visual)scrollInfo).Transform(new Point(size.Width / 2, size.Height / 2));
            center.Y += scrollInfo.VerticalOffset;
            center.X += scrollInfo.HorizontalOffset;

            // Adjust for logical scrolling
            if (scrollInfo is StackPanel || scrollInfo is VirtualizingStackPanel) {
                double logicalCenter = itemsControl.ItemContainerGenerator.IndexFromContainer(container) + 0.5;
                Orientation orientation = scrollInfo is StackPanel ? ((StackPanel)scrollInfo).Orientation : ((VirtualizingStackPanel)scrollInfo).Orientation;
                if (orientation == Orientation.Horizontal)
                    center.X = logicalCenter;
                else
                    center.Y = logicalCenter;
            }

            // Scroll the center of the container to the center of the viewport
            if (scrollInfo.CanVerticallyScroll) scrollInfo.SetVerticalOffset(CenteringOffset(center.Y, scrollInfo.ViewportHeight, scrollInfo.ExtentHeight));
            if (scrollInfo.CanHorizontallyScroll) scrollInfo.SetHorizontalOffset(CenteringOffset(center.X, scrollInfo.ViewportWidth, scrollInfo.ExtentWidth));
            return true;
        }

        private static double CenteringOffset(double center, double viewport, double extent) {
            return Math.Min(extent - viewport, Math.Max(0, center - viewport / 2));
        }
        private static DependencyObject FirstVisualChild(Visual visual) {
            if (visual == null) return null;
            if (VisualTreeHelper.GetChildrenCount(visual) == 0) return null;
            return VisualTreeHelper.GetChild(visual, 0);
        }
    }
    
    /// <summary>
    /// <para>Helps to focus on element with MVVM</para>
    /// <para>http://stackoverflow.com/a/1356781/5147998 </para>
    /// </summary>
    public static class FocusExtension {
        public static bool GetIsFocused(DependencyObject obj) {
            return (bool)obj.GetValue(IsFocusedProperty);
        }


        public static void SetIsFocused(DependencyObject obj, bool value) {
            obj.SetValue(IsFocusedProperty, value);
        }


        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
             "IsFocused", typeof(bool), typeof(FocusExtension),
             new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));


        private static void OnIsFocusedPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e) {
            var uie = (UIElement)d;
            if ((bool)e.NewValue) {
                uie.Focus(); // Don't care about false values.
            }
        }
    }

    /// <summary>
    /// Extension for lists
    /// </summary>
    public static class ListExtension {
        /// <summary>
        /// Bubble sort current list
        /// </summary>
        /// <param name="o"></param>
        public static void BubbleSort(this IList o) {
            for (int i = o.Count - 1; i >= 0; i--) {
                for (int j = 1; j <= i; j++) {
                    object o1 = o[j - 1];
                    object o2 = o[j];
                    if (((IComparable)o1).CompareTo(o2) > 0) {
                        o.Remove(o1);
                        o.Insert(j, o1);
                    }
                }
            }
        }
    }

    #endregion

}
