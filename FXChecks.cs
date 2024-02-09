using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    // TODO: effect_shapes [BeamBolt] [BeamSpear]
    public static class FXChecks
    {
        private static string[] effectFiles;
        private static string[] effectNicks;

        private static string[] explosionFiles;
        private static string[] explosionNicks;

        private static string[] fuseFiles;
        private static string[] fuseNicks;

        private static string[] effectTypes;
        private static string[] debrisNicknames;

        public static void CheckFX()
        {
            Logger.ILog("Checking FX-Folder");
            Util.RunChecks(CheckEffectFile, effectFiles);
            Util.RunChecks(CheckExplosionFile, explosionFiles);
            Util.RunChecks(CheckFuseFile, fuseFiles);
            Logger.ILog("Finished FX-Folder");
        }

        private static void CheckEffectFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var effects = ini.GetSettings("Effect", null).Union(ini.GetSettings("JumpGateEffect")).Union(ini.GetSettings("JumpShipEffect"));
            var visEffects = ini.sections.Where(s => s.sectionName.ToLower() == "viseffect").ToArray();

            foreach (var effect in effects)
            {
                switch (effect.settingName)
                {
                    case "vis_generic":
                    case "vis_effect":
                    case "glow_ring_effect":
                        foreach (var value in effect.values.Select(v => v.ToString()))
                        {
                            if (!EffectExists(value))
                                Logger.LogInvalidValue(file, effect, "Effect doesn't exist!", value);
                        }
                        break;
                    case "effect_type":
                        if (!EffectTypeExists(effect.Str(0)))
                            Logger.LogInvalidValue(file, effect, "Effect-type doesn't exist!");
                        break;
                    case "snd_effect":
                        if (!AudioChecks.SoundExists(effect.Str(0)))
                            Logger.LogInvalidValue(file, effect, "Sound doesn't exist!");
                        break;
                    case "jump_tunnel_effect":
                    case "jump_tunnel":
                    case "jump_out_effect":
                    case "jump_in_effect":
                        if (!EffectExists(effect.Str(0)))
                            Logger.LogInvalidValue(file, effect, "Effect doesn't exist!");
                        break;
                }
            }

            foreach (var visEffect in visEffects)
            {
                CheckVisEffect(visEffect, file);
            }
        }

        private static void CheckVisEffect(FLDataFile.Section visEffect, string file)
        {
            string alchemy = null;

            if (visEffect.SettingExists("alchemy"))
            {
                alchemy = visEffect.GetSetting("alchemy").Str(0);
                if (!File.Exists(Path.Combine(Checker.flDataPath, alchemy)))
                    Logger.LogFileNotFound(file, visEffect.GetSetting("alchemy"));
            }

            if (visEffect.SettingExists("textures"))
            {
                alchemy = visEffect.GetSetting("textures").Str(0);
                if (!File.Exists(Path.Combine(Checker.flDataPath, alchemy)))
                    Logger.LogFileNotFound(file, visEffect.GetSetting("textures"));
            }

            if (visEffect.SettingExists("effect_crc"))
            {
                Int64 effectCrc = Int64.Parse(visEffect.GetSetting("effect_crc").Str(0));

                if(alchemy == null)
                    Logger.LogInvalidValue(file, visEffect.GetSetting("effect_crc"), "effect_crc exists but alchemy doesn't!");
                else
                {
                    // TODO: check hash
                }
            }
        }

        private static void CheckExplosionFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var effects = ini.GetSettings("explosion", null);
            var debris = ini.GetSettings("Debris", null);

            foreach (var effect in effects)
            {
                switch (effect.settingName)
                {
                    case "effect":
                        if (!EffectExists(effect.Str(0)))
                            Logger.LogInvalidValue(file, effect, "Effect doesn't exist!");
                        break;
                    case "debris_type":
                        if (!DebrisExists(effect.Str(0)))
                            Logger.LogInvalidValue(file, effect, "Debris doesn't exist!");
                        break;
                    case "progress":
                        string str = effect.Str(0).ToLower();
                        if (str != "shatter" && str != "disappear" && str != "none")
                            Logger.LogInvalidValue(file, effect, "Has to be shatter, disappear or none.");
                        break;
                    case "innards_debris_object": // TODO: is any object useable here? or just [Simple] ones? There are simples in explosion.ini and in shiparch.ini, are both useable?
                        break;
                }
            }

            foreach (var debr in debris)
            {
                switch (debr.settingName)
                {
                    case "explosion":
                        if (!ExplosionExists(debr.Str(0)))
                            Logger.LogInvalidValue(file, debr, "Explosion doesn't exist!");
                        break;
                    case "trail":
                        if (!EffectExists(debr.Str(0)))
                            Logger.LogInvalidValue(file, debr, "Effect doesn't exist!");
                        break;
                }
            }
        }

        private static void CheckFuseFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);
            var fuseEffects = ini.GetSettings("start_effect", "effect");

            foreach (var fuseEffect in fuseEffects)
            {
                if (!EffectExists(fuseEffect.Str(0)))
                    Logger.LogInvalidValue(file, fuseEffect, "Effect doesn't exist!");
            }
        }
        
        public static void ParseFXFolder()
        {
            Logger.ILog("Parsing FX-Folder");

            string[][] effects = Util.GetNicks(new[] { "Data" }, new[] { "effects", "jump_effect", "gate_tunnels" }, new[] { "effect", "VisEffect", "JumpGateEffect", "gate_tunnel" }, new[] { "nickname" });
            FXChecks.effectFiles = effects[0];
            FXChecks.effectNicks = effects[1].Select(n => n.ToLower()).ToArray();
            FXChecks.effectTypes = Util.GetNicks("Data", "effects", "EffectType", "nickname")[1].Select(n => n.ToLower()).ToArray();

            string[][] explosions = Util.GetNicks("Data", "explosions", "explosion", "nickname");
            FXChecks.explosionFiles = explosions[0];
            FXChecks.explosionNicks = explosions[1].Select(n => n.ToLower()).ToArray();
            FXChecks.debrisNicknames = Util.GetNicks("Data", new[] { "explosions", "debris" }, "Debris", "nickname")[1].Select(n => n.ToLower()).ToArray();

            string[][] fuses = Util.GetNicks("Data", "fuses", "fuse", "name");
            FXChecks.fuseFiles = fuses[0];
            FXChecks.fuseNicks = fuses[1].Select(n => n.ToLower()).ToArray();

            Logger.ILog(string.Format("Finished FX-Folder, found {0} effects, {1} explosions and {2} fuses", effectNicks.Length, explosionNicks.Length, fuseNicks.Length));
        }

        public static void AddExplosions(IEnumerable<string> explosions)
        {
            FXChecks.explosionNicks = FXChecks.explosionNicks.Union(explosions).ToArray();
        }

        public static bool EffectExists(string nick)
        {
            nick = nick.ToLower();
            return effectNicks.Contains(nick);
        }

        public static bool ExplosionExists(string nick)
        {
            nick = nick.ToLower();
            return explosionNicks.Contains(nick);
        }

        public static bool FuseExists(string nick)
        {
            nick = nick.ToLower();
            return fuseNicks.Contains(nick);
        }

        public static bool EffectTypeExists(string nick)
        {
            nick = nick.ToLower();
            return effectTypes.Contains(nick);
        }

        public static bool DebrisExists(string nick)
        {
            nick = nick.ToLower();
            return debrisNicknames.Contains(nick);
        }
    }
}
