using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A abstract class for errors connected with INI-files
    /// </summary>
    public abstract class IniErrorLogEntry : LogEntry
    {
        private string file;
        private int lineNumber;
        private string section;
        private string setting;

        /// <summary>
        /// The INI-file
        /// </summary>
        public string File
        {
            get { return file; }
            set { file = value; }
        }

        /// <summary>
        /// The linenumber in <c>File</c>
        /// </summary>
        public int LineNumber
        {
            get { return lineNumber; }
            set { lineNumber = value; }
        }

        /// <summary>
        /// The section-name which caused the error
        /// </summary>
        public string Section
        {
            get { return section; }
            set { section = value; }
        }

        /// <summary>
        /// The setting which caused the error
        /// </summary>
        public string Setting
        {
            get { return setting; }
            set { setting = value; }
        }

        public IniErrorLogEntry(string message, string file, int lineNumber, string section, string setting) : base(message, LogLevel.error)
        {
            this.File = file;
            this.LineNumber = lineNumber;
            this.Section = section;
            this.Setting = setting;
        }

        public IniErrorLogEntry(string message, string file, FLDataFile.Setting setting) : this(message, file, setting.LineNumber, setting.sectionName, setting.settingName)
        {
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[{0}][{1}:{2}:{3}:{4}] {5}", Loglevel, File, LineNumber, Section, Setting, Message);
        }
    }
}
