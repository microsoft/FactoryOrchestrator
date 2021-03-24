// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
#pragma warning disable IDE0060 // Remove unused parameter

using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.FactoryOrchestrator.Service
{
    public sealed class LogFileProvider : ILoggerProvider
    {
        // Log to file next to the service binary
        private const String LogName = "FactoryOrchestratorService.log";
        private static StreamWriter _logStream = null;
        private static uint _logCount = 0;
        private static readonly object _logLock = new object();

        public ILogger CreateLogger(string categoryName)
        {
            // Synchronize to prevent multiple log streams from being created
            lock (_logLock)
            {
                _logCount++;
                String _logPath = Path.Combine(FOServiceExe.ServiceExeLogFolder, LogName);

                if (_logStream == null)
                {
                    try
                    {
                        Directory.CreateDirectory(FOServiceExe.ServiceExeLogFolder);
                        _logStream = new StreamWriter(_logPath, true);
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine(string.Format(CultureInfo.CurrentCulture, Resources.LogFileCreationFailed, _logPath));
                    }
                }
            }
            return new FileLogger(this, categoryName);
        }

        public void Dispose()
        {
            lock (_logLock)
            {
                _logCount--;
                if (_logCount == 0)
                {
                    if (_logStream != null)
                    {
                        _logStream.Close();
                        _logStream.Dispose();
                        _logStream = null;
                    }
                }
            }
        }

        public static void AddMessage(DateTimeOffset timestamp, string message)
        {
            // Synchronize to ensure messages are printed in order
            lock (_logLock)
            {
                if (_logStream != null)
                {
                    _logStream.WriteLine(message);
                    _logStream.Flush();
                }
            }
        }
    }

    public class FileLogger : ILogger
    {
        readonly LogFileProvider _provider;
        readonly string _category;
        public FileLogger(LogFileProvider provider, string categoryName)
        {
            _provider = provider;
            _category = categoryName;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }
            return true;
        }

        public void Log<TState>(DateTimeOffset timestamp, LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            if (!IsEnabled(logLevel))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.CurrentCulture));
            builder.Append(" [");
            builder.Append(logLevel.ToString());
            builder.Append("] ");
            builder.Append(_category);
            builder.Append(": ");
            builder.AppendLine(formatter(state, exception));

            if (exception != null)
            {
                builder.AppendLine(exception.ToString());
            }

            LogFileProvider.AddMessage(timestamp, builder.ToString());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
        }
    }
}
#pragma warning restore IDE0060 // Remove unused parameter
