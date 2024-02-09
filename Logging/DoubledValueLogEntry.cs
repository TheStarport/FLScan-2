using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    class DublicateValueLogEntry : InvalidValueLogEntry
    {
        public DublicateValueLogEntry(string message, string file, FLDataFile.Setting setting, LogLevel level) : base(message, file, setting)
        {
        }

        public DublicateValueLogEntry(string message, string file, FLDataFile.Setting setting, LogLevel level, string foundValue) : base(message, file, setting, foundValue)
        {
        }
    }
}
