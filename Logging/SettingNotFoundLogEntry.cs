using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A class for logging non-existent settings.
    /// </summary>
    public class SettingNotFoundLogEntry : IniErrorLogEntry
    {
        private object identifier;

        public object Identifier
        {
            get { return identifier; }
            set { identifier = value; }
        }

        public SettingNotFoundLogEntry(string file, object identifier, string section, string setting) : base("Setting not found!", file, 0, section, setting)
        {
            Loglevel = LogLevel.warning;
            this.Identifier = identifier;
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[{0}][{1}:{2}:{3}] {4} {5}", Loglevel, File, Section, Identifier, Message, Setting);
        }
    }
}
