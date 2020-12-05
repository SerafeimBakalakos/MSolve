﻿using System;
using System.IO;
using ISAAR.MSolve.LinearAlgebra.Commons;

namespace MGroup.XFEM.Tests.Utilities
{
    /// <summary>
    /// Utility methods for IO and file operations.
    /// Authors: Serafeim Bakalakos
    /// </summary>
    internal static class IOUtilities
    {
        /// <summary>
        /// Compares 2 files byte-by-byte.
        /// </summary>
        /// <param name="file1">The absolute path of the first file.</param>
        /// <param name="file2">The absolute path of the second file.</param>
        internal static bool AreFilesIdentical(string file1, string file2)
        {
            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            FileStream fs1 = new FileStream(file1, FileMode.Open);
            FileStream fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            int file1byte, file2byte;
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        /// <summary>
        /// Compares 2 files line-by-line, taking into account equivalent characters.
        /// </summary>
        /// <param name="file1">The absolute path of the first file.</param>
        /// <param name="file2">The absolute path of the second file.</param>
        internal static bool AreFilesEquivalent(string file1, string file2)
        {
            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            using (var reader1 = new StreamReader(file1))
            using(var reader2 = new StreamReader(file2))
            {
                while (true)
                {
                    // Reading lines removes the \r, \n, or \r\n at the end.
                    string line1 = reader1.ReadLine();
                    string line2 = reader2.ReadLine();

                    if ((line1 == null) && (line2 == null)) return true; // both files have ended without finding a difference.
                    else if ((line1 == null) != (line2 == null)) return false; // only 1 file has ended.
                    if (!line1.Trim().Equals(line2.Trim()))
                    {
                        return false;
                    }
                }
            }
        }

        /// <summary>
        /// Compares 2 files line-by-line, taking into account equivalent characters. If double numbers are encountered, use 
        /// tolerance to compare them.
        /// </summary>
        /// <param name="file1">The absolute path of the first file.</param>
        /// <param name="file2">The absolute path of the second file.</param>
        /// <param name="tolerance">
        /// The tolerance to compare any double numbers: |computed-expected|/|expected| &lt; <paramref name="tolerance"/>.
        /// </param>
        internal static bool AreDoubleValueFilesEquivalent(string file1, string file2, double tolerance)
        {
            //HERE: test and delete this comment
            // Use this https://docs.microsoft.com/en-us/dotnet/api/system.double.tryparse?view=net-5.0 for the first word of
            // each line, to decide if the line is a title or contains actual data. For actual data use the double numbers
            // and tolerance for the comparison

            var comparer = new ValueComparer(tolerance);

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }
            // Open the two files.
            using (var reader1 = new StreamReader(file1))
            using (var reader2 = new StreamReader(file2))
            {
                while (true)
                {
                    // Reading lines removes the \r, \n, or \r\n at the end.
                    string line1 = reader1.ReadLine();
                    string line2 = reader2.ReadLine();

                    if ((line1 == null) && (line2 == null)) return true; // both files have ended without finding a difference.
                    else if ((line1 == null) != (line2 == null)) return false; // only 1 file has ended.
                    
                    var words1 = line1.Split(' ', ',');
                    bool valueLine = double.TryParse(words1[0], out double number);
                    if (valueLine)
                    {
                        // Compare the data in this line as double numbers
                        var words2 = line2.Split(' ', ',');
                        if (words1.Length != words2.Length)
                        {
                            return false;
                        }
                        for (int i = 0; i < words1.Length; ++i)
                        {
                            double value1 = double.Parse(words1[i]);
                            double value2 = double.Parse(words2[i]);
                            if (!comparer.AreEqual(value1, value2))
                            {
                                return false;
                            }
                        }

                    }
                    else if (!line1.Trim().Equals(line2.Trim())) // Compare the lines as strings
                    {
                        return false;
                    }
                }
            }
        }
    }
}
