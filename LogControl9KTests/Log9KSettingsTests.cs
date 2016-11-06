using LogControl9K;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogControl9KTests {
    [TestClass]
    public class Log9KSettingsTests {

        [TestMethod]
        public void SaveSettingsToFileAndLoadThem_Test() {
            // arrange
            string filename = "test.xml";
            Log9KSettings expected = new Log9KSettings() {
                DateTimeFormat = "HH",
                IsWritingEachTabEnabled = false,
                IsWritingToFileEnabled = true,
                Folder = "kkk"
            };

            // act
            bool success = expected.SaveToFile(filename);
            if (!success) {
                Assert.Fail("SaveToFile returned false!");
            }

            Log9KSettings actual = new Log9KSettings();
            success = actual.LoadFromFile(filename);
            if (!success) {
                Assert.Fail("LoadFromFile returned false!");
            }

            // assert
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void LoadSettingsFromValidFile_Test() {
            // arrange
            Log9KSettings expected = new Log9KSettings() {
                DateTimeFormat = "G",
                IsWritingEachTabEnabled = true,
                IsWritingToFileEnabled = false,
                Folder = "666"
            };
            
            // act
            Log9KSettings actual = new Log9KSettings();
            bool success = actual.LoadFromFile("ValidSettingsFile.settings.xml");
            
            // assert
            Assert.AreEqual(true, success);
            Assert.AreEqual(expected, actual);
        }
    }
}
