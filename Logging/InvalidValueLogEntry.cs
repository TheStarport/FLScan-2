using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A class for logging invalid values in INI-files
    /// For example missing hardpoints
    /// </summary>
    public class InvalidValueLogEntry : IniErrorLogEntry
    {
        private string foundValue;

        public string FoundValue
        {
            get { return foundValue; }
            set { foundValue = value; }
        }

        public InvalidValueLogEntry(string message, string file, FLDataFile.Setting setting, string foundValue) : base(message, file, setting)
        {
            this.FoundValue = foundValue;
        }

        public InvalidValueLogEntry(string message, string file, FLDataFile.Setting setting) : this(message, file, setting, setting.Str(0))
        {
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} {1}", base.ToString(), foundValue);
        }
    }
}
