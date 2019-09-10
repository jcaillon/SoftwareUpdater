#region header
// ========================================================================
// Copyright (c) 2018 - Julien Caillon (julien.caillon@gmail.com)
// This file (SimpleFileUpdaterTest.cs) is part of SoftwareUpdater.Test.
//
// SoftwareUpdater.Test is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SoftwareUpdater.Test is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SoftwareUpdater.Test. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SoftwareUpdater.Test {

    [TestClass]
    public class SimpleFileUpdaterTest {

        private static string _testFolder;
        private static string TestFolder => _testFolder ?? (_testFolder = TestHelper.GetTestFolder(nameof(SimpleFileUpdaterTest)));

        [ClassInitialize]
        public static void Init(TestContext context) {
            Cleanup();
            Directory.CreateDirectory(TestFolder);
        }

        [ClassCleanup]
        public static void Cleanup() {
            if (Directory.Exists(TestFolder)) {
                Directory.Delete(TestFolder, true);
            }
        }

        [TestMethod]
        public void Test() {
            var updater = SimpleFileUpdater.Instance;
            updater.TryToCleanLastExe();

            File.WriteAllText(Path.Combine(TestFolder, "file"), "");

            updater.AddFileToMove(Path.Combine(TestFolder, "file"), Path.Combine(TestFolder, "file2"));

            Assert.IsFalse(File.Exists(Path.Combine(TestFolder, "file2")));

            updater.Start();
            updater.Start(-1);

            updater.WaitForProcessExit();

            Assert.IsTrue(File.Exists(Path.Combine(TestFolder, "file2")));

            updater.TryToCleanLastExe();
        }
    }
}
