using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FLScanIE.Logging;

namespace FLScanIE.Util_Functions
{
    public static class Util
    {
        public static string[][] GetNicks(string flini_section, string flini_name, string section, string name)
        {
            return GetNicks(new[] { flini_section }, new[] { flini_name }, new[] { section }, new[] { name });
        }

        public static string[][] GetNicks(string flini_section, string[] flini_names, string section, string name)
        {
            return GetNicks(new[] { flini_section }, flini_names, new[] { section }, new[] { name });
        }

        public static string[][] GetNicks(string flini_section, string flini_name, string[] sections, string name)
        {
            return GetNicks( new[] { flini_section }, new[] { flini_name }, sections, new[] { name });
        }

        public static string[][] GetNicks(string[] flini_sections, string[] flini_names, string[] sections, string[] names)
        {
            var inis = flini_sections.SelectMany(ss => Checker.flIni.GetSettings(ss).Where(s => flini_names.Contains(s.settingName)).Select(s => s.Str(0))).ToArray();
            var nicks = new List<string>();

            foreach (var ini in inis)
            {
                FLDataFile file = new FLDataFile(Checker.flDataPath + Path.DirectorySeparatorChar + ini, true);

                foreach (var section in sections)
                    nicks.AddRange(file.GetSettings(section).Where(s => names.Contains(s.settingName)).Select(s => s.Str(0)));
            }

            return new string[][] { inis, nicks.ToArray() };
        }

        public static bool CheckNumberOfArgs(FLDataFile.Section section, string file, string setting, int num)
        {
            if (section.SettingExists(setting))
                return CheckNumberOfArgs(section.GetSetting(setting), file, num);
            return false;
        }

        public static bool CheckNumberOfArgs(FLDataFile.Setting setting, string file, params int[] nums)
        {
            if (!nums.Contains(setting.NumValues()))
            {
                Logger.LogArgCount(file, setting, nums);
                return false;
            }
            return true;
        }

        public static bool CheckFileExists(FLDataFile.Section section, string file, string setting, string prefix)
        {
            if (section.SettingExists(setting))
                return CheckFileExists(section.GetSetting(setting), file, prefix);
            return false;
        }

        public static bool CheckFileExists(FLDataFile.Setting setting, string file, string prefix)
        {
            if (setting.NumValues() == 1 && !File.Exists(Path.Combine(prefix, setting.Str(0))))
            {
                Logger.LogFileNotFound(file, setting);
                return false;
            }
            return true;
        }

        public static bool CheckIDName(FLDataFile.Setting setting, object identifier, string file)
        {
            return CheckID(setting, identifier, file, false);
        }

        public static bool CheckIDInfo(FLDataFile.Setting setting, object identifier, string file)
        {
            return CheckID(setting, identifier, file, true);
        }

        public static bool CheckIDName(FLDataFile.Section section, string setting, object identifier, string file)
        {
            return CheckID(section, setting, identifier, file, false);
        }

        public static bool CheckIDInfo(FLDataFile.Section section, string setting, object identifier, string file)
        {
            return CheckID(section, setting, identifier, file, true);
        }

        public static bool CheckID(FLDataFile.Section section, string setting, object identifier, string file, bool infocard)
        {
            if (section.SettingExists(setting))
            {
                return CheckID(section.GetSetting(setting), identifier, file, infocard);
            }
            else
            {
                //Logger.LogSettingNotFound(file, identifier, section.sectionName, "ids_info");
                return false;
            }
            return true;
        }

        public static bool CheckID(FLDataFile.Setting setting, object identifier, string file, bool infocard)
        {
            string str_ids = setting.Str(0);

            if (str_ids != null)
            {
                uint ids;
                if (!uint.TryParse(str_ids, out ids))
                {
                    Logger.LogInvalidValue(file, setting, "Has to be a number!");
                    return false;
                }
                else
                {
                    if (!Infocards.IsInfocard(ids) && (infocard && !Infocards.IsName(ids)))
                    {
                        Logger.LogIDError(file, setting);
                        return false;
                    }
                }
            }
            else
            {
                //Logger.LogSettingNotFound(file, identifier, section.sectionName, "ids_info");
                return false;
            }
            return true;
        }

        public struct NickUtfTuple
        {
            public string Nick;
            public UtfFile UtfFile;
        }

        public static NickUtfTuple ParseNickUTF(string flDataPath, string file, FLDataFile.Section section)
        {
            return ParseNickUTF(flDataPath, file, section, true);
        }

