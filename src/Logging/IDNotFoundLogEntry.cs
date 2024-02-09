using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    public class IDNotFoundLogEntry : InvalidValueLogEntry
    {
        public IDNotFoundLogEntry(string file, FLDataFile.Setting setting) : base("ID not found!", file, setting)
        {
        }
    }
}
