using LogControl9K;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogControl9KTests {
    [TestClass]
    public class Log9KEntryTest {
        [TestMethod]
        public void SerializationDeserializationCustomTypeLogEntry_Test() {
            // arrange
            Log9KEntry expected = new Log9KEntry("CONSOLE", "message", Levels.SECONDARY);
            
            // act
            byte[] e = expected.ToByteArray();
            Log9KEntry actual = Log9KEntry.FromByteArray(e);
            
            // assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SerializationDeserializationErrorTypeLogEntry_Test() {
            // arrange
            Log9KEntry expected = new Log9KEntry(LogEntryTypes.ERROR, "message", Levels.SECONDARY);
            
            // act
            byte[] e = expected.ToByteArray();
            Log9KEntry actual = Log9KEntry.FromByteArray(e);
            
            // assert
            Assert.AreEqual(expected, actual);
        }
    }
}
