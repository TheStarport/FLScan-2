using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class MissionChecks
    {
        class FORMATION
        {
            public string nickname;
        }

        class SHIPCLASS
        {
            public string nickname;
            public List<string> members = new List<string>();
        }

        class NPCSHIPARCH
        {
            public string nickname;
            public string desc;
            public uint level;
            public Dictionary<uint, bool> levels = new Dictionary<uint, bool>();
            public List<string> shipclass_members = new List<string>();
        }

        class FACTIONPROPS
        {
            public string affiliation;
            public string desc;
            public List<string> npc_ships = new List<string>();
            public Dictionary<string, string> formations = new Dictionary<string, string>();
        }

        class SYSTEM_ENCOUNTER
        {
            public string name;
            public string desc;
            public uint level;
            public List<string> factions = new List<string>();
        }

        class ZONE
        {
            public string name;
        }

        class PATH_LABEL
        {
            public string path_label;
            public string desc;
            public List<uint> indices;
        }

        static Dictionary<string, FORMATION> formations = new Dictionary<string, FORMATION>();
        static Dictionary<string, SHIPCLASS> shipclasses = new Dictionary<string, SHIPCLASS>();
        static Dictionary<string, NPCSHIPARCH> npcships = new Dictionary<string, NPCSHIPARCH>();
        static Dictionary<string, FACTIONPROPS> factionprops = new Dictionary<string, FACTIONPROPS>();
        static Dictionary<string, ZONE> zones = new Dictionary<string, ZONE>();

        static void LoadFormations()
        {
            Logger.ILog("Loading formations.ini");
            string fileName = Checker.flDataPath + "\\MISSIONS\\formations.ini";
            FLDataFile file = new FLDataFile(fileName, true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (!sec.SettingExists("nickname"))
                    Logger.LogSettingNotFound(fileName, sec.settings[0].LineNumber, sec.sectionName, "nickname");

                FORMATION o = new FORMATION();
                o.nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant();
                formations[o.nickname] = o;
            }
        }

        static void LoadShipClasses()
        {
            Logger.ILog("Loading shipclasses.ini");
            string fileName = Checker.flDataPath + "\\MISSIONS\\shipclasses.ini";
            FLDataFile file = new FLDataFile(fileName, true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (!sec.SettingExists("nickname"))
                    Logger.LogSettingNotFound(fileName, sec.settings[0].LineNumber, sec.sectionName, "nickname");

                SHIPCLASS o = new SHIPCLASS();
                o.nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant();
                
                foreach (FLDataFile.Setting set in sec.settings)
                {
                    if (set.settingName == "member")
                    {
                        string member = set.Str(0).ToLowerInvariant();
                        o.members.Add(member);
                    }
                }
                shipclasses[o.nickname] = o;
            }
        }

        static bool ShipClassMemberExists(string member)
        {
            foreach (SHIPCLASS c in shipclasses.Values)
            {
                foreach (string m in c.members)
                {
                    if (m == member)
                        return true;
                }
            }
            return false;
        }
        
        static void CheckNPCShips(string npcShipPath, bool isMission)
        {
            Logger.ILog("Checking " + npcShipPath);

            FLDataFile file = new FLDataFile(npcShipPath, true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName != "NPCShipArch")
                    Logger.WLog("Illegal section at line " + sec.settings[0].LineNumber);

                if (!sec.SettingExists("nickname"))
                    Logger.LogSettingNotFound(npcShipPath, sec.settings[0].LineNumber, sec.sectionName, "nickname");

                NPCSHIPARCH o = new NPCSHIPARCH();
                o.nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant();

                foreach (FLDataFile.Setting set in sec.settings)
                {
                    if (set.settingName == "level")
                    {
                        uint level;
                        string tlevel = set.Str(0);
                        if (!uint.TryParse(tlevel.Substring(1), out level))
                            Logger.LogInvalidValue(npcShipPath, set, "Invalid level!");

                        o.level = level;
                    }
                    else if (set.settingName == "npc_class")
                    {
                        string legality = set.Str(0).ToLowerInvariant();
                        if (!ShipClassMemberExists(legality))
                            Logger.LogInvalidValue(npcShipPath, set, "Invalid legality member!");

                        string shipclass_member = set.Str(1).ToLowerInvariant();
                        if (!ShipClassMemberExists(shipclass_member))
                            Logger.LogInvalidValue(npcShipPath, set, "Invalid shipclass member!");

                        o.shipclass_members.Add(shipclass_member);
                        o.desc = set.desc;

                        for (int i = 2; i < set.NumValues(); i++)
                        {
                            uint level;
                            string tlevel = set.Str(i);
                            if (!uint.TryParse(tlevel.Substring(1), out level))
                                Logger.LogInvalidValue(npcShipPath, set, "Invalid level!", tlevel);

                            if (!ShipClassMemberExists(tlevel))
                                Logger.LogInvalidValue(npcShipPath, set, "Invalid shipclass member!", tlevel);

                            o.levels[level] = true;
                        }
                    }
                }
                if (!isMission)
                    npcships[o.nickname] = o;
            }
        }

        static void LoadNPCShips()
        {
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\npcships.ini", false);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M01a\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M01b\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M02\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M03\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M04\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M05\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M06\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M07\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M08\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M09\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M10\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M11\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M12\\npcships.ini", true);
            CheckNPCShips(Checker.flDataPath + "\\MISSIONS\\M13\\npcships.ini", true);
        }

        static void LoadFactionProps()
        {
            Logger.ILog("Checking faction_prop.ini");
            string fileName = Checker.flDataPath + "\\MISSIONS\\faction_prop.ini";
            FLDataFile file = new FLDataFile(fileName, true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName != "FactionProps")
                    Logger.WLog("Illegal section at line " + sec.settings[0].LineNumber);

                FACTIONPROPS o = new FACTIONPROPS();
                o.affiliation = sec.GetSetting("affiliation").Str(0).ToLowerInvariant();
                o.desc = sec.settings[0].desc;
                
                foreach (FLDataFile.Setting set in sec.settings)
                {
                    if (set.settingName == "formation" && Util.CheckNumberOfArgs(set, fileName, 2))
                    {
                        string formation_by_class = set.Str(0).ToLowerInvariant();
                        string formation = set.Str(1).ToLowerInvariant();

                        o.formations[formation_by_class] = formation;

                        if (!formations.ContainsKey(formation))
                            Logger.LogInvalidValue(fileName, set, "Formation not found!");
                    }
                    else if (set.settingName == "npc_ship")
                    {
                        string npc_ship = set.Str(0).ToLowerInvariant();

                        if (!npcships.ContainsKey(npc_ship))
                            Logger.LogInvalidValue(fileName, set, "npc_ship not found!");

                        o.npc_ships.Add(npc_ship);
                    }
                }

                factionprops[o.affiliation] = o;
            }
        }

        static void LoadSystem(string systemPath)
        {
            string fileName = Checker.flDataPath + "\\UNIVERSE\\" + systemPath;
            FLDataFile file = new FLDataFile(fileName, true);
            
            Dictionary<string, string> encounterFiles = new Dictionary<string, string>();
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName == "EncounterParameters")
                {
                    string nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant();
                    string filename = Checker.flDataPath + "\\" + sec.GetSetting("filename").Str(0).ToLowerInvariant();
                    if (!File.Exists(filename))
                        Logger.LogFileNotFound(fileName, sec.GetSetting("filename"));
                    else
                        encounterFiles[nickname] = filename;
                }
            }

            Dictionary<string, PATH_LABEL> path_labels = new Dictionary<string, PATH_LABEL>();

            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName.ToLowerInvariant() == "zone")
                {
                    // Load the encounter information for the zone.
                    string encounter = "";
                    Dictionary<string, SYSTEM_ENCOUNTER> encounters = new Dictionary<string, SYSTEM_ENCOUNTER>();
                    foreach (FLDataFile.Setting set in sec.settings)
                    {
                        if (set.settingName.ToLowerInvariant() == "path_label")
                        {
                            string path_label = set.Str(0).ToLowerInvariant();
                            if (!path_labels.ContainsKey(path_label))
                            {
                                path_labels[path_label] = new PATH_LABEL();
                                path_labels[path_label].desc = set.desc;
                                path_labels[path_label].indices = new List<uint>();
                            }

                            path_labels[path_label].path_label = path_label;

                            for (int i = 1; i < set.NumValues(); i++)
                            {
                                uint path_index = (uint)set.Int32(i);
                                if (path_labels[path_label].indices.Contains(path_index))
                                    Logger.LogDublicateValue(fileName, set, "Duplicate index!");
                                path_labels[path_label].indices.Add(path_index);
                            }
                        }
                        if (set.settingName.ToLowerInvariant() == "zone")
                        {
                            string zone = set.Str(0).ToLowerInvariant();
                            if (zones.ContainsKey(zone))
                                Logger.LogDublicateValue(fileName, set, "Duplicate zone!");
                            zones[zone] = new ZONE();
                            zones[zone].name = zone;
                        }
                        else if (set.settingName.ToLowerInvariant() == "encounter")
                        {
                            encounter = set.Str(0).ToLowerInvariant();
                            if (!encounterFiles.ContainsKey(encounter))
                                Logger.LogInvalidValue(fileName, set, "Encounter not found!");

                            if (encounters.ContainsKey(encounter))
                                Logger.LogDublicateValue(fileName, set, "Duplicate encounter - this may be intentional.", LogLevel.warning);

                            encounters[encounter] = new SYSTEM_ENCOUNTER();
                            encounters[encounter].name = encounter;
                            string str_level = set.Str(1);
                            if(!uint.TryParse(str_level, out encounters[encounter].level))
                                Logger.LogInvalidValue(fileName, set, "Has to be an integer!", str_level);
                            encounters[encounter].desc = set.desc;
                        }
                        else if (set.settingName.ToLowerInvariant() == "faction")
                        {
                            string faction = set.Str(0).ToLowerInvariant();
                            if (!factionprops.ContainsKey(faction))
                                Logger.LogInvalidValue(fileName, set, "Faction not found!");
                            if (!encounterFiles.ContainsKey(encounter))
                                Logger.LogInvalidValue(fileName, set, "Encounter not found!");

                            encounters[encounter].factions.Add(faction);
                        }
                    }


                    // Check the encounter information for the zone.
                    foreach (SYSTEM_ENCOUNTER e in encounters.Values)
                    {
                        if(!encounterFiles.ContainsKey(e.name))
                            continue;
                        string encounterFile = encounterFiles[e.name];
                        foreach (string faction in e.factions)
                        {
                            CheckEncounterForFaction(faction, encounterFile, e.desc);
                            CheckLevelForFaction(faction, encounterFile, e.level, e.desc);
                        }
                    }
                }
            }

            // Check path label indices are contiguous.
            foreach (PATH_LABEL pl in path_labels.Values)
            {
                if (pl.indices.Count == 0)
                {
                    Logger.ELog(String.Format("{0}: missing path_label indices, path_label={1}", pl.desc, pl.path_label));
                    continue;
                }
                
                pl.indices.Sort();
                uint low = pl.indices.Min();
                uint end = pl.indices.Max();
                for (int i = 0; i < pl.indices.Count; i++)
                {
                    if (pl.indices[i] != low++)
                        Logger.ELog(String.Format("{0}: missing non-contiguous path_label index, path_label={1}", pl.desc, pl.path_label));
                }
                if ((low - 1) != end)
                    Logger.ELog(String.Format("{0}: missing non-contiguous path_label index, path_label={1}", pl.desc, pl.path_label));
            }
        }

        static void CheckLevelForFaction(string encounterFaction, string encounterFile, uint level, string referencedFrom)
        {
            if (!factionprops.ContainsKey(encounterFaction))
                return; // TODO

            FACTIONPROPS fp = factionprops[encounterFaction];
            foreach (string npc_ship in fp.npc_ships)
            {
                if (!npcships.ContainsKey(npc_ship))
                {
                    Logger.ELog(String.Format("{0}: missing npc_ship, npc_ship={1} ref={2}", encounterFile, npc_ship, referencedFrom));
                    continue;
                }

                if (npcships[npc_ship].levels.ContainsKey(level))
                    return;
            }

            Logger.WLog(String.Format("{0}: missing level for npc_ship referenced from faction_prop, level={1} ref={2}", fp.desc, level, referencedFrom)); 
        }

        // Helper function to check:
        // [Encounter] reference [ShipClass] via the ship_class parameter
        // [FactionProps] reference [NPCShipArch] via the npc_ship parameter
        // [NPCShipArch] reference [ShipClass] via the npc_class parameter.
        // The npc_class must be a member of the [ShipClass]
        static bool CheckFactionNpcShipAndEncounterShipClassAreValid(SHIPCLASS sc, FACTIONPROPS fp)
        {
            foreach (string npc_ship in fp.npc_ships)
            {
                if (npcships.ContainsKey(npc_ship))
                {
                    foreach (string shipclass_member in npcships[npc_ship].shipclass_members)
                    {
                        if (sc.members.Contains(shipclass_member))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        static void CheckEncounterForFaction(string encounterFaction, string encounterFile, string referencedFrom)
        {
            if(!factionprops.ContainsKey(encounterFaction))
                return; // TODO
            FACTIONPROPS fp = factionprops[encounterFaction];

            FLDataFile file = new FLDataFile(encounterFile, true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName == "EncounterFormation")
                {
                    foreach (FLDataFile.Setting set in sec.settings)
                    {
                        // Check that ship_by_class parameter references a valid [ShipClass] 
                        // (not member of shipclass).
                        // Check [Encounter] reference [ShipClass] via the ship_class parameter
                        // Check [FactionProps] reference [NPCShipArch] via the npc_ship parameter
                        // Check [NPCShipArch] reference [ShipClass] via the npc_class parameter.
                        // Check The npc_class must be a member of the [ShipClass]           
                        if (set.settingName == "ship_by_class")
                        {
                            // Check that the ship class exists
                            string ship_class = set.Str(2).ToLowerInvariant();
                            if (!shipclasses.ContainsKey(ship_class))
                            {
                                Logger.ELog(String.Format("{0}: missing shipclass, ship_class={1} ref={2}", set.desc, ship_class, referencedFrom));
                                continue;
                            }

                            SHIPCLASS sc = shipclasses[ship_class];

                            if (!CheckFactionNpcShipAndEncounterShipClassAreValid(sc, fp))
                            {
                                Logger.ELog(String.Format("{0}: encounter npc_ship is not a member of ship class, ship_class={1} ref={2}", set.desc, ship_class, referencedFrom));
                                continue;
                            }
                            
                        }
                        // Check that make_class parameter references a valid [ShipClass] member
                        else if (set.settingName == "make_class")
                        {
                            string ship_class = set.Str(0).ToLowerInvariant();
                            if (!ShipClassMemberExists(ship_class))
                                Logger.ELog(String.Format("{0}: missing shipclass member, ship_class={1} ref={2}", set.desc, ship_class, referencedFrom));
                        }
                        // Check that the formation_by_class parameter references a formation class
                        // defined in the [FactionProps]
                        else if (set.settingName == "formation_by_class")
                        {
                            string formation_by_class = set.Str(0).ToLowerInvariant();

                            // this refers to the formation for the faction on the faction props
                            if (!fp.formations.ContainsKey(formation_by_class))
                                Logger.ELog(String.Format("{0}: missing formation_by_class, formation_by_class={1} ref={2}", set.desc, formation_by_class, referencedFrom));
                        }
                        else if (set.settingName == "formation")
                        {
                            string formation = set.Str(0).ToLowerInvariant();
                            if (!formations.ContainsKey(formation))
                                Logger.ELog(String.Format("{0}: missing formation, formation={1} ref={2}", set.desc, formation, referencedFrom));
                        }
                    }
                }
            }
        }

        static void LoadUniverse()
        {
            FLDataFile file = new FLDataFile(Checker.flDataPath + "\\UNIVERSE\\universe.ini", true);
            foreach (FLDataFile.Section sec in file.sections)
            {
                if (sec.sectionName == "system")
                {
                    LoadSystem(sec.GetSetting("file").Str(0));
                }
            }
        }

        public static void CheckForMissingFormations()
        {
#if !DEBUG
            try
            {
#endif
                formations.Clear();
                shipclasses.Clear();
                npcships.Clear();
                factionprops.Clear();
				zones.Clear();

                LoadFormations();
                LoadShipClasses();
                LoadNPCShips();
                LoadFactionProps();
                LoadUniverse();
#if !DEBUG
            }
            catch (Exception e)
            {
                Logger.FLog("Scan aborted: " + e.Message);
            }
#endif
        }
    }
}
