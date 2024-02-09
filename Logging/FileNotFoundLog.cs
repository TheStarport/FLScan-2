using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A class for logging non-existent, but referenced files.
    /// </summary>
    public class FileNotFoundLogEntry : IniErrorLogEntry
    {
        private string missingFile;
        public string MissingFile
        {
            get { return missingFile; }
            set { missingFile = value; }
        }

        public FileNotFoundLogEntry(string file, string missingFile, FLDataFile.Setting setting) : base("File not found!", file, setting)
        {
            MissingFile = missingFile;
        }

        public FileNotFoundLogEntry(string file, FLDataFile.Setting setting) : base("File not found!", file, setting)
        {
            MissingFile = setting.Str(0);
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("{0} {1}", base.ToString(), missingFile);
        }
    }
}
