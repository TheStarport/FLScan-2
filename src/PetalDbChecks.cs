using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class PetalDbChecks
    {
        private static Dictionary<string, string[]> petalDbNicks;
        private static Dictionary<string, Dictionary<string, UtfFile>> petalUTF;

        private static string[] allPetalDbNicks;

        /* TODO: check THN-files:
         *       template_name in the THN has to be in petalDbNicks["room"] / petalDbNicks["prop"] if categroy is room / prop */
        public static void ParsePetalDb()
        {
            Logger.ILog("Parsing petaldb");
            var pentalDBfiles = Checker.flIni.GetSettings("Data", "PetalDB").Select(s => s.Str(0)).ToArray();
            var pentalDbNicks = new Dictionary<string, List<string>>();
            petalUTF = new Dictionary<string, Dictionary<string, UtfFile>>();

            foreach (var pentalDBfile in pentalDBfiles)
            {
                FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, pentalDBfile), true);

                var settings = ini.GetSettings("ObjectTable");
                foreach (var setting in settings)
                {
                    bool countOK = Util.CheckNumberOfArgs(setting, pentalDBfile, 2);

                    if (countOK)
                    {
                        string nick = setting.Str(0).ToLower();
                        string utfFile = Path.Combine(Checker.flDataPath, setting.Str(1));
                        string key = setting.settingName.ToLower();

                        if (key != "room" && key != "prop" && key != "cart")
                            Logger.WLog(pentalDBfile + ":" + setting.LineNumber + ": Expected room, prop or cart entry!");

                        if (!pentalDbNicks.ContainsKey(key))
                            pentalDbNicks[key] = new List<string>();
                        pentalDbNicks[key].Add(nick);

                        if (File.Exists(utfFile))
                        {
                            if (!Checker.DisableUTF)
                            {
                                if (!petalUTF.ContainsKey(key))
                                    petalUTF[key] = new Dictionary<string, UtfFile>();
                                UtfFile utf = null;
                                try
                                {
                                    utf = new UtfFile(utfFile);
                                }
                                catch (Exception ex)
                                {
                                    Logger.ELog("Could not parse utf file! " + ex.Message + ", referenced at: " + setting.desc + ", utf-file: " + utfFile);
                                }
                                if(utf != null)
                                    petalUTF[key].Add(nick, utf);
                            }
                        }
                        else
                            Logger.LogFileNotFound(pentalDBfile, setting);
                    }
                }
            }

            PetalDbChecks.petalDbNicks = pentalDbNicks.ToDictionary(n => n.Key, n => n.Value.ToArray());
            PetalDbChecks.allPetalDbNicks = petalDbNicks.SelectMany(n => n.Value).ToArray();
            Logger.ILog("Finished petaldb");
        }

        public static bool PentalExists(string nick)
        {
            nick = nick.ToLower();
            return allPetalDbNicks.Contains(nick);
        }

        public static bool PentalExists(string nick, string type)
        {
            nick = nick.ToLower();
            if (!petalDbNicks.ContainsKey(type))
                return false;
            return petalDbNicks[type].Contains(nick);
        }

        public static string GetCategory(string nick)
        {
            nick = nick.ToLower();
            foreach (var nicks in petalDbNicks)
            {
                if (nicks.Value.Contains(nick))
                    return nicks.Key;
            }
            return null;
        }
    }
}
