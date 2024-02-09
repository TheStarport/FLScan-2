using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class LoadoutChecks
    {
        private static string[] loadoutFiles;
        private static string[] loadoutNicknames;

        public static void CheckLoadouts()
        {
            Logger.ILog("Checks Loadouts");
            Util.RunChecks(CheckLoadoutFile, loadoutFiles);
            Logger.ILog("Finished Loadouts");
        }

        private static void CheckLoadoutFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var loadouts = ini.GetSettings("Loadout");
            string currentArchetype = null;

            foreach (var loadout in loadouts)
            {
                string item;
                switch (loadout.settingName)
                {
                    case "nickname":
                        currentArchetype = null;
                        break;
                    case "archetype":
                        if(!ShipChecks.ShipExists(loadout.Str(0)) && !SolarChecks.SolarExists(loadout.Str(0)))
                            Logger.LogInvalidValue(file, loadout, "Archetype not found!");
                        currentArchetype = loadout.Str(0);
                        break;
                    case "equip": // TODO: check if there are more weapons than hardpoints
                        item = loadout.Str(0);
                        if(!EquipmentChecks.EquipExists(item))
                            Logger.LogInvalidValue(file, loadout, "Equip doesn't exist!");

                        if (loadout.NumValues() == 2 && currentArchetype != null)
                        {
                            string hardpoint = loadout.Str(1);
                            string category = EquipmentChecks.GetCategory(item);
                            if (!new [] { "CargoPod", "Light", "AttachedFX", "ShieldGenerator", "Shield", "Scanner", "Tractor", "CounterMeasureDropper", "Engine", "Thruster", "CloakingDevice", "Motor", "Gun", "MineDropper" }.Contains(category))
                                break;

                            if (Checker.DisableUTF)
                                break;
                            
                            Dictionary<string, UtfFile> utfList;
                            if (ShipChecks.ShipExists(currentArchetype))
                            {
                                utfList = ShipChecks.shipUTF;
                            }
                            else if (SolarChecks.SolarExists(currentArchetype))
                            {
                                utfList = SolarChecks.solarUTF;
                            }
                            else
                            {
                                Logger.LogInvalidValue(file, loadout, "Archetype not found!");
                                break;
                            }

                            if (!utfList[currentArchetype].HardpointExists(hardpoint))
                                Logger.LogInvalidValue(file, loadout, "Hardpoint not found!", hardpoint);
                        }
                        break;
                    case "cargo":
                        item = loadout.Str(0);
                        if(!EquipmentChecks.EquipExists(item))
                            Logger.LogInvalidValue(file, loadout, "Item doesn't exist!");
                        break;
                }
            }
        }

        public static void ParseLoadouts()
        {
            Logger.ILog("Parsing Loadouts");

            string[][] loadouts = Util.GetNicks("Data", "loadouts", "Loadout", "nickname");
            LoadoutChecks.loadoutFiles = loadouts[0];
            LoadoutChecks.loadoutNicknames = loadouts[1].Select(n => n.ToLower()).ToArray();

            Logger.ILog(string.Format("Finished Loadouts, found {0} loadouts", loadoutNicknames.Length));
        }

        public static bool LoadoutExists(string loadout)
        {
            loadout = loadout.ToLower();
            return loadoutNicknames.Contains(loadout);
        }
    }
}
