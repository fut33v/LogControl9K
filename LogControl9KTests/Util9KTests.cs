using System;
using LogControl9K;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LogControl9KTests {
    [TestClass]
    public class Util9KTests {
        [TestMethod]
        public void SumByteArrays_Three_Arrays() {
            // arrange
            byte[] first = {6, 6, 6};
            byte[] second = {1, 2};
            byte[] third = {3, 0, 1, 5, 9};
            byte[] expected = {6, 6, 6, 1, 2, 3, 0 , 1, 5, 9}; 
            byte[] actual = new byte[first.Length + second.Length + third.Length];
            // act
            Log9KUtil.SumByteArrays(ref actual, first, second, third);
            // assert
            CollectionAssert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SumByteArrays_Three_Arrays_Result_Array_Not_Big_Enough() {
            // arrange
            byte[] first = {6, 6, 6};
            byte[] second = {1, 2};
            byte[] third = {3, 0, 1, 5, 9};
            byte[] actual = new byte[first.Length + second.Length + third.Length - 2];
            // act
            Log9KUtil.SumByteArrays(ref actual, first, second, third);
            // assert is handled by the ExpectedException
        }

        [TestMethod]
        public void SplitStringWithNullEnding_Test() {
            // arrange
            string s = "abcd";
            string expected = s;
            s += "\0";
            s += "\0";
            s += "\0";
            s += "\0";
            // act 
            string actual = Log9KUtil.SplitStringWithNullEnding(s);
            // assert
            Assert.AreEqual(actual, expected);
        }

        [TestMethod]
        public void SliceByteArray_Five_Elements_Array_Test() {
            // arrange
            byte[] actual = {1, 2, 3, 4, 5};
            byte[] expected = {1, 2, 3};
            int size = 3;
            // act
            Log9KUtil.SliceByteArray(ref actual, size); 
            // assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void SliceByteArray_Zero_Elements_Test() {
            // arrange
            byte[] actual = {1, 2, 3, 4, 5};
            byte[] expected = {};
            int size = 0;
            // act
            Log9KUtil.SliceByteArray(ref actual, size); 
            // assert
            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void ByteArraySerialization_Test() {
            // arrange 
            string filename = "ByteArraySerialization_Test.test";
            byte[] expected = {3, 5, 9};
            int arraySize = expected.Length, numberOfArrays = 5;
            // act
            for (int i = 0; i < numberOfArrays; i++) {
                Log9KUtil.AppendByteArrayToFile(filename, expected);
            }
            // assert
            for (uint i = 0; i < numberOfArrays; i++) {
                byte[] actual;
                Log9KUtil.ReadFixedSizeByteArrayEntry(filename, arraySize, i, out actual);
                CollectionAssert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void StringSerialization_Test() {
            // arrange
            const string expected = "Allah Акбар";
            // act
            byte[] actualBytes = Log9KUtil.GetBytes(expected);
            string actual = Log9KUtil.GetString(actualBytes);
            // assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TrimStringBiggerThanTrimTo_Test() {
            // arrange
            const string expected = "Abcdabcd...asdf";
            
            // act
            const string inputString = "Abcdabcd tidish asdf";
            string actual = Log9KUtil.TrimString(inputString, expected.Length);
            
            // assert
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void TrimStringLessThanTrimTo_Test() {
            // arrange
            const string expected = "Abcdabcdasdf";
            
            // act
            const string inputString = "Abcdabcdasdf";
            string actual = Log9KUtil.TrimString(inputString, expected.Length + 5);
            
            // assert
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod]
        public void TrimStringWithTrimToEquals8_Test() {
            // arrange
            const string expected = "A...asdf";
            
            // act
            const string inputString = "Abcdabcdasdf";
            string actual = Log9KUtil.TrimString(inputString, 8);
            
            // assert
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void TrimStringWithTrimToLessThan8_Test() {
            // arrange
            const string expected = "Abcdabc";
            
            // act
            const string inputString = "Abcdabcdasdf";
            string actual = Log9KUtil.TrimString(inputString, 7);
            
            // assert
            Assert.AreEqual(expected, actual);
        }

    }
}
