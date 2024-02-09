using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class CharacterChecks
    {
        private static string[] bodypartFiles;
        private static string[] costumeFiles;

        private static Dictionary<string, string[]> bodyparts;
        private static Dictionary<string, Dictionary<string, UtfFile>> bodypartUTF;
        private static string[] allBodyparts;
        private static string[] costumeNicks;

        public static void CheckCharacterFolder()
        {
            Logger.ILog("Checking Character-Folder");
            Util.RunChecks(CheckBodypartFile, bodypartFiles);
            Util.RunChecks(CheckCostumeFile, costumeFiles);
            Logger.ILog("Finished Character-Folder");
        }

        private static void CheckBodypartFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "petalanimations":
                    case "animations":
                        Util.CheckFileExists(section, file, "anim", Checker.flDataPath);
                        break;
                    case "head":
                    case "body":
                    case "righthand":
                    case "lefthand":
                    case "accessory":
                        string nick = section.GetSetting("nickname").Str(0).ToLower();
                        foreach (var setting in section.settings)
                        {
                            switch (setting.settingName)
                            {
                                case "mesh":
                                    Util.CheckFileExists(setting, file, Checker.flDataPath);
                                    break;
                                case "hardpoint":
                                    if (!Checker.DisableUTF && bodypartUTF.ContainsKey(section.sectionName.ToLower()) && bodypartUTF[section.sectionName.ToLower()].ContainsKey(nick))
                                    {
                                        if (!bodypartUTF[section.sectionName.ToLower()][nick].HardpointExists(setting.Str(0)))
                                            Logger.LogHardpoint(file, setting);
                                    }
                                    break;
                                case "body_hardpoint": // TODO: hardpoint in body specified in [Costume]
                                    break;
                            }
                        }
                        break;
                    case "skeleton":
                        string sex = Util.TryGetStrSetting(section, "sex");
                        if (sex != null && sex != "male" && sex != "female" && sex != "none")
                            Logger.LogInvalidValue(file, section.GetSetting("sex"), "Has to be male, female or none!");
                        break;
                }
            }
        }

        private static void CheckCostumeFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections.Where(s => s.sectionName.ToLower() == "costume"))
            {
                foreach (var setting in section.settings)
                {
                    switch (setting.settingName.ToLower())
                    {
                        case "head":
                        case "body":
                        case "righthand":
                        case "lefthand":
                            if (!BodypartExists(setting.Str(0), setting.settingName))
                                Logger.LogInvalidValue(file, setting, setting.settingName + " doesn't exist!");
                            break;
                    }
                }
            }
        }

        public static void ParseCharacterFolder()
        {
            Logger.ILog("Parsing Character-Folder");
            var bodyparts = new Dictionary<string, List<string>>();
            var costumeNicks = new List<string>();

            var bodypartFiles = new List<string>();
            var costumeFiles = new List<string>();

            bodypartUTF = new Dictionary<string, Dictionary<string, UtfFile>>();

            foreach (var setting in Checker.flIni.GetSettings("Data"))
            {
                switch (setting.settingName)
                {
                    case "bodyparts":
                        ParseBodypartFile(setting.Str(0), ref bodyparts);
                        bodypartFiles.Add(setting.Str(0));
                        break;
                    case "costumes":
                        ParseCostumeFile(setting.Str(0), ref costumeNicks);
                        costumeFiles.Add(setting.Str(0));
                        break;
                }
            }

            CharacterChecks.bodyparts = bodyparts.ToDictionary(b => b.Key, b => b.Value.ToArray());
            CharacterChecks.allBodyparts = bodyparts.SelectMany(b => b.Value).ToArray();
            CharacterChecks.costumeNicks = costumeNicks.ToArray();

            CharacterChecks.bodypartFiles = bodypartFiles.ToArray();
            CharacterChecks.costumeFiles = costumeFiles.ToArray();

            Logger.ILog("Finished Character-Folder");
        }

        private static void ParseCostumeFile(string file, ref List<string> costumeNicks)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                if(section.sectionName.ToLower() != "costume")
                    continue;
                costumeNicks.Add(section.GetSetting("nickname").Str(0).ToLower());
            }
        }

        private static void ParseBodypartFile(string file, ref Dictionary<string, List<string>> bodyparts)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                string key = section.sectionName.ToLower();
                switch (key)
                {
                    case "head":
                    case "body":
                    case "righthand":
                    case "lefthand":
                    case "accessory":
                        var nickUtfTuple = Util.ParseNickUTF(Checker.flDataPath, file, section, "nickname", "mesh");
                        nickUtfTuple.Nick = nickUtfTuple.Nick.ToLower();

                        if(!bodyparts.ContainsKey(key))
                            bodyparts[key] = new List<string>();
                        bodyparts[key].Add(nickUtfTuple.Nick);
                        if (!bodypartUTF.ContainsKey(key))
                            bodypartUTF[key] = new Dictionary<string, UtfFile>();
                        if (nickUtfTuple.UtfFile != null)
                            bodypartUTF[key].Add(nickUtfTuple.Nick, nickUtfTuple.UtfFile);
                        break;

                    case "petalanimations":
                    case "animations":
                    case "detailswitchtable":
                    case "skeleton":
                        break;

                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        public static bool CostumeExists(string nick)
        {
            nick = nick.ToLower();
            return costumeNicks.Contains(nick);
        }

        public static bool BodypartExists(string nick)
        {
            nick = nick.ToLower();
            return allBodyparts.Contains(nick);
        }

        public static bool BodypartExists(string nick, string type)
        {
            nick = nick.ToLower();
            type = type.ToLower();
            if (!bodyparts.ContainsKey(type))
                return false;
            return bodyparts[type].Contains(nick);
        }
    }
}
