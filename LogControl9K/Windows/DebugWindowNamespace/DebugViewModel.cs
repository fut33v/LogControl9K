using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LogControl9K.Windows.Util;

namespace LogControl9K.Windows.DebugWindowNamespace {
    class DebugViewModel : ViewModelBase {
        
        private string _start;
        public string Start {
            get {return _start; }
            set { 
                _start = value;
                OnPropertyChanged("Start"); 
            }
        }
        
        private string _end;
        public string End {
            get {return _end; }
            set { 
                _end = value;
                OnPropertyChanged("End"); 
            }
        }

        private uint _howMuchEntries;
        public uint HowMuchEntries {
            get { return _howMuchEntries; }
            set {
                _howMuchEntries = value;
                OnPropertyChanged("HowMuchEntries");
            }
        }
        

        public ObservableCollection<Tuple<uint, Log9KEntry>> Collection { get; private set; }

        public ICommand ShowTempFileContentsCommand { get; private set; }
        public ICommand HowMuchEntriesCommand { get; private set; }

        public DebugViewModel() {
            Collection = new ObservableCollection<Tuple<uint, Log9KEntry>>(); 
            ShowTempFileContentsCommand = new sCommand(ShowTempFileContentsAction);
            HowMuchEntriesCommand = new sCommand(HowMuchEntriesAction);
        }

        void HowMuchEntriesAction() {
            uint z;
            Log9KUtil.GetEntriesNumberInFile(Log9KCore.Instance.TabAll.FilenameTempFile, Log9KEntry.ENTRY_SIZE, out z);
            HowMuchEntries = z;
        }


        void ShowTempFileContentsAction() {
            Collection.Clear();
            string filename = Log9KCore.Instance.TabAll.FilenameTempFile;
            uint start, end; 
            uint.TryParse(Start, out start);
            uint.TryParse(End, out end);
            for (uint i = start; i < end; i++) {
                byte[] buffer;
                Log9KUtil.ReadFixedSizeByteArrayEntry(filename, Log9KEntry.ENTRY_SIZE, i, out buffer);
                Log9KEntry entry = Log9KEntry.FromByteArray(buffer);
                Collection.Add(new Tuple<uint, Log9KEntry>(i, entry));
            }
        }
        
    }
}