        public static NickUtfTuple ParseNickUTF(string flDataPath, string file, FLDataFile.Section section, bool errorUtf)
        {
            return ParseNickUTF(flDataPath, file, section, "nickname", "DA_archetype", true);
        }

        public static NickUtfTuple ParseNickUTF(string flDataPath, string file, FLDataFile.Section section, string nick, string utf)
        {
            return ParseNickUTF(flDataPath, file, section, nick, utf, true);
        }

        public static NickUtfTuple ParseNickUTF(string flDataPath, string file, FLDataFile.Section section, string nick, string utf, bool errorUtf)
        {
            var retn = new NickUtfTuple();

            retn.Nick = section.GetSetting(nick).Str(0);
            if (section.SettingExists(utf))
            {
                string utfFile = Path.Combine(flDataPath, section.GetSetting(utf).Str(0));
                if (File.Exists(utfFile))
                {
                    if (!Checker.DisableUTF)
                    {
                        try
                        {
                            retn.UtfFile = new UtfFile(utfFile);
                        }
                        catch (Exception ex)
                        {
                            Logger.ELog("Could not parse utf file! " + ex.Message + ", referenced at: " + section.GetSetting(utf) + ", utf-file: " + utfFile);
                        }
                    }
                }
                else
                    Logger.LogFileNotFound(file, section.GetSetting(utf));
            }
            else if(errorUtf)
                Logger.LogSettingNotFound(file, retn.Nick, section.sectionName, utf);

            return retn;
        }

        public static FLDataFile.Setting TryGetSetting(FLDataFile.Section section, string name)
        {
            if (section.SettingExists(name))
                return section.GetSetting(name);
            return null;
        }

        public static string TryGetStrSetting(FLDataFile.Section section, string name)
        {
            var setting = TryGetSetting(section, name);
            return setting == null ? null : setting.Str(0);
        }

        public delegate void CheckDelegate();
        public static void RunCheck(CheckDelegate check, string name)
        {
#if DEBUG
            check.Invoke();
#else
            try { check.Invoke(); }
            catch (Exception ex) { Logger.FLog("An error happend while checking " + name + " '" + ex.Message + "'\n" + ex.StackTrace); }
#endif
        }

        public delegate void CheckFileDelegate(string file);
        public static void RunCheck(CheckFileDelegate check, string file)
        {
#if DEBUG
            check.Invoke(file);
#else
            try { check.Invoke(file); }
            catch (Exception ex) { Logger.FLog("An error happend while checking file: " + file + " '" + ex.Message + "'\n" + ex.StackTrace); }
#endif
        }

        public static void RunChecks(CheckFileDelegate check, IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                RunCheck(check, file);
            }
        }

        public static string[] LoadConfig(string section)
        {
            var sec = Checker.confIni.GetSettings(section);
            var entries = new List<string>(sec.Count * 8);

            foreach (var set in sec)
            {
                string s = set.settingName.ToLower();
                if (!s.Contains("$"))
                {
                    entries.Add(s);
                }
                else
                {
                    try
                    {
                        int pStart, pEnd, pSep;
                        pStart = s.IndexOf('$');
                        pEnd = s.LastIndexOf('$');
                        pSep = s.IndexOf('-', pStart);

                        int start = int.Parse(s.Substring(pStart + 1, pSep - pStart - 1));
                        int end = int.Parse(s.Substring(pSep + 1, pEnd - pSep - 1));

                        string first = s.Substring(0, pStart);
                        string second = s.Substring(pEnd + 1);

                        for (int i = start; i <= end; i++)
                        {
                            entries.Add(string.Format("{0}{1}{2}", first, i, second));
                        }
                    }
                    catch
                    {
                        Logger.ELog("Ignored invalid config-line: " + set.LineNumber);
                    }
                }
            }
            return entries.ToArray();
        }

        public static string[] ParseShapeFile(string file, FLDataFile.Setting refSet, string refFile)
        {
            if (!File.Exists(Path.Combine(Checker.flDataPath, file)))
            {
                Logger.LogFileNotFound(refFile, refSet);
                return new string[0];
            }
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), false);
            List<string> values = new List<string>();

            foreach (var set in ini.sections.SelectMany(s => s.settings))
            {
                string name = set.settingName.ToLower();

                if (name == "tex_shape" || name == "shape_name")
                {
                    string value = set.Str(0).ToLower();
                    values.Add(value);
                }
            }

            return values.ToArray();
        }
    }
}
