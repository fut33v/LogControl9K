using System.Collections.Generic;
using System.Linq;
using LogControl9K;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogControl9KTests {
    [TestClass]
    public class Log9KTabTests {
        [TestMethod]
        public void AddEntriesToLog9KTabCollectionAndInsertAt0IsSorted_Test() {
            // arrange
            Log9KEntry e1 = new Log9KEntry(LogEntryTypes.INFO, "1"); // ID=1
            Log9KEntry e2 = new Log9KEntry(LogEntryTypes.INFO, "2"); // ID=2
            Log9KEntry e3 = new Log9KEntry(LogEntryTypes.INFO, "3"); // ID=3
            Log9KEntry e4 = new Log9KEntry(LogEntryTypes.INFO, "4"); // ID=4
            List<Log9KEntry> expected = new List<Log9KEntry>() {e1, e2, e3, e4};
            Log9KTabObservableCollection c = new Log9KTabObservableCollection(100u);
            c.Add(e1);
            c.Add(e2);
            c.Add(e4);

            // act
            c.Insert(0, e3);
            List<Log9KEntry> actual = c.ToList();

            // assert
            CollectionAssert.AreEqual(expected, actual);
        }
    }
}
