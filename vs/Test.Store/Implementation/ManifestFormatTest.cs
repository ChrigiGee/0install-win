﻿/*
 * Copyright 2010 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using System.IO;
using Common.Helpers;
using NUnit.Framework;

namespace ZeroInstall.Store.Implementation
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestFormat"/>.
    /// </summary>
    [TestFixture]
    public class ManifestFormatTest
    {
        #region Helpers
        /// <summary>
        /// Creates a temporary directory containing a files named "x.txt" and "Y.txt" and a sub-directory named "directory" with a file named "file.txt".
        /// </summary>
        /// <returns>The path of the directory</returns>
        internal static string CreateArtificialPackage()
        {
            string packageDir = FileHelper.GetTempDirectory();
            File.WriteAllText(Path.Combine(packageDir, "x.txt"), @"AAA");
            File.WriteAllText(Path.Combine(packageDir, "Y.txt"), @"AAA");
            var subdir = Directory.CreateDirectory(Path.Combine(packageDir, "directory"));
            File.WriteAllText(Path.Combine(subdir.FullName, "file.txt"), @"AAA");

            return packageDir;
        }
        #endregion

        /// <summary>
        /// Ensures that <see cref="ManifestFormat.GetSortedDirectoryEntries"/> correctly sorts and lists file system entries.
        /// </summary>
        [Test]
        public void TestGetSortedDirectoryEntries()
        {
            // Create a test directory to analyze
            string tempDir = CreateArtificialPackage();

            try
            {
                // In the old format sub-directories and files are sorted together, so "directory" comes before "x.txt"
                var entries = new List<FileSystemInfo>(ManifestFormat.Sha1Old.GetSortedDirectoryEntries(tempDir));
                Assert.AreEqual("Y.txt", entries[0].Name); // Capital letters come before lowercase letters
                Assert.AreEqual("directory", entries[1].Name);
                Assert.AreEqual("file.txt", entries[2].Name);
                Assert.AreEqual("x.txt", entries[3].Name);

                // In the new format sub-directories are listed after files, so "Y.txt" and "x.txt" come before "directory"
                entries = new List<FileSystemInfo>(ManifestFormat.Sha1New.GetSortedDirectoryEntries(tempDir));
                Assert.AreEqual("Y.txt", entries[0].Name);
                Assert.AreEqual("x.txt", entries[1].Name);
                Assert.AreEqual("directory", entries[2].Name); // Capital letters come before lowercase letters
                Assert.AreEqual("file.txt", entries[3].Name);
            }
            finally
            { // Clean up
                Directory.Delete(tempDir, true);
            }
        }
    }
}
