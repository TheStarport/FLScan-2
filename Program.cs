using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Diagnostics;
using FLScanIE.Logging;

namespace FLScanIE
{
    static class Program
    {
        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        private static FileStream fs;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if(args.Length == 0)
                RunGui();
            else
                try
                {
                    RunCommandline(args);
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    try { fs.Close(); }
                    catch { }
                }
            Logger.Abort();
        }

        private static void RunGui()
        {
            // You can't write to the console when the application-type is set to "Windows-Forms-Application"
            // so I had to set it to Console-Application, and hide the console window here ... :-/
            ShowWindow((int)Process.GetCurrentProcess().MainWindowHandle, 0);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }

        private static void RunCommandline(string[] args)
        {
            Console.WindowWidth = 160;
            Console.BufferHeight = 9000;

            Dictionary<string, string> options = new Dictionary<string, string>(args.Length);

            foreach (var arg in args)
            {
                string[] parts = arg.Split('=');
                if (parts.Length == 2)
                    options.Add(parts[0], parts[1]);
                else
                    options.Add(parts[0], null);
            }

            if (options.ContainsKey("--help"))
            {
                Console.WriteLine("Options:");
                Console.WriteLine("\tRequired:");
                Console.WriteLine("\t\t--flpath");
                Console.WriteLine("\tOptional:");
                Console.WriteLine("\t\t--disable-utf");
                Console.WriteLine("\t\t--disable-all-checks");
                Console.WriteLine("\t\t--enable-<CheckName>");
                Console.WriteLine("\t\t--disable-<CheckName>");
                Console.WriteLine("\tLogging:");
                Console.WriteLine("\t\t--log-html=<FileName>");
                Console.WriteLine("\t\t--log-file=<FileName>");
                Console.WriteLine("\t\t--log-console (implied when html and file aren't set)");
                Console.WriteLine("\tChecknames:");
                foreach (var check in Enum.GetNames(typeof(Checks)))
                {
                    if(check != "None")
                        Console.WriteLine("\t\t" + check);
                }
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("FLScanII.exe --flpath=C:\\Freelancer --disable-all-checks --enable-solar");
                Console.WriteLine("FLScanII.exe --flpath=C:\\Freelancer --disable-universe");
                Console.WriteLine("FLScanII.exe --flpath=C:\\Freelancer --log-html=MyFile.html");
                Console.WriteLine("Logging to console and to a file:");
                Console.WriteLine("FLScanII.exe --flpath=C:\\Freelancer --log-file=MyFile.html --log-console");
                return;
            }

            if (options.ContainsKey("--log-html"))
            {
                File.Delete(options["--log-html"]);
                fs = File.OpenWrite(options["--log-html"]);
                WriteLineToFile(fs, "<html><body><h2>FLScanII Log</h2><table width=\"100%\" border=\"1\">");
                Logger.HandleLog += LogToHTML;
            }
            else if(options.ContainsKey("--log-file"))
            {
                fs = File.OpenWrite(options["--log-file"]);
                Logger.HandleLog += LogToFile;
            }

            if (options.ContainsKey("--log-console") || !options.ContainsKey("--log-file") && !options.ContainsKey("--log-html"))
            {
                Logger.HandleLog += LogToConsole;
            }

            if (options.ContainsKey("--disable-utf"))
                Checker.DisableUTF = true;

            if (options.ContainsKey("--disable-all-checks"))
            {
                Checker.Checks = Checks.None;

                foreach (var option in options)
                {
                    if (option.Key.StartsWith("--enable"))
                    {
                        Checker.Checks = Checker.Checks | (Checks)Enum.Parse(typeof(Checks), option.Key.Substring(9), true);
                    }
                }
            }
            else
            {
                List<string> disabledChecks = new List<string>();
                foreach (var option in options)
                {
                    if (option.Key.StartsWith("--disable"))
                    {
                        disabledChecks.Add(option.Key.Substring(10).ToLower());
                    }
                }

                foreach (var check in Enum.GetValues(typeof(Checks)))
                {
                    if (!disabledChecks.Contains(check.ToString().ToLower()))
                        Checker.Checks = Checker.Checks | (Checks)check;
                }
            }

            if (options.ContainsKey("--flpath"))
            {
                Logger.ILog("Scan started");
                bool retn = Checker.Parse(options["--flpath"]);
                if (!retn)
                    return;
                Checker.Check();
                Logger.ILog("Scan complete");
            }
            else
                Console.WriteLine("Missing --flpath argument!");

            Logger.WaitForEnd();
            if (options.ContainsKey("--log-html"))
            {
                WriteLineToFile(fs, "</table></body></html>");
            }
        }

        private static void LogToConsole(LogEntry log)
        {
            switch (log.Loglevel)
            {
                case LogLevel.info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                case LogLevel.warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.fatal:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
            }
            Console.WriteLine(log.ToString());
            Console.ResetColor();
        }

        private static void LogToFile(LogEntry log)
        {
            WriteLineToFile(fs, log.ToString());
        }

        private static void LogToHTML(LogEntry log)
        {
            var sb = new StringBuilder();
            sb.Append("<tr style=\"color:");
            switch (log.Loglevel)
            {
                case LogLevel.info:
                    sb.Append("gray");
                    break;
                case LogLevel.warning:
                    sb.Append("orange");
                    break;
                case LogLevel.error:
                    sb.Append("red");
                    break;
                case LogLevel.fatal:
                    sb.Append("darkred");
                    break;
            }
            sb.Append("\">");
            string msg = log.ToString();
            List<string> parts = new List<string>();

            foreach (Match match in new Regex(@"\[[a-zA-Z0-9_:\.-]+\]").Matches(msg))
            {
                parts.Add(match.Value.Substring(1, match.Value.Length - 2).Trim());
            }
            parts.Add(msg.Substring(msg.LastIndexOf("]") + 2).Trim());

            sb.AppendFormat("<td width=\"20px\">{0}</td>", log.Loglevel.ToString().ToUpper());

            if (parts.Count == 3)
            {
                sb.AppendFormat("<td>{0}</td>", parts[1]);
                sb.AppendFormat("<td>{0}</td>", parts[2]);
            }
            else if(parts.Count == 2)
            {
                sb.AppendFormat("<td>&nbsp;</td>");
                sb.AppendFormat("<td>{0}</td>", parts[1]);
            }
            sb.Append("</tr>");

            WriteLineToFile(fs, sb.ToString());
        }

        private static void WriteLineToFile(FileStream fs, string str)
        {
            var buff = Encoding.ASCII.GetBytes(str + "\r\n");
            fs.Write(buff, 0, buff.Length);
        }
    }
}
