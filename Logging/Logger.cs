using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using FLScanIE.Util_Functions;

namespace FLScanIE.Logging
{
    /// <summary>
    /// Delegate used by Logger.HandleLog
    /// </summary>
    /// <param name="log">The log</param>
    public delegate void HandleLogDelegate(LogEntry log);
    /// <summary>
    /// Logs things
    /// </summary>
    public static class Logger
    {
        private static readonly Queue<LogEntry> logs;
        private static readonly Thread logThread;
        private static bool abort;

        public static event HandleLogDelegate HandleLog;

        static Logger()
        {
            logs = new Queue<LogEntry>(10000);
            abort = false;
            logThread = new Thread(HandleLogs);
            logThread.Priority = ThreadPriority.BelowNormal;
            logThread.Name = "LoggerThread";
            logThread.Start();
        }

        /// <summary>
        /// Logs <paramref name="log"/>
        /// </summary>
        /// <param name="log">The Log to log</param>
        public static void Log(LogEntry log)
        {
            if(log == null)
                throw new ArgumentNullException("log");
            logs.Enqueue(log);
        }

        /// <summary>
        /// Creates a new Log and logs it
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The LogLevel</param>
        public static void Log(string message, LogLevel level)
        {
            Log(new LogEntry(message, level));
        }

        /// <summary>
        /// Logs a new Log with <c>LogLevel.info</c> as level
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void ILog(string message)
        {
            Log(message, LogLevel.info);
        }

        /// <summary>
        /// Logs a new Log with <c>LogLevel.warning</c> as level
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void WLog(string message)
        {
#if DEBUG
            if (message.StartsWith("Unexpected section "))
                throw new NotImplementedException();
#endif
            Log(message, LogLevel.warning);
        }

        /// <summary>
        /// Logs a new Log with <c>LogLevel.error</c> as level
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void ELog(string message)
        {
            Log(message, LogLevel.error);
        }

        /// <summary>
        /// Logs a new Log with <c>LogLevel.fatal</c> as level
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void FLog(string message)
        {
            Log(message, LogLevel.fatal);
        }

        /// <summary>
        /// Log a new <c>FileNotFoundLogEntry</c>
        /// The name of the missing file will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        public static void LogFileNotFound(string file, FLDataFile.Setting setting)
        {
            Log(new FileNotFoundLogEntry(file, setting));
        }

        /// <summary>
        /// Log a new <c>FileNotFoundLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="missingFile">The name of the missing file</param>
        public static void LogFileNotFound(string file, FLDataFile.Setting setting, string missingFile)
        {
            Log(new FileNotFoundLogEntry(file, missingFile, setting));
        }

        /// <summary>
        /// Logs a new <c>SettingNotFoundLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="identifier">A identifer to help the user finding the section (such as a nickname or linenumber)</param>
        /// <param name="section">The section name</param>
        /// <param name="setting">The name of the missing setting</param>
        public static void LogSettingNotFound(string file, object identifier, string section, string setting)
        {
            Log(new SettingNotFoundLogEntry(file, identifier, section, setting));
        }

        /// <summary>
        /// Logs a new <c>InvalidValueCountLogEntry</c>
        /// The actual number of values will be taken from <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="expectedCount">The expected number of values</param>
        public static void LogArgCount(string file, FLDataFile.Setting setting, params int[] expectedCount)
        {
            Log(new InvalidValueCountLogEntry(file, setting, expectedCount));
        }

        /// <summary>
        /// Logs a new <c>InvalidValueLogEntry</c>
        /// The wrong value will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A error message (such as "Hardpoint not found!")</param>
        public static void LogInvalidValue(string file, FLDataFile.Setting setting, string message)
        {
            Log(new InvalidValueLogEntry(message, file, setting));
        }

        /// <summary>
        /// Logs a new <c>InvalidValueLogEntry</c>
        /// The wrong value will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A error message (such as "Hardpoint not found!")</param>
        /// <param name="level"> </param>
        public static void LogInvalidValue(string file, FLDataFile.Setting setting, string message, LogLevel level)
        {
            var entry = new InvalidValueLogEntry(message, file, setting);
            entry.Loglevel = level;
            Log(entry);
        }

        /// <summary>
        /// Logs a new <c>InvalidValueLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A error message (such as "Hardpoint not found!")</param>
        /// <param name="foundValue">The invalid value</param>
        public static void LogInvalidValue(string file, FLDataFile.Setting setting, string message, string foundValue)
        {
            Log(new InvalidValueLogEntry(message, file, setting, foundValue));
        }

