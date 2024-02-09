using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLScanIE.Logging
{
    /// <summary>
    /// A basic class used for status reporting
    /// Base class for all other LogEntry classes
    /// </summary>
    public class LogEntry
    {
        private string message;
        private LogLevel loglevel;

        public LogLevel Loglevel
        {
            get { return loglevel; }
            set { loglevel = value; }
        }

        public string Message
        {
            get { return message; }
            set { message = value; }
        }

        public LogEntry(string message, LogLevel loglevel)
        {
            this.Message = message;
            this.Loglevel = loglevel;
        }

        /// <summary>
        /// Returns the information saved in this LogEntry in a human-readable format.
        /// </summary>
        public override string ToString()
        {
            return string.Format("[{0}] {1}", loglevel, message);
        }
    }
}
