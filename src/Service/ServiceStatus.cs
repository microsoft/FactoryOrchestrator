// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.FactoryOrchestrator.Core;
using Microsoft.Win32;

namespace Microsoft.FactoryOrchestrator.Service
{
    /// <summary>
    /// A file-backed representation of key service state that persists through reboots.
    /// </summary>
    [XmlRootAttribute(ElementName = "FOServiceStatus", IsNullable = false)]
    public sealed class FOServiceStatus
    {
        private FOServiceStatus()
        {
            _firstBootStateLoaded = false;
            _firstBootTaskListsComplete = false;
            _logFolder = "";
        }

        [XmlElement]
        public bool FirstBootStateLoaded
        {
            get => _firstBootStateLoaded;
            set
            {
                if (!Equals(value, _firstBootStateLoaded))
                {
                    _firstBootStateLoaded = value;
                    UpdateServiceStatus();
                }
            }
        }
        private bool _firstBootStateLoaded;

        [XmlElement]
        public bool FirstBootTaskListsComplete
        {
            get => _firstBootTaskListsComplete;
            set
            {
                if (!Equals(value, _firstBootTaskListsComplete))
                {
                    _firstBootTaskListsComplete = value;
                    UpdateServiceStatus();
                }
            }
        }
        private bool _firstBootTaskListsComplete;

        /// <summary>
        /// Directory where the TaskRun logs are stored. This directory path can be changed by the user at runtime, unlike the Service log directory (FOServiceExe.ServiceExeLogFolder). Therefore it is not necessarily related to the Service log directory.
        /// </summary>
        [XmlElement]
        public string LogFolder
        {
            get => _logFolder;
            set
            {
                if (!Equals(value, _logFolder))
                {
                    _logFolder = value;
                    UpdateServiceStatus();
                }
            }
        }
        private string _logFolder;

        [XmlIgnore]
        private string _filename;
        [XmlIgnore]
        private ILogger _logger;

        public static FOServiceStatus CreateOrLoad(string filename, ILogger logger)
        {
            FOServiceStatus status = null;

            if (filename == null)
            {
                throw new ArgumentException(null, nameof(filename));
            }

            if (File.Exists(filename))
            {
                try
                {
                    var deserializer = new XmlSerializer(typeof(FOServiceStatus));
                    using (FileStream fs = new FileStream(filename, FileMode.Open))
                    using (XmlReader reader = XmlReader.Create(fs))
                    {
                        status = (FOServiceStatus)deserializer.Deserialize(reader);
                        status._filename = filename;
                        status._logger = logger;
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.UnableToParseError, filename)} {e.Message}");
                    status = null;
                }
            }

            if (status == null)
            {
                status = new FOServiceStatus();
                status._filename = filename;
                status._logger = logger;
            }

            return status;
        }

        private void UpdateServiceStatus()
        {
            if (_filename != null)
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(FOServiceStatus));

                    using (XmlWriter writer = new XmlTextWriter(_filename, null))
                    {
                        serializer.Serialize(writer, this);
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _filename)} {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// A file or registry backed representation of key service state that does not persist through reboots.
    /// </summary>
    [XmlRootAttribute(ElementName = "FOVolatileServiceStatus", IsNullable = false)]
    public sealed class FOVolatileServiceStatus
    {
        // user tasklists state registry values
        private const string _everyBootCompleteValue = @"EveryBootTaskListsComplete";
        private const string _loopbackEnabledValue = @"UWPLocalLoopbackEnabled";

        private FOVolatileServiceStatus()
        {
            _everyBootTaskListsComplete = false;
            _localLoopbackEnabled = false;
        }

        [XmlElement]
        public bool EveryBootTaskListsComplete
        {
            get => _everyBootTaskListsComplete;
            set
            {
                if (!Equals(value, _everyBootTaskListsComplete))
                {
                    _everyBootTaskListsComplete = value;
                    UpdateServiceStatus(_everyBootCompleteValue);
                }
            }
        }
        private bool _everyBootTaskListsComplete;

        /// <summary>
        /// Windows only. True if UWP LocalLoopback has been enabled for the requested apps.
        /// </summary>
        [XmlIgnore]
        public bool LocalLoopbackEnabled
        {
            get => _localLoopbackEnabled;
            set
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    throw new PlatformNotSupportedException(string.Format(CultureInfo.CurrentCulture, Resources.WindowsOnlyError, "LocalLoopbackEnabled"));
                }

                if (!Equals(value, _localLoopbackEnabled))
                {
                    _localLoopbackEnabled = value;
                    UpdateServiceStatus(_loopbackEnabledValue);
                }
            }
        }
        private bool _localLoopbackEnabled;

        [XmlIgnore]
        private string _filename;
        [XmlIgnore]
        private RegistryKey _volatileKey;
        [XmlIgnore]
        private ILogger _logger;

        public static FOVolatileServiceStatus CreateOrLoad(string filename, RegistryKey volatileKey, ILogger logger)
        {
            FOVolatileServiceStatus status = null;

            // On Unix, FOVolatileServiceStatus is file backed 
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && File.Exists(filename))
            {
                if (filename == null)
                {
                    throw new ArgumentException(null, nameof(filename));
                }

                try
                {
                    var deserializer = new XmlSerializer(typeof(FOVolatileServiceStatus));
                    using (FileStream fs = new FileStream(filename, FileMode.Open))
                    using (XmlReader reader = XmlReader.Create(fs))
                    {
                        status = (FOVolatileServiceStatus)deserializer.Deserialize(reader);
                        status._filename = filename;
                        status._logger = logger;
                    }
                }
                catch (Exception e)
                {
                    logger?.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.UnableToParseError, filename)} {e.Message}");
                    status = null;
                }
            }

            if (status == null)
            {
                status = new FOVolatileServiceStatus();
                status._filename = filename;
                status._logger = logger;
            }

            // On Windows, FOVolatileServiceStatus is registry backed
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (volatileKey == null)
                {
                    throw new ArgumentException(null, nameof(volatileKey));
                }

                status._volatileKey = volatileKey;
                status._everyBootTaskListsComplete = Convert.ToBoolean((int)volatileKey.GetValue(_everyBootCompleteValue, 0));
                status._localLoopbackEnabled = Convert.ToBoolean((int)volatileKey.GetValue(_loopbackEnabledValue, 0));
            }

            return status;
        }

        private void UpdateServiceStatus(string valueName)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    switch (valueName)
                    {
                        case _everyBootCompleteValue:
                            _volatileKey.SetValue(valueName, EveryBootTaskListsComplete, RegistryValueKind.DWord);
                            break;
                        case _loopbackEnabledValue:
                            _volatileKey.SetValue(valueName, LocalLoopbackEnabled, RegistryValueKind.DWord);
                            break;
                        default:
                            throw new ArgumentException(null, nameof(valueName));
                    }
                }
                else if (_filename != null)
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(FOVolatileServiceStatus));

                    using (XmlWriter writer = new XmlTextWriter(_filename, null))
                    {
                        serializer.Serialize(writer, this);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError($"{string.Format(CultureInfo.CurrentCulture, Resources.FileSaveError, _filename)} {e.Message}");
            }
        }
    }
}
