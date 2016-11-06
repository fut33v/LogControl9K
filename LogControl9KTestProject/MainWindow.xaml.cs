using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using LogControl9K;

namespace LogControl9KTestProject {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        static Log9KCore _log9K = Log9KCore.Instance;

        public MainWindow() {
            InitializeComponent();

            SetUpLogControl9K();
            SetUpTestDataContext();

            _log9K.Warning("KAlkj;");

//            var RNEXT = 20;
//            Random r = new Random();
//            for (int j = 0; j < 3; j++) {
//                BackgroundWorker bw = new BackgroundWorker();
//                int threadNumber = j;
//                var types = Log9KUtil.EnumUtil.GetValues<LogEntryTypes>().ToList();
////                string threadNumber = "";
//                bw.DoWork += (sender, args) => {
//                    for ( ; ; ){
//                        for (int i = 0; i < 5; i++) {
//                            var randomType = types[r.Next(types.Count - 1)];
//                            if (randomType != LogEntryTypes.CUSTOM) {
//                                _log9K.Log(randomType, "Message. " + r.Next(100) + " Some message in thread #" + threadNumber);
//                            }
//                            _log9K.Log("Console", "Testin! " + threadNumber);
//                        }
//                        Thread.Sleep(r.Next(1000)); 
//                    }
//                };
//                bw.RunWorkerAsync();
//            }
        }

        private void SetUpTestDataContext() {
            Message = "Message";
            Type = LogEntryTypes.INFO;
            LogEntryTypesList = new ObservableCollection<LogEntryTypes> {
                LogEntryTypes.CRITICAL_ERROR,
                LogEntryTypes.ERROR, 
                LogEntryTypes.INFO,
                LogEntryTypes.SUCCESS,
                LogEntryTypes.CODE_FLOW,
                LogEntryTypes.WARNING
            };
            DataContext = this;

            AllahAkbar("something", 12);
        }

        private void AllahAkbar(string something, int i)
        {
            _log9K.Debug_f("{0} Testing  {1}", something, i);
        }

        private static void SetUpLogControl9K() {
            Log9KCore.Settings.DateTimeFormat = "HH:mm:ss.fff";

            Log9KCore.Instance.AddCustomType("CAMERA0");
            Log9KCore.Instance.AddCustomType("BABAX");
            Log9KCore.Instance.AddCustomType("Console");
            
            Log9KCore.Instance.SetTabOrder(TabTypes.ALL, TabTypes.DEBUG, TabTypes.CRITICAL_ERROR, "Console", "CAMERA0", TabTypes.WARNING);

            Log9KCore.Instance.SetTabHeader(TabTypes.ALL, "ALL");
            Log9KCore.Instance.AddCustomType("CAMERA1");
        }


        #region UI stuff for testing LogControl9K

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (!string.IsNullOrEmpty(Message)) {
                Log9KCore.Instance.Log(Type, Message);
                TabTypes tabType;
                bool s = Enum.TryParse(Type.ToString(), out tabType);
                if (s) {
                    Log9KCore.Instance.SetActiveTab(tabType);
                }
            }
        }

        public ObservableCollection<LogEntryTypes> LogEntryTypesList { get; private set; }
        public string Message { get; set; }
        public LogEntryTypes Type { get; set; }

        #endregion

        private void Log9KControl_Loaded(object sender, RoutedEventArgs e) {

        }


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            _log9K.Log(LogEntryTypes.INFO, "");
            _log9K.Log("Console", "test");
            _log9K.SetActiveTab("Console");
        }

        private void TEST(object sender, RoutedEventArgs e)
        {
            _log9K.Log_f(LogEntryTypes.SUCCESS, "abc {0} {2} {1}", 1, 2, 3);
            _log9K.Error_f("abc {0} {2} {1}", 1, 3, 3);
            _log9K.Debug();
        }
    }
}




