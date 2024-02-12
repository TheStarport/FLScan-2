using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class UniverseChecks
    {
        private static string[] universeFiles;
        private static Dictionary<string, string> baseFiles;
        private static string[] systemFiles;

        //                         sys     nicks
        private static Dictionary<string, string[]> baseNicks;
        private static string[] allBaseNicks;
        private static string[] systemNicks;

        //                         sys                type     nicks
        private static Dictionary<string, Dictionary<string, string[]>> objectNicks;

        private static string[] popTypes = Util.LoadConfig("ZonePopTypes");
        private static string[] hotspotNames = Util.LoadConfig("BaseHotspotNames");
        private static string[] hotspotBehaviors = Util.LoadConfig("BaseHotspotBehaviors");

        public static void CheckUniverseFolder()
        {
            // TODO: check for duplicate nicknames
            Logger.ILog("Checking Universe-Folder");
            Util.RunChecks(CheckUniverseFile, universeFiles);
            Util.RunChecks(CheckBaseFile, baseFiles.Select(b => b.Value));
            Logger.ILog("Finished Universe-Folder");
        }

        private static void CheckBaseFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            var rooms = ini.sections.Where(s => s.sectionName.ToLower() == "room").ToArray();
            FLDataFile.Section baseInfo = ini.sections.FirstOrDefault(s => s.sectionName.ToLower() == "baseinfo");
            string nick = baseInfo.GetSetting("nickname").Str(0);

            var roomNicks = rooms.Select(r => r.GetSetting("nickname").Str(0).ToLower()).ToArray();

            string startRoom = Util.TryGetStrSetting(baseInfo, "start_room");
            if (startRoom == null)
                Logger.LogSettingNotFound(file, nick ?? baseInfo.settings[0].LineNumber.ToString(), "BaseInfo", "start_room");
            else if (!roomNicks.Contains(startRoom.ToLower()))
                Logger.LogInvalidValue(file, baseInfo.GetSetting("start_room"), "Room doesn't exist!");

            foreach (var room in rooms)
            {
                string roomFile = Util.TryGetStrSetting(room, "file");
                if(roomFile == null)
                    Logger.LogSettingNotFound(file, room.settings[0].LineNumber, "Room", "file");
                else if (!File.Exists(Path.Combine(Checker.flDataPath, roomFile)))
                    Logger.LogFileNotFound(file, room.GetSetting("file"));
                else
                    CheckRoomFile(roomFile, ref roomNicks);
            }
        }

        private static void CheckRoomFile(string file, ref string[] roomNicks)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            /*
            [Room_Info] set_script  file exists
            [Room_Info] scene   1st-value: TODO, 2nd-value: file exists
            [Room_Sound] music  sound exists
            [Room_Sound] ambient  sound exists
            [CharacterPlacement] start_script  file exists
            [Camera] name  TODO: thn?
            [CharacterPlacement] name  TODO: thn
            */

            foreach (var hotspot in ini.GetSettings("Hotspot"))
            {
                string val = hotspot.Str(0).ToLower();

                switch (hotspot.settingName)
                {
                    case "name":
                        if (!hotspotNames.Contains(val))
                            Logger.LogInvalidValue(file, hotspot, "Invalid name!");
                        break;
                    case "room_switch":
                    case "set_virtual_room":
                       // if (!roomNicks.Contains(val)) // TODO: spams like hell - find out which values allowed when the room doesn't exist
                       //     Logger.LogInvalidValue(file, hotspot, "Room doesn't exist!");
                        break;
                    case "behavior":
                        if (!hotspotBehaviors.Contains(val))
                            Logger.LogInvalidValue(file, hotspot, "Invalid behavior!");
                        break;
                }
            }
        }

        private static void CheckUniverseFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            bool gotSystem = false;

            foreach (var section in ini.sections)
            {
                //string nick; unused
                switch (section.sectionName.ToLower())
                {
                    case "base":
                        if (gotSystem)
                        {
                            gotSystem = false;
                            Logger.WLog(file + ":" + (section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString()) + ": Base/System entries are in the wrong order!");
                        }
                        CheckBaseEntry(file, section);
                        break;
                    case "system":
                        gotSystem = true;
                        CheckSystemEntry(file, section);
                        break;
                    case "time":
                        break;
                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        private static void CheckBaseEntry(string file, FLDataFile.Section section)
        {
            string nick = Util.TryGetStrSetting(section, "nickname");
            Util.CheckIDName(section, "strid_name", nick, file);

            foreach (var setting in section.settings)
            {
                string val = setting.Str(0);

                switch (setting.settingName.ToLower())
                {
                    case "file":
                        if (!File.Exists(Path.Combine(Checker.flDataPath, val)))
                            Logger.LogFileNotFound(file, setting);
                        break;
                    case "system":
                        if(!SystemExists(val))
                            Logger.LogInvalidValue(file, setting, "System not found!");
                        break;
                    case "terrain_dyna_01":
                    case "terrain_dyna_02":
                    case "terrain_lrg":
                    case "terrain_mdm":
                    case "terrain_sml":
                    case "terrain_tiny":
                        if(!SolarChecks.AsteroidExists(val))
                            Logger.LogInvalidValue(file, setting, "Asteroid not found!");
                        break;

                    case "BGCS_base_run_by": // TODO
                        break;
                }
            }
        }

        private static void CheckSystemEntry(string file, FLDataFile.Section section)
        {
            string nick = Util.TryGetStrSetting(section, "nickname").ToLower();
            Util.CheckIDInfo(section, "ids_info", nick, file);
            Util.CheckIDName(section, "strid_name", nick, file);

            foreach (var setting in section.settings)
            {
                string val = setting.Str(0);

                switch (setting.settingName.ToLower())
                {
                    case "file":
                        if (!File.Exists(Path.Combine(Path.Combine(Checker.flDataPath, "universe"), val)))
                            Logger.LogFileNotFound(file, setting);
                        else
                            CheckSystemFile(nick, val);
                        break;
                    case "pos":
                        Util.CheckNumberOfArgs(setting, file, 2);
                        break;
                    case "msg_id_prefix":
                        //if(!AudioChecks.VoiceMsgExists(val)) // TODO
                        //    Logger.LogInvalidValue(file, setting, "Msg doesn't exist!");
                        break;
                }
            }
        }

        private static void CheckSystemFile(string sysnick, string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Path.Combine(Checker.flDataPath, "universe"), file), true);
            string val, nick;
            /*
[zone] attack_ids => ?
[zone] lane_id => ?, look attack_ids
[zone] density_restriction => Str(1) [ShipClass] 
             */
            foreach (var section in ini.sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "encounterparameters":
                        string enc_file = Util.TryGetStrSetting(section, "filename");

                        if (enc_file != null)
                            CheckEncounterFile(enc_file);
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "EncounterParameters", "file");
                        break;
                    case "nebula":
                        string neb_file = Util.TryGetStrSetting(section, "file");
                        string neb_zone = Util.TryGetStrSetting(section, "zone");

                        if (neb_file != null)
                            CheckNebulaFile(neb_file, sysnick);
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Nebula", "file");
                        if (neb_zone != null && !objectNicks[sysnick]["zone"].Contains(neb_zone.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("zone"), "Zone doesn't exist!");
                        break;
                    case "zone":
                        nick = Util.TryGetStrSetting(section, "nickname");
                        Util.CheckIDName(section, "ids_name", nick, file);
                        Util.CheckIDInfo(section, "ids_info", nick, file);

                        foreach (var setting in section.settings)
                        {
                            val = setting.Str(0);

                            switch (setting.settingName.ToLower())
                            {
                                case "vignette_type":
                                    val = val.ToLower();
                                    if (val != "exclusion" && val != "field" && val != "open")
                                        Logger.LogInvalidValue(file, setting, "Invalid type!");
                                    break;
                                case "usage":
                                    val = val.ToLower();
                                    if (val != "patrol" && val != "trade")
                                        Logger.LogInvalidValue(file, setting, "Invalid usage!");
                                    break;
                                case "spacedust":
                                    if (!FXChecks.EffectExists(val))
                                        Logger.LogInvalidValue(file, setting, "Effect doesn't exist!");
                                    break;
                                case "shape":
                                    val = val.ToLower();
                                    if (val != "box" && val != "cylinder" && val != "ellipsoid" && val != "sphere" && val != "ring")
                                        Logger.LogInvalidValue(file, setting, "Invalid shape!");
                                    break;
                                case "pos":
                                case "rotate":
                                case "property_fog_color":
                                    Util.CheckNumberOfArgs(setting, file, 3);
                                    break;
                                case "pop_types":
                                    val = val.ToLower();
                                    if (!popTypes.Contains(val))
                                        Logger.LogInvalidValue(file, setting, "Invalid pop_type!");
                                    break;
                                case "music":
                                    if(!AudioChecks.SoundExists(val))
                                        Logger.LogInvalidValue(file, setting, "Sound not found!");
                                    break;
                                case "faction":
                                case "edge_fraction": // TODO depends on a new MissionChecks.cs
                                    break;
                                case "mission_type":
                                    if (val != "lawful" && val != "unlawful")
                                        Logger.LogInvalidValue(file, setting, "Invalid mission_type!");
                                    break;
                                case "encounter":
                                    if (!objectNicks[sysnick]["encounterparameters"].Contains(val.ToLower()))
                                        Logger.LogInvalidValue(file, setting, "Encounter doesn't exist!");
                                    break;
                            }
                        }
                        break;
                    case "systeminfo": // Updated 01/2024
                        string name = Util.TryGetStrSetting(section, "name");
                        string local_fact = Util.TryGetStrSetting(section, "local_faction"); // TODO
                        string rpopd = Util.TryGetStrSetting(section, "rpop_solar_detection");
                        float farclip = float.Parse(Util.TryGetStrSetting(section, "space_farclip"));

                        //if (name != null && name.ToLower() != sysnick)
                        //Logger.LogInvalidValue(file, section.GetSetting("name"), "Should equal system-nickname!");
                        if (name != null)
                            Logger.LogInvalidValue(file, section.GetSetting("name"), "Invalid line in file.");
                        if (rpopd != null)
                            if (rpopd.ToLower() != "false" && rpopd.ToLower() != "true")
                                Logger.LogInvalidValue(file, section.GetSetting("rpopd"), "Must be true or false.");
                        if (farclip < 1000)
                            Logger.LogInvalidValue(file, section.GetSetting("farclip"), "Must be at least 1000.");
                        else if (farclip > 999)
                            break;
                        else
                            Logger.LogInvalidValue(file, section.GetSetting("farclip"), "Invalid data type, expected numeric value.");
                        break;
                    case "texturepanels":
                        val = Util.TryGetStrSetting(section, "file");
                        if (val != null && !File.Exists(Path.Combine(Checker.flDataPath, val)))
                            Logger.LogFileNotFound(file, section.GetSetting("file"));
                        break;
                    case "archetype": // Updated 01/2024
                        foreach (var setting in section.settings)
                        {
                            switch (setting.settingName)
                            {
                                case "solar":
                                    break;
                                case "ship":
                                    break;
                                case "simple":
                                    break;
                                case "equipment":
                                    break;
                                case "snd":
                                    break;
                                case "voice":
                                    break;
                            }
                        }
                        break;
                    case "asteroids":
                        string ast_file = Util.TryGetStrSetting(section, "file");
                        string ast_zone = Util.TryGetStrSetting(section, "zone");

                        if (ast_file != null)
                            CheckAsteroidFile(ast_file, sysnick);
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Asteroids", "file");

                        if (ast_zone != null)
                        {
                            if (!ThingExistsInSystem(sysnick, ast_zone, "zone"))
                                Logger.LogInvalidValue(file, section.GetSetting("zone"), "Zone doesn't exist!");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Asteroids", "zone");
                        break;
                    case "music":
                        string music_battle = Util.TryGetStrSetting(section, "battle");
                        string music_danger = Util.TryGetStrSetting(section, "danger");
                        string music_space = Util.TryGetStrSetting(section, "space");

                        if (music_battle != null)
                        {
                            if (!AudioChecks.SoundExists(music_battle))
                                Logger.LogInvalidValue(file, section.GetSetting("battle"), "Sound doesn't exist!");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Music", "music_battle");

                        if (music_danger != null)
                        {
                            if (!AudioChecks.SoundExists(music_battle))
                                Logger.LogInvalidValue(file, section.GetSetting("danger"), "Sound doesn't exist!");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Music", "music_danger");

                        if (music_space != null)
                        {
                            if (!AudioChecks.SoundExists(music_space))
                                Logger.LogInvalidValue(file, section.GetSetting("space"), "Sound doesn't exist!");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Music", "music_space");

                        break;
                    case "background":
                        string basic_stars = Util.TryGetStrSetting(section, "basic_stars");
                        string complex_stars = Util.TryGetStrSetting(section, "complex_stars");
                        string nebulae = Util.TryGetStrSetting(section, "nebulae");

                        if (basic_stars != null)
                        {
                            if (!File.Exists(Path.Combine(Checker.flDataPath, basic_stars)))
                                Logger.LogFileNotFound(file, section.GetSetting("basic_stars"));
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Background", "basic_stars");

                        if (complex_stars != null)
                        {
                            if (!File.Exists(Path.Combine(Checker.flDataPath, complex_stars)))
                                Logger.LogFileNotFound(file, section.GetSetting("complex_stars"));
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Background", "complex_stars");

                        if (nebulae != null)
                        {
                            if (!File.Exists(Path.Combine(Checker.flDataPath, nebulae)))
                                Logger.LogFileNotFound(file, section.GetSetting("nebulae"));
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Background", "nebulae");

                        break;
                    case "dust":
                        string dust_spacedust = Util.TryGetStrSetting(section, "spacedust");
                        if(dust_spacedust != null && !FXChecks.EffectExists(dust_spacedust))
                            Logger.LogInvalidValue(file, section.GetSetting("spacedust"), "Effect doesn't exist!");
                        break;
                    case "lightsource": // Updated 01/2024
                        /*string ls_pos = Util.TryGetStrSetting(section, "pos"); TODO
                        string ls_rot = Util.TryGetStrSetting(section, "rotate");
                        string ls_color = Util.TryGetStrSetting(section, "color");
                        string ls_range = Util.TryGetStrSetting(section, "range");
                        string ls_atten = Util.TryGetStrSetting(section, "attenuation");*/
                        string ls_attenCurve = Util.TryGetStrSetting(section, "atten_curve");
                        string ls_behavior = Util.TryGetStrSetting(section, "behavior");
                        //string ls_colcurve = Util.TryGetStrSetting(section, "color_curve");
                        string ls_type = Util.TryGetStrSetting(section, "type");

                       /* if (ls_pos != null)
                            string pos_x = ls_pos.Str(0);
                            string pos_y = ls_pos.Str(1);
                            string pos_z = ls_pos.Str(2);
                            float posx;
                            float posy;
                            float posz;
                            if (!Util.CheckNumberOfArgs(ls_pos, file, 3) && float.TryParse(posx, out pos_x))
                                Logger.LogInvalidValue(file, section.GetSetting("pos"), "Position must have three number values: 0, 0, 0");
                       */
                        if (ls_attenCurve != null)
                        {
                            if (ls_attenCurve.ToLower() != "dynamic_direction")
                                Logger.LogInvalidValue(file, section.GetSetting("atten_curve"), "Has to be DYNAMIC_DIRECTION.");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "LightSource", "atten_curve");

                        if (ls_behavior != null)
                            Logger.LogInvalidValue(file, section.GetSetting("behavior"), "Invalid line in file.");
                        /*{
                            if (ls_behavior.ToLower() != "nothing")
                                Logger.LogInvalidValue(file, section.GetSetting("behavior"), "Has to be NOTHING.");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "LightSource", "behavior");
                        */
                        if (ls_type != null)
                        {
                            if (ls_type.ToLower() != "point" && ls_type.ToLower() != "directional")
                                Logger.LogInvalidValue(file, section.GetSetting("type"), "Has to be DIRECTIONAL or POINT.");
                        }
                        else
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "LightSource", "type");
                        break;
                    case "object":
                        // TODO [Object] pilot => [Pilot]

                        nick = Util.TryGetStrSetting(section, "nickname");
                        Util.CheckIDName(section, "ids_name", nick, file);
                        Util.CheckIDInfo(section, "ids_info", nick, file);

                        string o_prevRing = null;
                        string o_nextRing = null;

                        foreach (var setting in section.settings)
                        {
                            if(setting.settingName == "260800" && file.ToLower().EndsWith("iw01.ini")) // fix for a error in the original file
                                continue;

                            if(setting.NumValues() == 0)
                                continue;

                            val = setting.Str(0);

                            string set = setting.settingName.ToLower();

                            if (set == "prev_ring")
                                o_prevRing = val;
                            else if (set == "next_ring")
                                o_nextRing = val;

                            switch (set)
                            {
                                case "voice":
                                    if (!AudioChecks.VoiceExists(val))
                                        Logger.LogInvalidValue(file, setting, "Voice not found!");
                                    break;
                                case "tradelane_space_name":
                                    uint ids;
                                    if (!uint.TryParse(val, out ids))
                                        Logger.LogInvalidValue(file, section.GetSetting("ids_info"), "Has to be a number!");
                                    else if (!Infocards.IsName(ids))
                                        Logger.LogIDError(file, section.GetSetting("ids_info"));
                                    break;
                                case "star":
                                    if (!SolarChecks.StarExists(val))
                                        Logger.LogInvalidValue(file, setting, "Star not found!");
                                    break;
                                case "space_costume":
                                    if (!Util.CheckNumberOfArgs(setting, file, 2, 3))
                                        break;

                                    string head = setting.Str(0);
                                    string body = setting.Str(1);
                                    string acces = null;
                                    if (setting.NumValues() == 3)
                                        acces = setting.Str(2);

                                    if (head.Length != 0 && !CharacterChecks.BodypartExists(head, "head") )
                                        Logger.LogInvalidValue(file, setting, "Head not found!", head);
                                    if (!CharacterChecks.BodypartExists(body, "body"))
                                        Logger.LogInvalidValue(file, setting, "Body not found!", body);
                                    if (acces != null && !CharacterChecks.BodypartExists(acces, "accessory"))
                                        Logger.LogInvalidValue(file, setting, "Accessory not found!", acces);
                                    break;
                                case "pos":
                                case "rotate":
                                case "spin":
                                    Util.CheckNumberOfArgs(setting, file, 3);
                                    break;
                                case "ring":
                                    if (!Util.CheckNumberOfArgs(setting, file, 2))
                                        break;
                                    string ring_zone = setting.Str(0);
                                    string ring_file = setting.Str(1);
                                    if (File.Exists(Path.Combine(Checker.flDataPath, ring_file)))
                                        CheckRingFile(ring_file, sysnick);
                                    else
                                        Logger.LogFileNotFound(file, setting, ring_file);
                                    if(!ZoneExists(sysnick, ring_zone))
                                        Logger.LogInvalidValue(file, setting, "Zone doesn't exist!", ring_zone);
                                    break;
                                case "reputation": // TODO depends on a new MissionChecks.cs
                                    break;
                                case "parent":
                                case "prev_ring":
                                case "next_ring":
                                    if(!ObjectExists(sysnick, val))
                                        Logger.LogInvalidValue(file, setting, "Object doesn't exist!");
                                    break;
                                case "msg_id_prefix":
                                    //if(!AudioChecks.VoiceMsgExists(val)) // TODO spams like hell because the messages are defined as "gcs_refer_system_hi02-", but "gcs_refer_system_hi02" is referenced 
                                    //    Logger.LogInvalidValue(file, setting, "Msg doesn't exist!");
                                    break;
                                case "loadout":
                                    if(!LoadoutChecks.LoadoutExists(val))
                                        Logger.LogInvalidValue(file, setting, "Loadout doesn't exist!");
                                    break;
                                case "jump_effect":
                                    if(!FXChecks.EffectExists(val))
                                        Logger.LogInvalidValue(file, setting, "Effect doesn't exist!");
                                    break;
                                case "goto":
                                    if(!Util.CheckNumberOfArgs(setting, file, 3))
                                        break;
                                    string goto_sys = setting.Str(0);
                                    string goto_obj = setting.Str(1);
                                    string goto_eff = setting.Str(2);
                                    if(!SystemExists(goto_sys))
                                        Logger.LogInvalidValue(file, setting, "System doesn't exist!", goto_sys);
                                    else if(!ObjectExists(goto_sys, goto_obj))
                                        Logger.LogInvalidValue(file, setting, "Object doesn't exist!", goto_obj);
                                    if(!FXChecks.EffectExists(goto_eff))
                                        Logger.LogInvalidValue(file, setting, "Effect doesn't exist!", goto_eff);
                                    break;
                                case "base":
                                case "dock_with":
                                    if(!BaseExists(val))
                                        Logger.LogInvalidValue(file, setting, "Base doesn't exist!");
                                    break;
                                case "behavior":
                                    if(val.ToLower() != "nothing")
                                        Logger.LogInvalidValue(file, setting, "Invalid behavior");
                                    break;
                                case "archetype":
                                    if(!SolarChecks.ArchetypeExists(val))
                                        Logger.LogInvalidValue(file, setting, "Archetype doesn't exist!");
                                    break;
                            }
                        }
                        
                        if(o_prevRing == o_nextRing && o_prevRing != null)
                            Logger.LogInvalidValue(file, section.GetSetting("prev_ring"), "prev_ring equals next_ring!");
                        break;
                    case "ambient": // TODO [Ambient] color 3 args
                        break;
                    case "field":
                        break;
                    case "asteroidbillboards":
                        break;
                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        private static void CheckNebulaFile(string file, string sys)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), false);
            string[] shapes = new string[0];
            if (ini.SettingExists("TexturePanels", "file"))
            {
                FLDataFile.Setting shapesSet = ini.GetSetting("TexturePanels", "file");
                shapes = Util.ParseShapeFile(shapesSet.Str(0), shapesSet, file);
            }

            CheckFieldFile(ini, shapes, file, sys);
        }

        private static void CheckEncounterFile(string file)
        {
            // TODO
        }

        private static void CheckAsteroidFile(string file, string sys)
        {
/* TODO
[TexturePanels]
file = solar\asteroids\rock_shapes.ini

[DynamicAsteroids]
asteroid = DAsteroid_mineable_small1

[Cube]
asteroid = mine_spike_minedout

[LootableZone]
asteroid_loot_container = lootcrate_ast_loot_gold
asteroid_loot_commodity = commodity_gold*/
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), false);
            string[] shapes = new string[0];
            if (ini.SettingExists("TexturePanels", "file"))
            {
                FLDataFile.Setting shapesSet = ini.GetSetting("TexturePanels", "file");
                shapes = Util.ParseShapeFile(shapesSet.Str(0), shapesSet, file);
            }

            CheckFieldFile(ini, shapes, file, sys);
        }

        private static void CheckRingFile(string file, string sys)
        {
            // TODO
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), false);
            string[] shapes = new string[0];
            if (ini.SettingExists("TexturePanels", "file"))
            {
                FLDataFile.Setting shapesSet = ini.GetSetting("TexturePanels", "file");
                shapes = Util.ParseShapeFile(shapesSet.Str(0), shapesSet, file);
            }

            CheckFieldFile(ini, shapes, file, sys);
        }

        private static void CheckFieldFile(FLDataFile ini, string[] shapes, string file, string sys)
        {
            foreach (var sec in ini.sections)
            {
                switch (sec.sectionName.ToLower())
                {
                    case "properties":
                        string pro_flag = Util.TryGetStrSetting(sec, "flag");
                        // TODO
                        /* rock_objects gas_danger_objects mine_danger_objects danger_density_med danger_density_low danger_density_high Object_density_low Object_density_med Object_density_high nebula*/
                        break;
                    case "exclusion zones":
                        Util.CheckFileExists(sec, file, "zone_shell", Checker.flDataPath);
                        Util.CheckFileExists(sec, file, "zone_shell", Checker.flDataPath);
                        string exclusion = Util.TryGetStrSetting(sec, "exclusion");
                        if (exclusion != null)
                        {
                            if (!ThingExistsInSystem(sys, exclusion, "zone"))
                                Logger.LogInvalidValue(file, sec.GetSetting("exclusion"), "Zone not found!");
                        }
                        break;
                    case "band":
                    case "asteroidbillboards":
                    case "exterior":
                        string ext_shape = Util.TryGetStrSetting(sec, "shape");
                        string ext_fillshape = Util.TryGetStrSetting(sec, "fill_shape");
                        string ext_detshape = Util.TryGetStrSetting(sec, "detail_shape");
                        if (ext_shape != null && !shapes.Contains(ext_shape.ToLower()))
                            Logger.LogInvalidValue(file, sec.GetSetting("shape"), "Shape not found!");
                        if (ext_fillshape != null && !shapes.Contains(ext_fillshape.ToLower()))
                            Logger.LogInvalidValue(file, sec.GetSetting("fill_shape"), "Shape not found!");
                        if (ext_detshape != null && !shapes.Contains(ext_detshape.ToLower()))
                            Logger.LogInvalidValue(file, sec.GetSetting("detail_shape"), "Shape not found!");
                        break;
                    case "clouds":
                        string cl_shape = Util.TryGetStrSetting(sec, "puff_shape");
                        if (cl_shape != null && !shapes.Contains(cl_shape.ToLower()))
                            Logger.LogInvalidValue(file, sec.GetSetting("puff_shape"), "Shape not found!");
                        break;
                }
            }
        }

        public static void ParseUniverseFolder()
        {
            Logger.ILog("Parsing Universe-Folder");
            universeFiles = Checker.flIni.GetSettings("Data", "universe").Select(s => s.Str(0)).ToArray();
            List<string> systemNicks = new List<string>();
            var baseNicks = new Dictionary<string, List<string>>();
            var objectNicks = new Dictionary<string, Dictionary<string, List<string>>>();
            baseFiles = new Dictionary<string, string>();

            foreach (var universeFile in universeFiles)
            {
                ParseUniverseFile(universeFile, ref systemNicks, ref baseNicks, ref objectNicks);
            }

            UniverseChecks.baseNicks = baseNicks.Select(s => new KeyValuePair<string, string[]>(s.Key, s.Value.ToArray())).ToDictionary(s => s.Key, s => s.Value);
            allBaseNicks = baseNicks.SelectMany(s => s.Value).ToArray();
            UniverseChecks.systemNicks = systemNicks.ToArray();

            UniverseChecks.objectNicks = objectNicks.ToDictionary(o => o.Key, o => o.Value.ToDictionary(s => s.Key, s => s.Value.ToArray()));
            
            Logger.ILog("Finished Universe-Folder");
        }

        private static void ParseUniverseFile(string file, ref List<string> systemNicks, ref Dictionary<string, List<string>> baseNicks, ref Dictionary<string, Dictionary<string, List<string>>> objectNicks)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                string nick;
                switch (section.sectionName.ToLower())
                {
                    case "base":
                        nick = Util.TryGetStrSetting(section, "nickname");
                        string sys = Util.TryGetStrSetting(section, "system");
                        string bfile = Util.TryGetStrSetting(section, "file");

                        if (nick == null || sys == null)
                        {
                            if (nick == null)
                                Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Base", "nickname");
                            if (sys == null)
                                Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Base", "system");
                            if (bfile == null)
                                Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Base", "file");
                            continue;
                        }
                        if(!baseNicks.ContainsKey(sys))
                            baseNicks[sys] = new List<string>();
                        baseNicks[sys].Add(nick.ToLower());

                        if (!File.Exists(Path.Combine(Checker.flDataPath, bfile)))
                        {
                            Logger.LogFileNotFound(file, section.GetSetting("file"));
                            break;
                        }
                        baseFiles.Add(nick, bfile);
                        break;
                    case "system":
                        nick = Util.TryGetStrSetting(section, "nickname");
                        string sfile = Util.TryGetStrSetting(section, "file");

                        if (sfile == null || nick == null)
                        {
                            if(sfile == null)
                                Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "System", "file");
                            if (nick == null)
                                Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "System", "nickname");
                            continue;
                        }

                        nick = nick.ToLower();
                        systemNicks.Add(nick);
                        if(!objectNicks.ContainsKey(nick))
                            objectNicks[nick] = new Dictionary<string, List<string>>();
                        ParseSystemFile(sfile, nick, ref objectNicks);
                        break;
                    case "time":
                        break;
                }
            }
        }

        private static void ParseSystemFile(string file, string system, ref Dictionary<string, Dictionary<string, List<string>>> objectNicks)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Path.Combine(Checker.flDataPath, "universe"), file), true);

            foreach (var section in ini.sections)
            {
                section.sectionName = section.sectionName.ToLower();
                switch (section.sectionName)
                {
                    case "encounterparameters":
                    case "lightsource":
                    case "object":
                    case "zone":
                        if (!objectNicks[system].ContainsKey(section.sectionName))
                            objectNicks[system][section.sectionName] = new List<string>();
                        objectNicks[system][section.sectionName].Add(section.GetSetting("nickname").Str(0).ToLower());
                        break;

                    case "field":
                    case "ambient":
                    case "asteroidbillboards":

                    case "systeminfo":
                    case "background":
                    case "archetype":
                    case "texturepanels":
                    case "dust":
                    case "music":
                    case "asteroids":
                    case "nebula":
                        break;
                }
            }
        }

        public static bool SystemExists(string nick)
        {
            nick = nick.ToLower();
            return systemNicks.Contains(nick);
        }

        public static bool ObjectExists(string sys, string nick)
        {
            return ThingExistsInSystem(sys, nick, "object");
        }

        public static bool ZoneExists(string sys, string nick)
        {
            return ThingExistsInSystem(sys, nick, "zone");
        }

        public static bool ThingExistsInSystem(string sys, string nick, string name)
        {
            nick = nick.ToLower();
            sys = sys.ToLower();
            return objectNicks[sys][name].Contains(nick);
        }

        public static bool BaseExists(string nick)
        {
            nick = nick.ToLower();
            return allBaseNicks.Contains(nick);
        }
    }
}
