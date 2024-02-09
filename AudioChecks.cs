using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class AudioChecks
    {
        private static string[] soundFiles;
        private static string[] soundNicknames;

        private static string[] voiceFiles;
        private static string[] voiceMsgs;
        private static string[] voiceNicks;

        public static void CheckAudioFolder()
        {
            Logger.ILog("Checking Audio-Folder");
            Util.RunChecks(CheckSoundFile, soundFiles);
            // TODO voice files, the nicknames hash has to exists as node in the utf-file
            Logger.ILog("Finished Audio-Folder");
        }

        private static void CheckSoundFile(string file)
        {
            FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, file), true);

            var fuseEffects = ini.GetSettings("Sound", "file");

            foreach (var fuseEffect in fuseEffects)
            {
                if (!File.Exists(Checker.flDataPath + Path.DirectorySeparatorChar + fuseEffect.Str(0)))
                    Logger.LogFileNotFound(file, fuseEffect);
            }
        }

        public static void ParseAudioFolder()
        {
            Logger.ILog("Parsing Audio-Folder");

            var voiceMsgs = new List<string>();
            var voiceNicks = new List<string>();

            string[][] sounds = Util.GetNicks("Data", "sounds", "Sound", "nickname");
            soundFiles = sounds[0];
            soundNicknames = sounds[1].Select(n => n.ToLower()).ToArray();

            voiceFiles = Checker.flIni.GetSettings("Data", "voices").Select(s => s.Str(0)).ToArray();

            foreach (var voiceFile in voiceFiles)
            {
                FLDataFile ini = new FLDataFile(Path.Combine(Checker.flDataPath, voiceFile), false);
                var allSettings = ini.sections.SelectMany(s => s.settings).Where(s => s.sectionName.ToLower() == "sound" || s.sectionName.ToLower() == "voice").ToArray();
                voiceMsgs.AddRange(allSettings.Where(s => s.settingName == "msg").Select(s => s.Str(0).ToLower()).ToArray());
                voiceNicks.AddRange(allSettings.Where(s => s.settingName == "nickname").Select(s => s.Str(0).ToLower()).ToArray());
            }

            AudioChecks.voiceMsgs = voiceMsgs.ToArray();
            AudioChecks.voiceNicks = voiceNicks.ToArray();

            Logger.ILog(string.Format("Finished Audio-Folder, found {0} sounds and {1} voice messages", soundNicknames.Length, AudioChecks.voiceMsgs.Length));
        }

        public static bool SoundExists(string soundNick)
        {
            soundNick = soundNick.ToLower();
            return soundNicknames.Contains(soundNick);
        }

        public static bool VoiceMsgExists(string voiceMsg)
        {
            voiceMsg = voiceMsg.ToLower();
            return voiceMsgs.Contains(voiceMsg);
        }

        public static bool VoiceExists(string nick)
        {
            nick = nick.ToLower();
            return voiceNicks.Contains(nick);
        }
    }
}
