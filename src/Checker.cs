using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FLScanIE.Logging;
using FLScanIE.Util_Functions;

namespace FLScanIE
{
    [Flags]
    public enum Checks
    {
        None = 0,
        Audio = 1,
        Character = 2,
        Equipment = 4,
        Fonts = 8,
        Fx = 16,
        Loadouts = 32,
        Missions = 64,
        PetalDb = 128,
        Ship = 256,
        Solar = 512,
        Universe = 1024
    }

    public static class Checker
    {
        public static Checks Checks;
        public static FLDataFile flIni;
        public static string flDataPath;
        private static bool disableUTF;

        public static FLDataFile confIni;

        public static bool DisableUTF
        {
            get { return disableUTF; }
            set { disableUTF = Properties.Settings.Default.setDisableUTF = value; }
        }

        static Checker()
        {
            confIni = new FLDataFile("conf.ini", false);
        }

        public static bool Parse(string flPath)
        {
            string flExePath = Path.Combine(flPath, "EXE");
            try
            {
                flIni = new FLDataFile(flExePath + Path.DirectorySeparatorChar + "Freelancer.ini", true);
            }
            catch (Exception ex)
            {
                Logger.ELog("Error '" + ex.Message + "' when parsing '" + flExePath);
                return false;
            }
            flDataPath = Path.GetFullPath(Path.Combine(flExePath, flIni.GetSetting("Freelancer", "data path").Str(0)));

            try
            {
                CharacterChecks.ParseCharacterFolder();
                PetalDbChecks.ParsePetalDb();
                AudioChecks.ParseAudioFolder();
                FXChecks.ParseFXFolder();
                ShipChecks.ParseShipFolder();
                LoadoutChecks.ParseLoadouts();
                SolarChecks.ParseSolarFolder();
                Infocards.Load();
                EquipmentChecks.ParseEquipmentFolder();
                UniverseChecks.ParseUniverseFolder();
                FontChecks.ParseFontFoler();
            }
            catch (Exception ex)
            {
                Logger.ELog("An error happend while parsing. The scan can't continue. '" + ex.Message + "'" + ex.StackTrace);
                return false;
            }
            return true;
        }

        public static void Check()
        {
            if (ShouldCheck(Checks.Solar))
                Util.RunCheck(CharacterChecks.CheckCharacterFolder, "characters");
            if (ShouldCheck(Checks.Universe))
                Util.RunCheck(UniverseChecks.CheckUniverseFolder, "universe");
            if (ShouldCheck(Checks.Audio))
                Util.RunCheck(AudioChecks.CheckAudioFolder, "audio");
            if (ShouldCheck(Checks.Fx))
                Util.RunCheck(FXChecks.CheckFX, "fx");
            if (ShouldCheck(Checks.Loadouts))
                Util.RunCheck(LoadoutChecks.CheckLoadouts, "loadouts");
            if (ShouldCheck(Checks.Solar))
                Util.RunCheck(SolarChecks.CheckSolarFolder, "solar");
            if (ShouldCheck(Checks.Ship))
                Util.RunCheck(ShipChecks.CheckShipFolder, "ships");
            if (ShouldCheck(Checks.Equipment))
                Util.RunCheck(EquipmentChecks.CheckEquipmentFolder, "equip");
            if (ShouldCheck(Checks.Missions))
                Util.RunCheck(MissionChecks.CheckForMissingFormations, "missions");
            if (ShouldCheck(Checks.Fonts))
                Util.RunCheck(FontChecks.CheckFontFolder, "fonts");
        }

        private static bool ShouldCheck(Checks check)
        {
            return (Checks & check) != Checks.None;
        }
    }
}
