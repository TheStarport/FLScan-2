using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    class ShipChecks
    {
        private static string[] shipFiles;
        private static string[] shipNicknames;
        public static Dictionary<string, UtfFile> shipUTF; 

        private static string[] simpleNicknames;
        private static Dictionary<string, UtfFile> simpleUTF;

        private static string[] shipTypes = Util.LoadConfig("ShipTypes");
        private static string[] collisionGroupTypes = Util.LoadConfig("CollisionGroupTypes"); // TODO are these values from the UTF?

        public static void CheckShipFolder()
        {
            Logger.ILog("Checking Ship-Folder");
            Util.RunChecks(CheckShipFile, shipFiles);
            Logger.ILog("Finished Ship-Folder");
        }

        private static void CheckShipFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            var ships = ini.sections.Where(s => s.sectionName.ToLower() == "ship");
            var collisionGroups = ini.sections.Where(s => s.sectionName.ToLower() == "CollisionGroup");
            var simples = ini.sections.Where(s => s.sectionName.ToLower() == "simple");

            foreach (var ship in ships)
            {
                CheckShip(file, ship);
            }
            foreach (var collGrp in collisionGroups)
            {
                CheckCollGroup(file, collGrp);
            }
            foreach (var simple in simples)
            {
                CheckSimpe(file, simple);
            }
        }

        private static void CheckShip(string file, FLDataFile.Section ship)
        {
            string nick = ship.GetSetting("nickname").Str(0);

            if (!shipTypes.Contains(ship.GetSetting("type").Str(0).ToLower()))
                Logger.LogInvalidValue(file, ship.GetSetting("nickname"), "Ship type doesn't exist!");

            Util.CheckIDName(ship, "ids_name", nick, file);
            Util.CheckIDName(ship, "ids_info", nick, file);

            for (int i = 1; i <= 3; i++)
                Util.CheckIDName(ship, "ids_info" + i, nick, file);

            var fuses = ship.GetSettings("fuse");
            foreach (var fuse in fuses)
            {
                Util.CheckNumberOfArgs(fuse, file, 3);
                if (!FXChecks.FuseExists(fuse.Str(0)))
                    Logger.LogInvalidValue(file, fuse, "Fuse doesn't exist!");
            }

            var materials = ship.GetSettings("material_library");
            foreach (var material in materials)
            {
                if (!File.Exists(Path.Combine(Checker.flDataPath, material.Str(0))))
                    Logger.LogFileNotFound(file, material);
            }

            if (ship.SettingExists("explosion_arch"))
            {
                if (!FXChecks.ExplosionExists(ship.GetSetting("explosion_arch").Str(0)))
                    Logger.LogInvalidValue(file, ship.GetSetting("explosion_arch"), "Explosion doesn't exist!");
            }
            else
                Logger.LogSettingNotFound(file, nick, "Ship", "explosion_arch");

            var surfaceEffects = ship.GetSettings("surface_hit_effects");
            foreach (var surfaceEffect in surfaceEffects)
            {
                Util.CheckNumberOfArgs(surfaceEffect, file, 4);
                for (int i = 1; i <= 3; i++)
                    if (!FXChecks.EffectExists(surfaceEffect.Str(i)))
                        Logger.LogInvalidValue(file, surfaceEffect, "Effect doesn't exist!");
            }

            if (ship.SettingExists("bay_doors_open_snd") && !AudioChecks.SoundExists(ship.GetSetting("bay_doors_open_snd").Str(0)))
                Logger.LogInvalidValue(file, ship.GetSetting("bay_doors_open_snd"), "Sound doesn't exist!");
            if (ship.SettingExists("bay_doors_close_snd") && !AudioChecks.SoundExists(ship.GetSetting("bay_doors_close_snd").Str(0)))
                Logger.LogInvalidValue(file, ship.GetSetting("bay_doors_close_snd"), "Sound doesn't exist!");
            if (ship.SettingExists("msg_id_prefix") && !AudioChecks.VoiceMsgExists(ship.GetSetting("msg_id_prefix").Str(0)))
                Logger.LogInvalidValue(file, ship.GetSetting("msg_id_prefix"), "Msg doesn't exist!");

            Util.CheckNumberOfArgs(ship, file, "camera_offset", 2);
            Util.CheckNumberOfArgs(ship, file, "steering_torque", 3);
            Util.CheckNumberOfArgs(ship, file, "angular_drag", 3);
            Util.CheckNumberOfArgs(ship, file, "rotation_inertia", 3);

            if (ship.SettingExists("HP_tractor_source") && ShipChecks.shipUTF.ContainsKey(nick) && !ShipChecks.HardpointExists(nick, ship.GetSetting("HP_tractor_source").Str(0)))
                Logger.LogHardpoint(file, ship.GetSetting("HP_tractor_source"));
            if (ship.SettingExists("HP_bay_surface") && ShipChecks.shipUTF.ContainsKey(nick) && !ShipChecks.HardpointExists(nick, ship.GetSetting("HP_bay_surface").Str(0)))
                Logger.LogHardpoint(file, ship.GetSetting("HP_bay_surface"));
            if (ship.SettingExists("HP_bay_external") && ShipChecks.shipUTF.ContainsKey(nick) && !ShipChecks.HardpointExists(nick, ship.GetSetting("HP_bay_external").Str(0)))
                Logger.LogHardpoint(file, ship.GetSetting("HP_bay_external"));

            if (!Checker.DisableUTF && ShipChecks.shipUTF.ContainsKey(nick))
            {
                if (ship.SettingExists("num_exhaust_nozzles"))
                {
                    int engines_ini = (int) ship.GetSetting("num_exhaust_nozzles").Int32(0);
                    int engines_cmp = shipUTF[nick].hardpoints.Count(h => h.ToLower().StartsWith("hpengine"));

                    if (engines_ini > engines_cmp)
                        Logger.LogInvalidValue(file, ship.GetSetting("num_exhaust_nozzles"), 
                                               "Isn't smaller or equal than number of HpEngines in cmp (" + engines_cmp + ")!");
                }
                else
                    Logger.LogSettingNotFound(file, nick, "Ship", "explosion_arch");
            }

            var hp_typeEntries = ship.GetSettings("hp_type");
            foreach (var hpTypeEntry in hp_typeEntries)
            {
                if (!EquipmentChecks.HpTypeExists(hpTypeEntry.Str(0).ToLower())) // TODO: check category
                    Logger.LogInvalidValue(file, hpTypeEntry, "Invalid hardpoint-type!", hpTypeEntry.Str(0));
                for (int i = 1; i < hpTypeEntry.NumValues(); i++)
                {
                    if (hpTypeEntry.Str(i).Trim().Length != 0 && !Checker.DisableUTF && !shipUTF[nick].HardpointExists(hpTypeEntry.Str(i)))
                        Logger.LogHardpoint(file, hpTypeEntry, hpTypeEntry.Str(i));
                }
            }

            var shieldLinks = ship.GetSettings("shield_link");
            foreach (var shieldLink in shieldLinks)
            {
                if (!EquipmentChecks.EquipExists(shieldLink.Str(0)))
                    Logger.LogInvalidValue(file, shieldLink, "Equip doesn't exist!");
                else if (EquipmentChecks.GetCategory(shieldLink.Str(0)) != "shield")
                    Logger.LogInvalidValue(file, shieldLink, "Equip isn't a shield!");

                for (int i = 1; i < shieldLink.NumValues(); i++)
                {
                    if (shieldLink.Str(i).Trim().Length != 0 && !Checker.DisableUTF && shipUTF.ContainsKey(nick) && !shipUTF[nick].HardpointExists(shieldLink.Str(i)))
                        Logger.LogHardpoint(file, shieldLink, shieldLink.Str(i));
                }
            }

            if (ship.SettingExists("cockpit"))
            {
                string cockpit = ship.GetSetting("cockpit").Str(0);
                if (File.Exists(Path.Combine(Checker.flDataPath, cockpit)))
                {
                    CheckCockpitFile(cockpit);
                }
                else
                    Logger.LogFileNotFound(file, ship.GetSetting("cockpit"));
            }
            else
                Logger.LogSettingNotFound(file, nick, "Ship", "cockpit");

            if (ship.SettingExists("pilot_mesh"))
            {
                if (!simpleNicknames.Contains(ship.GetSetting("pilot_mesh").Str(0)))
                    Logger.LogInvalidValue(file, ship.GetSetting("pilot_mesh"), "Simple doesn't exist!");
            }
            else
                Logger.LogSettingNotFound(file, nick, "Ship", "pilot_mesh");
        }

        private static void CheckCollGroup(string file, FLDataFile.Section collGrp)
        {
            int firstLine = collGrp.settings[0].LineNumber;

            if (collGrp.SettingExists("explosion_arch"))
            {
                if (!FXChecks.ExplosionExists(collGrp.GetSetting("explosion_arch").Str(0)))
                    Logger.LogInvalidValue(file, collGrp.GetSetting("explosion_arch"), "Explosion doesn't exist!");
            }
            else
                Logger.LogSettingNotFound(file, firstLine, "CollisionGroup", "explosion_arch");

            var fuses = collGrp.GetSettings("fuse");
            foreach (var fuse in fuses)
            {
                Util.CheckNumberOfArgs(fuse, file, 3);
                if (!FXChecks.FuseExists(fuse.Str(0)))
                    Logger.LogInvalidValue(file, fuse, "Fuse doesn't exist!");
            }

            if (collGrp.SettingExists("type"))
            {
                if (!collisionGroupTypes.Contains(collGrp.GetSetting("type").Str(0).ToLower()))
                    Logger.LogInvalidValue(file, collGrp.GetSetting("type"), "Invalid type!");
            }
            else
                Logger.LogSettingNotFound(file, firstLine, "CollisionGroup", "type");

            if (collGrp.SettingExists("debris_type"))
            {
                if (!collisionGroupTypes.Contains(collGrp.GetSetting("debris_type").Str(0).ToLower()))
                    Logger.LogInvalidValue(file, collGrp.GetSetting("debris_type"), "Debris type doesn't exist!");
            }
            else
                Logger.LogSettingNotFound(file, firstLine, "CollisionGroup", "debris_type");

            if (collGrp.SettingExists("dmg_obj"))
            {
                string dmgObj = collGrp.GetSetting("dmg_obj").Str(0);
                string dmgHp = collGrp.GetSetting("dmg_hp").Str(0);

                if (!simpleNicknames.Contains(dmgObj))
                    Logger.LogInvalidValue(file, collGrp.GetSetting("dmg_obj"), "Simple doesn't exist!");

                if (!Checker.DisableUTF)
                {
                    if (!simpleUTF[dmgObj].HardpointExists(dmgHp))
                        Logger.LogHardpoint(file, collGrp.GetSetting("dmg_hp"));
                }
            }
            else
            {
                Logger.LogSettingNotFound(file, firstLine, "CollisionGroup", "dmg_obj");
                if (collGrp.SettingExists("dmg_hp"))
                    Logger.LogInvalidValue(file, collGrp.GetSetting("dmg_hp"), "dmg_hp exists but dmg_obj doesn't", "");
            }
        }

        private static void CheckSimpe(string file, FLDataFile.Section simple)
        {
            if (simple.SettingExists("material_library"))
            {
                if (!File.Exists(Path.Combine(Checker.flDataPath, simple.GetSetting("material_library").Str(0))))
                    Logger.LogFileNotFound(file, simple.GetSetting("material_library"));
            }
            else
                Logger.LogSettingNotFound(file, simple.settings[0].LineNumber, "Simple", "material_library");
        }

        private static void CheckCockpitFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            FLDataFile.Section CockpitSection = ini.sections.First(s => s.sectionName == "Cockpit");
            FLDataFile.Section TurretCameraSection = ini.sections.FirstOrDefault(s => s.sectionName == "TurretCamera");

            if(CockpitSection.SettingExists("mesh"))
            {
                var mesh = CockpitSection.GetSetting("mesh");
                if (!File.Exists(Path.Combine(Checker.flDataPath, mesh.Str(0))))
                    Logger.LogFileNotFound(file, mesh);
            }
            else
                Logger.LogSettingNotFound(file, "", "Cockpit", "mesh");

            Util.CheckNumberOfArgs(CockpitSection, file, "head_turn", 2);
            if (TurretCameraSection != null)
                Util.CheckNumberOfArgs(TurretCameraSection, file, "tether", 3);
        }

        public static void ParseShipFolder()
        {
            Logger.ILog("Parsing Ship-Folder");
            ShipChecks.shipFiles = Checker.flIni.GetSettings("Data", "ships").Select(s => s.Str(0)).ToArray();

            List<string> shipNicknames = new List<string>();
            shipUTF = new Dictionary<string, UtfFile>();

            List<string> simpleNicknames = new List<string>();
            simpleUTF = new Dictionary<string, UtfFile>();

            foreach (var shipFile in ShipChecks.shipFiles)
            {
                FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, shipFile), true);

                foreach (var section in ini.sections)
                {
                    Util.NickUtfTuple retn;
                    switch (section.sectionName)
                    {
                        case "Ship":
                            retn = Util.ParseNickUTF(Checker.flDataPath, shipFile, section);
                            shipNicknames.Add(retn.Nick);
                            if (retn.UtfFile != null)
                                shipUTF.Add(retn.Nick, retn.UtfFile);
                            break;
                        case "Simple":
                            retn = Util.ParseNickUTF(Checker.flDataPath, shipFile, section);
                            simpleNicknames.Add(retn.Nick);
                            if (retn.UtfFile != null)
                                simpleUTF.Add(retn.Nick, retn.UtfFile);
                            break;
                    }
                }
            }

            ShipChecks.shipNicknames = shipNicknames.Select(n => n.ToLower()).ToArray();
            ShipChecks.simpleNicknames = simpleNicknames.Select(n => n.ToLower()).ToArray();

            Logger.ILog(string.Format("Finished Ship-Folder, found {0} ships and {1} simples", ShipChecks.shipNicknames.Length, ShipChecks.simpleNicknames.Length));
        }

        public static bool ShipExists(string nickname)
        {
            nickname = nickname.ToLower();
            return shipNicknames.Contains(nickname);
        }

        private static bool HardpointExists(string ship, string hp)
        {
            if (Checker.DisableUTF)
                return true;
            return shipUTF[ship].HardpointExists(hp);
        }
    }
}
