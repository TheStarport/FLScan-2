using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A class used for logging a wrong number of arguments
    /// For example:
    /// <example>
    /// [Ship]
    /// camera_offset = 50, 10
    /// </example>
    /// camera_offset has to have 2 values, one for vertical and one for horizontal.
    /// </summary>
    public class InvalidValueCountLogEntry : IniErrorLogEntry
    {
        private int foundValueCount;
        private int[] expectedValueCount;

        public int FoundValueCount
        {
            get { return foundValueCount; }
            set { foundValueCount = value; }
        }

        public int[] ExpectedValueCount
        {
            get { return expectedValueCount; }
            set { expectedValueCount = value; }
        }

        public InvalidValueCountLogEntry(string file, FLDataFile.Setting setting, params int[] expectedValueCount) : base("Invalid value-count!", file, setting)
        {
            this.FoundValueCount = setting.NumValues();
            this.ExpectedValueCount = expectedValueCount;
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} expected: ({1}), found {2}", base.ToString(), new string(ExpectedValueCount.SelectMany(i => i.ToString() + ",").Take(ExpectedValueCount.Length*2-1).ToArray()), FoundValueCount);
        }
    }
}
