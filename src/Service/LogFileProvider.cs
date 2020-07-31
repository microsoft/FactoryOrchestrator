// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Core;
using System;
using System.IO;
using System.Text;

namespace Microsoft.FactoryOrchestrator.Service
{
    public class LogFileProvider : ILoggerProvider
    {
        // Log to file next to the service binary
        private static readonly String _logName = "FactoryOrchestratorService.log";
        private static StreamWriter _logStream = null;
        private static uint _logCount = 0;
        private static object _logLock = new object();

        public ILogger CreateLogger(string categoryName)
        {
            // Synchronize to prevent multiple log streams from being created
            lock (_logLock)
            {
                _logCount++;
                String _logPath = Path.Combine(FOServiceExe.ServiceLogFolder, _logName);

                if (_logStream == null)
                {
                    try
                    {
                        Directory.CreateDirectory(FOServiceExe.ServiceLogFolder);
                        _logStream = new StreamWriter(_logPath, true);
                    }
                    catch (Exception)
                    {
                        Console.Error.WriteLine(string.Format(Resources.LogFileCreationFailed));
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

        public void AddMessage(DateTimeOffset timestamp, string message)
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
        LogFileProvider _provider;
        string _category;
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
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var builder = new StringBuilder();
            builder.Append(timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
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

            _provider.AddMessage(timestamp, builder.ToString());
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Log(DateTimeOffset.Now, logLevel, eventId, state, exception, formatter);
        }
    }
}