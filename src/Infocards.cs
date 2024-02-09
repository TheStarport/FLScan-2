using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using FLScanIE.Util_Functions;
using FLScanIE.Logging;

namespace FLScanIE
{
    public static class Infocards
    {
        public struct Infocard
        {
            public bool is_string_name;
            public string text;
            public bool has_error;
            public bool changed;
            public string dll;
        }

        static private Dictionary<int, Infocard> infocards = new Dictionary<int, Infocard>();

        /// <summary>
        /// Unmanaged functions to access libraries
        /// </summary>
        private const int DONT_RESOLVE_DLL_REFERENCES = 0x00000001;
        private const int LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadLibraryExA(string lpLibFileName, int hFile, int dwFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int LoadStringW(IntPtr hInstance, int uID, byte[] lpBuffer, int nBufferMax);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int FreeLibrary(IntPtr hInstance);

        [DllImport("kernel32.dll")]
        static extern IntPtr FindResource(IntPtr hModule, int lpID, int lpType);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);

        struct ResourceDLL
        {
            public string path;
            public IntPtr hInstance;
        }

        /// <summary>
        /// Resource dlls containing strings.
        /// </summary>
        static List<ResourceDLL> vDLLs = new List<ResourceDLL>();

        static private void LoadLibrary(string dllPath)
        {
            IntPtr hInstance = LoadLibraryExA(dllPath, 0, DONT_RESOLVE_DLL_REFERENCES | LOAD_LIBRARY_AS_DATAFILE);
            if (hInstance != IntPtr.Zero)
            {
                ResourceDLL dll = new ResourceDLL();
                dll.path = dllPath;
                dll.hInstance = hInstance;
                vDLLs.Add(dll);
            }
        }

        public static bool IsName(int IDS)
        {
            if(IDS < 0)
                throw new ArgumentException("Only non-negative values are allowed!", "IDS");
            return IsName((uint)IDS);
        }

        public static bool IsName(uint IDS)
        {
            if (!infocards.ContainsKey((int)IDS))
                return false;
            if (!infocards[(int)IDS].is_string_name)
                return false;
            return true;
        }

        public static bool IsInfocard(int IDS)
        {
            if (IDS < 0)
                throw new ArgumentException("Only non-negative values are allowed!", "IDS");
            return IsInfocard((uint)IDS);
        }

        public static bool IsInfocard(uint IDS)
        {
            if (!infocards.ContainsKey((int)IDS))
                return false;
            if (infocards[(int)IDS].is_string_name)
                return false;
            return true;
        }

        public static void AddInfocard(int ids, bool is_string_name, string text, bool has_error, bool changed, string dll)
        {
            Infocard info = new Infocard();
            info.is_string_name = is_string_name;
            info.text = text;
            info.has_error = has_error;
            info.changed = changed;
            info.dll = dll;
            infocards[ids] = info;
        }

        public static void Load()
        {
            Logger.ILog("Loading infocards");
            infocards.Clear();
            vDLLs.Clear();
            string flExePath =  Path.GetFullPath(Checker.flDataPath + "\\..\\EXE");

            // Load the string dlls into memory
            LoadLibrary(Path.Combine(flExePath, "resources.dll"));
            foreach (FLDataFile.Setting flIniEntry in Checker.flIni.GetSettings("Resources", "DLL"))
                LoadLibrary(Path.Combine(flExePath, flIniEntry.Str(0)));

            // Pull out all infocards into memory.
            for (int iDLL = 0; iDLL < vDLLs.Count; iDLL++)
            {
                IntPtr hInstance = vDLLs[iDLL].hInstance;
                string dllPath = vDLLs[iDLL].path;
                Logger.ILog("Parsing " + dllPath);

                for (int resId = 0; resId < 0x10000; resId++)
                {
                    int iIDS = (iDLL * 0x10000) + resId;

                    byte[] bufName = new byte[0x10000];
                    int len = LoadStringW(hInstance, resId, bufName, bufName.Length);
                    if (len > 0)
                    {
                        if (infocards.ContainsKey(iIDS))
                        {
                            var first = infocards[iIDS];
                            Logger.WLog(string.Format("Duplicate ids {0}, first: {1}, second: {2}", iIDS, first.dll, dllPath));
                        }
                        string text = System.Text.Encoding.Unicode.GetString(bufName, 0, len * 2);
                        AddInfocard(iIDS, true, text, false, false, dllPath);
                    }

                    IntPtr hFindRes = FindResource(hInstance, resId, 23);
                    if (hFindRes != IntPtr.Zero)
                    {
                        IntPtr resContent = LoadResource(hInstance, hFindRes);
                        if (resContent != IntPtr.Zero)
                        {
                            int size = SizeofResource(hInstance, hFindRes);
                            byte[] bufInfo = new byte[size];
                            Marshal.Copy(resContent, bufInfo, 0, (int)size);

                            int start = 0;
                            int end = size - 1;

                            // Strip the unicode 16 little endian bom from the start
                            // and the \n\n and null at the end of the string. We will
                            // re-add these if we save changes.
                            if (size > 1 && bufInfo[0] == 0xFF && bufInfo[1] == 0xFE)
                                start += 2;

                            while (end > (start + 2) && bufInfo[end - 1] == 0x0A && bufInfo[end] == 0x00)
                                end -= 2;

                            while (end > (start + 2) && bufInfo[end - 1] == 0x0D && bufInfo[end] == 0x00)
                                end -= 2;

                            if (end <= start)
                            {
                                Logger.WLog("No content for ids " + iIDS);
                                continue;
                            }

                            bool fix_failed = false;
                            if (end > start && bufInfo[end - 1] != 0x3E || bufInfo[end] != 0x00)
                            {
                                int end_before_fix_attempt = end;
                                //Logger.WLog("'>' not found at end of ids " + iIDS);

                                // fix corrupted strings
                                if (end > (start + 1) && bufInfo[end - 1] == 0x00 && bufInfo[end] == 0x00)
                                    end -= 1;

                                // remove whitespace
                                while (end > (start + 2) && bufInfo[end - 1] == 0x20 && bufInfo[end] == 0x00)
                                    end -= 2;

                                // fix missing L
                                if (end > start && bufInfo[end - 1] == 0x44 && bufInfo[end] == 0x00)
                                {
                                    end += 2;
                                    bufInfo[end - 1] = 0x4c;
                                    bufInfo[end] = 0x00;
                                }

                                // fix missing >
                                if (end > start && bufInfo[end - 1] == 0x4c && bufInfo[end] == 0x00)
                                {
                                    end += 2;
                                    bufInfo[end - 1] = 0x3E;
                                    bufInfo[end] = 0x00;
                                }

                                if (bufInfo[end - 1] != 0x3E || bufInfo[end] != 0x00)
                                {
                                    fix_failed = true;
                                    end = end_before_fix_attempt;
                                }
                                else if (end <= start)
                                {
                                    fix_failed = true;
                                    end = end_before_fix_attempt;
                                }
                            }

                            int count = end - start + 1;
                            string text = System.Text.Encoding.Unicode.GetString(bufInfo, start, count);

                            if (infocards.ContainsKey(iIDS))
                            {
                                var first = infocards[iIDS];
                                Logger.WLog(string.Format("Duplicate ids {0}, first: {1}, second: {2}", iIDS, first.dll, dllPath));
                            }
                            AddInfocard(iIDS, false, text, fix_failed, false, dllPath);
                        }
                    }
                }
            }

            // Unload the dlls.
            foreach (ResourceDLL dll in vDLLs)
                FreeLibrary(dll.hInstance);
            Logger.ILog("Finished infocards");
        }
    }
}