        /// <summary>
        /// Logs a new <c>InvalidValueLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A error message (such as "Hardpoint not found!")</param>
        /// <param name="foundValue">The invalid value</param>
        /// <param name="level">The entrys <c>LogLevel</c></param>
        public static void LogInvalidValue(string file, FLDataFile.Setting setting, string message, string foundValue, LogLevel level)
        {
            var entry = new InvalidValueLogEntry(message, file, setting, foundValue);
            entry.Loglevel = level;
            Log(entry);
        }

        /// <summary>
        /// Logs a new <c>IDNotFoundLogEntry</c>
        /// The wrong ID will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        public static void LogIDError(string file, FLDataFile.Setting setting)
        {
            Log(new IDNotFoundLogEntry(file, setting));
        }


        /// <summary>
        /// Logs a new <c>HardpointNotFoundLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        public static void LogHardpoint(string file, FLDataFile.Setting setting)
        {
            Log(new HardpointNotFoundLogEntry(file, setting));
        }

        /// <summary>
        /// Logs a new <c>HardpointNotFoundLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="foundValue">The hardpoint</param>
        public static void LogHardpoint(string file, FLDataFile.Setting setting, string foundValue)
        {
            Log(new HardpointNotFoundLogEntry(file, setting, foundValue));
        }

        /// <summary>
        /// Logs a new <c>DoubleValueLogEntry</c>
        /// The doubled value will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A message describing the error</param>
        public static void LogDublicateValue(string file, FLDataFile.Setting setting, string message)
        {
            Log(new DublicateValueLogEntry(message, file, setting, LogLevel.error));
        }

        /// <summary>
        /// Logs a new <c>DoubleValueLogEntry</c>
        /// The doubled value will be the first value of <paramref name="setting"/>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A message describing the error</param>
        /// <param name="level">The entrys <c>LogLevel</c></param>
        public static void LogDublicateValue(string file, FLDataFile.Setting setting, string message, LogLevel level)
        {
            Log(new DublicateValueLogEntry(message, file, setting, level));
        }

        /// <summary>
        /// Logs a new <c>DoubleValueLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A message describing the error</param>
        public static void LogDublicateValue(string file, FLDataFile.Setting setting, string message, string foundValue)
        {
            Log(new DublicateValueLogEntry(message, file, setting, LogLevel.error, foundValue));
        }

        /// <summary>
        /// Logs a new <c>DoubleValueLogEntry</c>
        /// </summary>
        /// <param name="file">The ini file</param>
        /// <param name="setting">The setting which caused the log</param>
        /// <param name="message">A message describing the error</param>
        /// <param name="level">The entrys <c>LogLevel</c></param>
        /// <param name="foundValue"></param>
        public static void LogDublicateValue(string file, FLDataFile.Setting setting, string message, LogLevel level, string foundValue)
        {
            Log(new DublicateValueLogEntry(message, file, setting, level, foundValue));
        }

        /// <summary>
        /// Tells the logger to shutdown
        /// </summary>
        public static void Abort()
        {
            Abort(false);
        }

        /// <summary>
        /// Tells the logger to shutdown, forces it it <paramref name="force"/> is true
        /// </summary>
        /// <param name="force">true if the shutdown sould be forced, otherwise false</param>
        public static void Abort(bool force)
        {
            abort = true;
            if (force)
                logThread.Abort();
        }

        /// <summary>
        /// Fires the HandleLog-event
        /// </summary>
        /// <param name="log"></param>
        private static void OnHandleLog(LogEntry log)
        {
            if (HandleLog != null)
            {
                HandleLog.Invoke(log);
            }
        }

        /// <summary>
        /// Watches out for new logs and calls OnHandleLog
        /// </summary>
        private static void HandleLogs()
        {
            while (true)
            {
                if (logs.Count == 0)
                {
                    Thread.Sleep(100);

                    if (abort && logs.Count == 0)
                        return;
                }
                else
                {
                    while (logs.Count != 0)
                        OnHandleLog(logs.Dequeue());
                }
            }
        }

        public static void WaitForEnd()
        {
            while (true)
            {
                if(logs.Count == 0)
                    return;
                Thread.Sleep(100);
            }
        }
    }
}
