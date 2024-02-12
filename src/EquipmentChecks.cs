using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class EquipmentChecks
    {
        /*
TODO: commodities_per_faction.ini, depends on a new MissionChecks.cs
TODO: check if [Muniton] hp_trail_parent exists in [Gun] DA_archetype
TODO: check if [Muniton] weapon_type equals [Gun] weapon_type
TODO: there are references to [LightAnim]-nicknames, but lightanim.ini isn't referenced in freelancer.ini
         */

        private static string[] equipmentFiles;
        private static Dictionary<string, string[]> equipNicks;
        private static Dictionary<string, Dictionary<string, UtfFile>> equipUtf;
        private static Dictionary<string, string> shipHulls;
        private static string[] AllEquipNicks;

        private static string[] goodFiles;
        private static string[] goodNicknames;

        private static string[] marketFiles;
        public static Dictionary<string, string[]> marketEntries;

        private static string[] weaponModFiles;
        private static string[] weaponTypes;

        private static string[] hpTypes = Util.LoadConfig("HardpointTypes");
        private static string[] shieldTypes = Util.LoadConfig("ShieldTypes");

        private static string[] GoodCategories = Util.LoadConfig("GoodCategories");

        public static void CheckEquipmentFolder()
        {
            Logger.ILog("Checking Equipment-Folder");
            var shipHulls = new Dictionary<string, string>();
            Util.RunChecks(CheckEquipmentFile, equipmentFiles);
            Util.RunChecks(CheckGoodFile, goodFiles);
            Util.RunChecks(CheckMarketFile, marketFiles);
            Util.RunChecks(CheckWeaponModFile, weaponModFiles);
            Logger.ILog("Finished Equipment-Folder");
        }

        private static void CheckWeaponModFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var setting in ini.sections.Where(s => s.sectionName.ToLower() == "weapontype").SelectMany(s => s.GetSettings("shield_mod")))
            {
                if (Util.CheckNumberOfArgs(setting, file, 2))
                {
                    string shieldType = setting.Str(0).ToLower();
                    if(!shieldType.Contains(shieldType))
                        Logger.LogInvalidValue(file, setting, "Shield-type not found!", shieldType);
                }
            }
        }

        private static string[] hasIDS = new string[] { "Commodity", "ShieldGenerator", "Engine", "CounterMeasure", "CounterMeasureDropper", "RepairKit", "Scanner", "ShieldBattery", "Tractor", "CloakingDevice", "Gun", "Mine", "MineDropper", "Munition"}.Select(s => s.ToLower()).ToArray();
        private static string[] hasArchetype = new string[] { "LootCrate", "Shield", "CargoPod", "ShieldGenerator", "Power", "CounterMeasure", "CloakingDevice", "Gun", "Mine", "MineDropper", "Munition", "CounterMeasureDropper" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasMaterial = new string[] { "LootCrate", "CargoPod", "ShieldGenerator", "Power", "CounterMeasureDropper", "CloakingDevice", "Gun", "Mine", "MineDropper" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasLootAppearance = new string[] { "Commodity", "CounterMeasure", "RepairKit", "ShieldBattery", "Mine Munition" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasSeparationExplosion = new string[] { "CounterMeasureDropper", "Gun", "MineDropper", "ShieldGenerator", "Thruster", "CollisionGroup" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasExplosionArch = new string[] { "LootCrate", "CargoPod", "Mine", "Munition", "Ship", "Asteroid", "AsteroidMine", "DynamicAsteroid", "Solar" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasDebris = new string[] { "CargoPod", "ShieldGenerator", "CounterMeasureDropper", "Gun", "MineDropper" }.Select(s => s.ToLower()).ToArray();
        private static string[] hasHpChild = new string[] { "CargoPod", "Shield", "ShieldGenerator", "CounterMeasureDropper", "CloakingDevice", "Gun", "MineDropper" }.Select(s => s.ToLower()).ToArray();

        private static void CheckEquipmentFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                string nick = Util.TryGetStrSetting(section, "nickname");
                if (nick != null)
                    nick = nick.ToLower();
                string cat = section.sectionName.ToLower();

                if(hasIDS.Contains(cat))
                {
                    Util.CheckIDInfo(section, "ids_info", nick, file);
                    Util.CheckIDName(section, "ids_name", nick, file);
                }
                if (hasArchetype.Contains(cat))
                {
                    string arch = Util.TryGetStrSetting(section, "da_archetype");
                    if (arch != null && !File.Exists(Path.Combine(Checker.flDataPath, arch)))
                        Logger.LogFileNotFound(file, section.GetSetting("da_archetype"));
                }
                if (hasMaterial.Contains(cat))
                {
                    string mat = Util.TryGetStrSetting(section, "material_library");
                    if (mat != null && !File.Exists(Path.Combine(Checker.flDataPath, mat)))
                        Logger.LogFileNotFound(file, section.GetSetting("material_library"));
                }
                if (hasLootAppearance.Contains(cat))
                {
                    string app = Util.TryGetStrSetting(section, "loot_appearance");
                    if (app != null && !equipNicks["lootcrate"].Contains(app))
                        Logger.LogInvalidValue(file, section.GetSetting("loot_appearance"), "LootCrate doesn't exist!");
                }
                if (hasSeparationExplosion.Contains(cat))
                {
                    string expl = Util.TryGetStrSetting(section, "separation_explosion");
                    //if (expl != null && AudioChecks.SoundExists(expl)) // TODO: is this really a sound?
                    //    Logger.LogInvalidValue(file, section.GetSetting("separation_explosion"), "Sound doesn't exist!");
                }
                if (hasExplosionArch.Contains(cat))
                {
                    string expl = Util.TryGetStrSetting(section, "explosion_arch");
                    if (expl != null && !FXChecks.ExplosionExists(expl))
                        Logger.LogInvalidValue(file, section.GetSetting("explosion_arch"), "Explosion doesn't exist!");
                }
                if (hasDebris.Contains(cat))
                {
                    string debris = Util.TryGetStrSetting(section, "debris_type");
                    if (debris != null && !FXChecks.DebrisExists(debris))
                        Logger.LogInvalidValue(file, section.GetSetting("debris_type"), "Debris doesn't exist!");
                }
                if (hasHpChild.Contains(cat))
                {
                    string hp_child = Util.TryGetStrSetting(section, "hp_child");
                    if (hp_child != null && !Checker.DisableUTF && equipUtf[cat].ContainsKey(nick) && !equipUtf[cat][nick].HardpointExists(hp_child))
                        Logger.LogHardpoint(file, section.GetSetting("hp_child"));
                }

                switch (section.sectionName.ToLower())
                {
                    case "cargopod": // see general checks
                        break;
                    case "shield": // see general checks
                        break;
                    case "power": // see general checks
                        break;
                    case "repairkit": // see general checks
                        break;
                    case "shieldbattery": // see general checks
                        break;
                    case "lootcrate": // see general checks
                        break;
                    case "scanner": // see general checks
                        break;

                    case "countermeasure":
                        string cm_eff = Util.TryGetStrSetting(section, "const_effect");
                        string cm_snd = Util.TryGetStrSetting(section, "one_shot_sound");

                        if (cm_eff != null && !FXChecks.EffectExists(cm_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("const_effect"), "Effect doesn't exist!");
                        if (cm_snd != null && !AudioChecks.SoundExists(cm_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("one_shot_sound"), "Sound doesn't exist!");
                        break;
                    case "attachedfx":
                        string afx_particles = Util.TryGetStrSetting(section, "particles");
                        if (afx_particles != null && !FXChecks.EffectExists(afx_particles))
                            Logger.LogInvalidValue(file, section.GetSetting("particles"), "Effect doesn't exist!");
                        break;
                    case "commodity":
                        string comm_podApp = Util.TryGetStrSetting(section, "pod_appearance");
                        if (comm_podApp != null && !equipNicks["cargopod"].Contains(comm_podApp.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("pod_appearance"), "CargoPod doesn't exist!");
                        break;
                    case "shieldgenerator":
                        string sh_collapse_snd = Util.TryGetStrSetting(section, "shield_collapse_sound");
                        string sh_rebuilt_snd = Util.TryGetStrSetting(section, "shield_rebuilt_sound");
                        string sh_collapse_eff = Util.TryGetStrSetting(section, "shield_collapse_particle");
                        string sh_hit_eff = Util.TryGetStrSetting(section, "shield_hit_eff");

                        if (sh_collapse_snd != null && !AudioChecks.SoundExists(sh_collapse_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("shield_collapse_sound"), "Sound doesn't exist!");
                        if (sh_rebuilt_snd != null && !AudioChecks.SoundExists(sh_rebuilt_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("shield_rebuilt_sound"), "Sound doesn't exist!");
                        if (sh_collapse_eff != null && !FXChecks.EffectExists(sh_collapse_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("shield_collapse_particle"), "Effect doesn't exist!");
                        if (sh_hit_eff != null && !FXChecks.EffectExists(sh_hit_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("shield_hit_eff"), "Effect doesn't exist!");
                        break;
                    case "tradelane":
                        foreach (var setting in section.settings)
                        {
                            switch (setting.settingName)
                            {
                                case "tl_player_travel":
                                case "tl_player_splash":
                                case "tl_ring_active":
                                case "tl_ship_disrupt":
                                case "tl_ship_enter":
                                case "tl_ship_exit":
                                case "tl_ship_travel":
                                    if(!FXChecks.EffectExists(setting.Str(0)))
                                        Logger.LogInvalidValue(file, setting, "Effect not found!");
                                    break;
                            }
                        }
                        break;
                    case "thruster":
                        string thr_hp_particles = Util.TryGetStrSetting(section, "hp_particles");
                        string thr_particles = Util.TryGetStrSetting(section, "particles");

                        if (thr_hp_particles != null && !Checker.DisableUTF && !equipUtf[cat][nick].HardpointExists(thr_hp_particles))
                            Logger.LogHardpoint(file, section.GetSetting("hp_particles"));
                        if (thr_particles != null && !FXChecks.EffectExists(thr_particles))
                            Logger.LogInvalidValue(file, section.GetSetting("particles"), "Effect doesn't exist!");
                        break;
                    case "engine":
                        foreach (var setting in section.settings)
                        {
                            string val = setting.Str(0);
                            switch (setting.settingName)
                            {
                                case "character_loop_sound":
                                case "character_start_sound":
                                case "cruise_backfire_sound":
                                case "cruise_disrupt_sound":
                                case "cruise_loop_sound":
                                case "cruise_start_sound":
                                case "cruise_stop_sound":
                                case "engine_kill_sound":
                                case "rumble_sound":
                                    if (!AudioChecks.SoundExists(val))
                                        Logger.LogInvalidValue(file, setting, "Sound not found!");
                                    break;
                                case "trail_effect":
                                case "trail_effect_player":
                                case "cruise_disrupt_effect":
                                case "flame_effect":
                                    if (!FXChecks.EffectExists(val))
                                        Logger.LogInvalidValue(file, setting, "Effect not found!");
                                    break;
                            }
                        }
                        break;
                    case "light":
                        string li_inherit = Util.TryGetStrSetting(section, "inherit");
                        if (li_inherit != null && !equipNicks["light"].Contains(li_inherit.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("inherit"), "Light not found!");
                        break;
                    case "countermeasuredropper":
                        string cmd_particle = Util.TryGetStrSetting(section, "flash_particle_name");
                        string cmd_light_anim = Util.TryGetStrSetting(section, "light_anim"); // TODO
                        string cmd_archtype = Util.TryGetStrSetting(section, "projectile_archetype");

                        if (cmd_particle != null && !FXChecks.EffectExists(cmd_particle))
                            Logger.LogInvalidValue(file, section.GetSetting("flash_particle_name"), "Effect not found!");
                        if (cmd_archtype != null && !equipNicks["countermeasure"].Contains(cmd_archtype.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("projectile_archetype"), "CounterMeasure not found!");
                        break;
                    case "tractor":
                        string trac_op_eff = Util.TryGetStrSetting(section, "operating_effect");
                        string trac_snd = Util.TryGetStrSetting(section, "tractor_complete_snd");

                        if (trac_op_eff != null && !FXChecks.EffectExists(trac_op_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("operating_effect"), "Effect doesn't exist!");
                        if (trac_snd != null && !AudioChecks.SoundExists(trac_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("tractor_complete_snd"), "Sound doesn't exist!");
                        break;
                    case "cloakingdevice":
                        string cd_in_eff = Util.TryGetStrSetting(section, "cloakin_fx");
                        string cd_out_eff = Util.TryGetStrSetting(section, "cloakout_fx");

                        if (cd_in_eff != null && !FXChecks.EffectExists(cd_in_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("cloakin_fx"), "Effect doesn't exist!");
                        if (cd_out_eff != null && !FXChecks.EffectExists(cd_out_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("cloakout_fx"), "Effect doesn't exist!");
                        break;
                    case "explosion":
                        string expl_eff = Util.TryGetStrSetting(section, "effect");
                        if(expl_eff != null && !FXChecks.EffectExists(expl_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("effect"), "Effect doesn't exist!");
                        break;
                    case "gun":
                    /*
[Gun] light_anim => [LightAnim] TODO
[Gun] projectile_archetype => TODO save reference for munition checks
                     */
                        string gun_snd = Util.TryGetStrSetting(section, "dry_fire_sound");
                        string gun_particle = Util.TryGetStrSetting(section, "flash_particle_name");
                        string gun_hpType = Util.TryGetStrSetting(section, "hp_gun_type");
                        string gun_projArch = Util.TryGetStrSetting(section, "projectile_archetype");

                        if (gun_snd != null && !AudioChecks.SoundExists(gun_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("dry_fire_sound"), "Sound doesn't exist!");
                        if(gun_particle != null && !FXChecks.EffectExists(gun_particle))
                            Logger.LogInvalidValue(file, section.GetSetting("flash_particle_name"), "Effect doesn't exist!");
                        if(gun_hpType != null && !HpTypeExists(gun_hpType))
                            Logger.LogInvalidValue(file, section.GetSetting("hp_gun_type"), "Invalid hardpoint-type!");
                        if (gun_projArch != null && !equipNicks["munition"].Contains(gun_projArch.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("dry_fire_sound"), "Munition doesn't exist!");

                        break;
                    case "mine":
                        string m_const_efect = Util.TryGetStrSetting(section, "const_efect");
                        string m_snd = Util.TryGetStrSetting(section, "one_shot_sound");

                        if (m_const_efect != null && !FXChecks.EffectExists(m_const_efect))
                            Logger.LogInvalidValue(file, section.GetSetting("const_efect"), "Effect doesn't exist!");
                        if (m_snd != null && !AudioChecks.SoundExists(m_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("one_shot_sound"), "Sound doesn't exist!");
                        break;
                    case "minedropper":
                        string md_snd = Util.TryGetStrSetting(section, "dry_fire_sound");
                        string md_projArch = Util.TryGetStrSetting(section, "projectile_archetype");

                        if (md_snd != null && !AudioChecks.SoundExists(md_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("dry_fire_sound"), "Sound doesn't exist!");
                        if (md_projArch != null && !equipNicks["mine"].Contains(md_projArch.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("dry_fire_sound"), "Mine doesn't exist!");
                        break;
                    case "munition":
                        string mun_const_eff = Util.TryGetStrSetting(section, "const_effect");
                        string mun_hit_eff = Util.TryGetStrSetting(section, "munition_hit_effect");
                        string mun_hp_type = Util.TryGetStrSetting(section, "hp_type");
                        string mun_motor = Util.TryGetStrSetting(section, "motor");
                        string mun_snd = Util.TryGetStrSetting(section, "one_shot_sound");
                        string mun_seeker = Util.TryGetStrSetting(section, "seeker");

                        if(mun_const_eff != null && !FXChecks.EffectExists(mun_const_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("const_effect"), "Effect not found!");
                        if(mun_hit_eff != null && !FXChecks.EffectExists(mun_hit_eff))
                            Logger.LogInvalidValue(file, section.GetSetting("munition_hit_effect"), "Effect not found!");
                        if(mun_hp_type != null && !HpTypeExists(mun_hp_type))
                            Logger.LogInvalidValue(file, section.GetSetting("hp_type"), "Invalid hardpoint type!");
                        if(mun_motor != null && !equipNicks["motor"].Contains(mun_motor.ToLower()))
                            Logger.LogInvalidValue(file, section.GetSetting("motor"), "Motor doesn't exist!");
                        if(mun_snd != null && !AudioChecks.SoundExists(mun_snd))
                            Logger.LogInvalidValue(file, section.GetSetting("one_shot_sound"), "Sound doesn't exist!");
                        if (mun_seeker != null && (mun_seeker.ToLower() != "dumb" && mun_seeker.ToLower() != "lock"))
                            Logger.LogInvalidValue(file, section.GetSetting("seeker"), "Must be DUMB or LOCK!");
                        break;
                    case "lod":
                        string obj = Util.TryGetStrSetting(section, "obj");
                        if (obj != null && (obj.ToLower() != "root" && obj.ToLower() != "barrel"))
                            Logger.LogInvalidValue(file, section.GetSetting("obj"), "Must be root or barrel!");
                        break;
                    case "armor":
                    case "internalfx":
                    case "motor": // TODO
                        break;
                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        private static void CheckGoodFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "good":
                                                                                                                     // Removed , str_ids_info, str_ids_name
                        string nick, category, equipment, item_icon, material_library, shop_archetype, msg_id_prefix, ship, hull;
                        string[][] addons;
                        FLDataFile.Setting[] addonSettings;
                        //uint ids_info, ids_name; unused

                        if (section.SettingExists("nickname"))
                        {
                            nick = section.GetSetting("nickname").Str(0);
                        }
                        else
                        {
                            Logger.LogSettingNotFound(file, section.settings.Count == 0 ? "0" : section.settings[0].LineNumber.ToString(), "Good", "nickname");
                            break;
                        }

                        category = Util.TryGetStrSetting(section, "category");
                        equipment = Util.TryGetStrSetting(section, "equipment");
                        ship = Util.TryGetStrSetting(section, "ship");
                        hull = Util.TryGetStrSetting(section, "hull");
                        addonSettings = section.GetSettings("addon").ToArray();
                        addons = addonSettings.Select(a => a.values.Select(s => s.ToString()).ToArray()).ToArray();
                        item_icon = Util.TryGetStrSetting(section, "item_icon");
                        material_library = Util.TryGetStrSetting(section, "material_library");
                        shop_archetype = Util.TryGetStrSetting(section, "shop_archetype");
                        msg_id_prefix = Util.TryGetStrSetting(section, "msg_id_prefix");

                        Util.CheckIDName(section, "ids_name", nick, file);
                        Util.CheckIDInfo(section, "ids_info", nick, file);

                        if (category != null && !GoodCategories.Contains(category))
                            Logger.LogInvalidValue(file, section.GetSetting("category"), "Not a valid category!");

                        if (category == null)
                        {
                            Logger.LogSettingNotFound(file, section.settings[0].LineNumber, section.sectionName, "category");
                            return;
                        }

                        switch (category.ToLower())
                        {
                            case "ship":
                                hull = hull.ToLower();
                                if (!shipHulls.ContainsKey(hull))
                                {
                                    Logger.LogInvalidValue(file, section.GetSetting("hull"), "Ship doesn't exist!", hull);
                                    break;
                                }

                                if (Checker.DisableUTF)
                                    break;
                                
                                if (!ShipChecks.shipUTF.ContainsKey(shipHulls[hull]))
                                {
                                    Logger.LogInvalidValue(file, section.GetSetting("hull"), "Ship hull not found", shipHulls[hull]);
                                    break;
                                }

                                UtfFile shipUtf = ShipChecks.shipUTF[shipHulls[hull]];
                                for (int i = 0; i < addons.Length; i++)
                                {
                                    string hp, equip;

                                    if (addons[i].Length == 3)
                                    {
                                        equip = addons[i][0];
                                        hp = addons[i][1];

                                        if (!EquipExists(equip))
                                            Logger.LogInvalidValue(file, addonSettings[i], "Equip doesn't exist!", equip);
                                        if (Checker.DisableUTF)
                                            break;
                                        if (hp != "internal" && !shipUtf.HardpointExists(hp))
                                            Logger.LogHardpoint(file, addonSettings[i], hp);
                                    }
                                    else
                                        Logger.LogArgCount(file, addonSettings[i], 3);
                                }
                                break;
                            case "equipment": // TODO
                            case "commodity":
                                break;
                            case "shiphull":
                                if (!ShipChecks.ShipExists(ship))
                                    Logger.LogInvalidValue(file, section.GetSetting("category"), "Ship doesn't exist!");
                                break;
                        }

                        if (item_icon != null && !File.Exists(Path.Combine(Checker.flDataPath, item_icon)))
                            Logger.LogFileNotFound(file, section.GetSetting("item_icon"));
                        if (material_library != null && !File.Exists(Path.Combine(Checker.flDataPath, material_library)))
                            Logger.LogFileNotFound(file, section.GetSetting("material_library"));
                        if (shop_archetype != null && !File.Exists(Path.Combine(Checker.flDataPath, shop_archetype)))
                            Logger.LogFileNotFound(file, section.GetSetting("shop_archetype"));

                        if (msg_id_prefix != null && !AudioChecks.VoiceMsgExists(msg_id_prefix))
                            Logger.LogInvalidValue(file, section.GetSetting("msg_id_prefix"), "Msg doesn't exist!");

                        break;
                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        private static void CheckMarketFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            foreach (var section in ini.sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "basegood":
                        string Base = section.GetSetting("base").Str(0);
                        
                        if(!UniverseChecks.BaseExists(Base))
                            Logger.LogInvalidValue(file, section.GetSetting("base"), "Base doesn't exist!");

                        var MarketGoods = section.GetSettings("MarketGood").ToArray();
                        foreach (var marketGood in MarketGoods)
                        {
                            if (marketGood.NumValues() < 7)
                                break;

                            string[] args = marketGood.values.Select(s => s.ToString().Trim()).ToArray();

                            string equipNick = args[0];
                            int rank;
                            float rep;
                            int min, max, _noSell;
                            bool noSell;
                            float multip;

                            if (!int.TryParse(args[1], out rank))
                                Logger.LogInvalidValue(file, marketGood, "Invalid rank! Has to be a number!", args[1]);
                            if (!float.TryParse(args[2].Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out rep))
                                Logger.LogInvalidValue(file, marketGood, "Invalid rep! Has to be a number!", args[2]);
                            else if(rep < -1 || rep > 1)
                                Logger.LogInvalidValue(file, marketGood, "Invalid rep! Has to be between -1 and 1!", args[2]);
                            if (!int.TryParse(args[3], out min))
                                Logger.LogInvalidValue(file, marketGood, "Invalid min! Has to be a number!", args[3]);
                            if (!int.TryParse(args[4], out max))
                                Logger.LogInvalidValue(file, marketGood, "Invalid max! Has to be a number!", args[4]);
                            if (!int.TryParse(args[5], out _noSell))
                                Logger.LogInvalidValue(file, marketGood, "Invalid noSell! Has to be 1 or 0!", args[5]);
                            else
                            {
                                if (_noSell == 0) noSell = false;
                                else if (_noSell == 1) noSell = true;
                                else Logger.LogInvalidValue(file, marketGood, "Invalid noSell! Has to be 1 or 0!", args[5]);
                            }
                            if (!float.TryParse(args[6], out multip))
                                Logger.LogInvalidValue(file, marketGood, "Invalid multiplier! Has to be a number!", args[6]);

                            if (!GoodExists(equipNick))
                                Logger.LogInvalidValue(file, marketGood, "Equipment doesn't exist!", equipNick);
                        }
                        break;
                    default:
                        Logger.WLog(string.Format("Unexpected section {0} in file {1}", section.sectionName, file));
                        break;
                }
            }
        }

        public static void ParseEquipmentFolder()
        {
            Logger.ILog("Parsing Equipment-Folder");
            FLDataFile.Section data = Checker.flIni.sections.First(s => s.sectionName.ToLower() == "data");

            equipmentFiles = data.GetSettings("equipment").Select(s => s.Str(0)).ToArray();
            goodFiles = data.GetSettings("goods").Select(s => s.Str(0)).ToArray();
            marketFiles = data.GetSettings("markets").Select(s => s.Str(0)).ToArray();
            weaponModFiles = data.GetSettings("WeaponModDB").Select(s => s.Str(0)).ToArray();
            equipUtf = new Dictionary<string, Dictionary<string, UtfFile>>();

            var equipNicks = new Dictionary<string, List<string>>();
            List<string> explosionNicks = new List<string>();
            LinkedList<string> str = new LinkedList<string>();
            List<string> goodNicknames = new List<string>();
            List<string> weaponTypes = new List<string>();
            shipHulls = new Dictionary<string, string>();
            marketEntries = new Dictionary<string, string[]>();

            foreach (var equipFile in equipmentFiles)
            {
                ParseEquipFile(equipFile, ref explosionNicks, ref equipNicks);
            }
            foreach (var goodFile in goodFiles)
            {
                ParseGoodFile(goodFile, ref goodNicknames);
            }
            foreach (var marketFile in marketFiles)
            {
                ParseMarketFile(marketFile);
            }
            foreach (var weaponModFile in weaponModFiles)
            {
                FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, weaponModFile), true);
                weaponTypes.AddRange(ini.GetSettings("WeaponType", "nickname").Select(s => s.Str(0)));
            }

            AllEquipNicks = equipNicks.SelectMany(n => n.Value).ToArray();
            EquipmentChecks.equipNicks = equipNicks.Select(n => new KeyValuePair<string, string[]>(n.Key, n.Value.ToArray())).ToDictionary(n => n.Key, n => n.Value);
            EquipmentChecks.goodNicknames = goodNicknames.ToArray();
            EquipmentChecks.weaponTypes = weaponTypes.ToArray();
            FXChecks.AddExplosions(explosionNicks);
            Logger.ILog("Finished Equipment-Folder");
        }

        private static void ParseEquipFile(string file, ref List<string> explosionNicks, ref Dictionary<string, List<string>> equipNicks)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var sections = ini.sections;

            foreach (var section in sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "armor":
                    case "attachedfx":
                    case "internalfx":
                    case "power":
                    case "thruster":
                    case "shield":
                    case "shieldgenerator":
                    case "shieldbattery":
                    case "repairkit":
                    case "countermeasure":
                    case "countermeasuredropper":
                    case "gun":
                    case "minedropper":
                    case "mine":
                    case "munition":
                    case "motor":
                    case "scanner":
                    case "tractor":
                    case "engine":
                    case "cloakingdevice":
                    case "tradelane":
                    case "cargopod":
                    case "lootcrate":
                    case "commodity":
                    case "light":
                        string cat = section.sectionName.ToLower();
                        if(!equipNicks.ContainsKey(cat))
                            equipNicks[cat] = new List<string>();
                        if(!equipUtf.ContainsKey(cat))
                            equipUtf[cat] = new Dictionary<string, UtfFile>();

                        var nickUtfTuple = Util.ParseNickUTF(Checker.flDataPath, file, section, "nickname", "da_archetype", false);
                        nickUtfTuple.Nick = nickUtfTuple.Nick.ToLower();

                        equipNicks[cat].Add(nickUtfTuple.Nick);

                        if (!Checker.DisableUTF && nickUtfTuple.UtfFile != null)
                        {
                            equipUtf[cat][nickUtfTuple.Nick] = nickUtfTuple.UtfFile;
                        }
                        break;
                    case "explosion":
                        explosionNicks.Add(section.GetSetting("nickname").Str(0).ToLower());
                        break;
                    case "lod":
                        break;
                }
            }
        }

        private static void ParseGoodFile(string file, ref List<string> goodNicknames)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            foreach (var section in ini.sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "good":
                        string nick = section.GetSetting("nickname").Str(0).ToLower();
                        goodNicknames.Add(nick);

                        string category = Util.TryGetStrSetting(section, "category");
                        if (category == "shiphull")
                        {
                            string ship = Util.TryGetStrSetting(section, "ship");
                            shipHulls.Add(nick.ToLower(), ship.ToLower());
                        }
                        break;
                }
            }
        }

        private static void ParseMarketFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var sections = ini.sections;

            foreach (var section in sections)
            {
                switch (section.sectionName.ToLower())
                {
                    case "basegood":
                        string Base = section.GetSetting("base").Str(0);
                        string[] MarketGoods = section.GetSettings("MarketGood").Select(s => s.Str(0).ToLower()).ToArray();
                        if (marketEntries.ContainsKey(Base))
                            marketEntries[Base] = marketEntries[Base].Union(MarketGoods).ToArray();
                        else
                            marketEntries[Base] = MarketGoods;
                        break;
                }
            }
        }

        public static string GetCategory(string nick)
        {
            nick = nick.ToLower();
            foreach (var nicks in equipNicks)
            {
                if (nicks.Value.Contains(nick))
                    return nicks.Key;
            }
            return null;
        }

        public static bool EquipExists(string nick)
        {
            nick = nick.ToLower();
            return AllEquipNicks.Contains(nick);
        }

        public static bool GoodExists(string nick)
        {
            nick = nick.ToLower();
            return goodNicknames.Contains(nick);
        }

        public static bool HpTypeExists(string hpType)
        {
            hpType = hpType.ToLower();
            return hpTypes.Contains(hpType);
        }

        public static bool ShieldTypeExists(string type)
        {
            type = type.ToLower();
            return shieldTypes.Contains(type);
        }

        public static bool WeaponTypeExists(string type)
        {
            type = type.ToLower();
            return weaponTypes.Contains(type);
        }
    }
}
