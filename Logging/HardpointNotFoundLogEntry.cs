using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    class HardpointNotFoundLogEntry : InvalidValueLogEntry
    {
        public HardpointNotFoundLogEntry(string file, FLDataFile.Setting setting, string foundValue) : base("Hardpoint not found!", file, setting)
        {
            this.FoundValue = foundValue;
        }

        public HardpointNotFoundLogEntry(string file, FLDataFile.Setting setting) : this(file, setting, setting.Str(0))
        {
        }
    }
}
