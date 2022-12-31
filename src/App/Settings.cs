using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.FactoryOrchestrator;
using Windows.Storage;

namespace Microsoft.FactoryOrchestrator.UWP
{
    /// <summary>
    /// App settings are stored in this class as Properties.
    /// </summary>
    public static class Settings
    {

        /// <summary>
        /// Event when a Property changes.
        /// </summary>
        public static event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Call when a Property changes.
        /// </summary>
        /// <param name="propertyName">Name of the Property that changed.</param>
        private static void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        private static bool AppSettingExists(string setting)
        {
            return ApplicationData.Current.LocalSettings.Values.ContainsKey(setting);
        }

        private static bool SetAppSetting(string setting, object value, bool notify = true)
        {
            if (value == null)
            {
                ApplicationData.Current.LocalSettings.Values.Remove(setting);
                return notify;
            }
            else if (!value.Equals(ApplicationData.Current.LocalSettings.Values[setting]))
            {
                ApplicationData.Current.LocalSettings.Values[setting] = value;
                return notify;
            }

            return false;
        }

        private static T GetAppSetting<T>(string setting, T defaultValue)
        {
            if (AppSettingExists(setting))
            {
                return (T)ApplicationData.Current.LocalSettings.Values[setting];
            }
            else
            {
                // Initialize the setting, but don't call NotifyPropertyChanged
                SetAppSetting(setting, defaultValue, false);
                return defaultValue;
            }

        }

        /// <summary>
        /// User setting. Setting to show ExternalTestResultPage.
        /// </summary>
        public static bool ShowExternalTasks
        {
            get
            {
                return GetAppSetting<bool>(ShowExternalTasksKey, true);
            }
            set
            {
                if (SetAppSetting(ShowExternalTasksKey, value))
                {
                    NotifyPropertyChanged(ShowExternalTasksKey);
                }
            }
        }

        /// <summary>
        /// User setting. Setting to track Task status directly on TaskListExecutionPage.
        /// </summary>
        public static bool TrackExecution
        {
            get
            {
                return GetAppSetting<bool>(TrackExecutionKey, true);
            }
            set
            {
                if (SetAppSetting(TrackExecutionKey, value))
                {
                    NotifyPropertyChanged(TrackExecutionKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered IP address that successfully connected.
        /// </summary>
        public static string LastIp
        {
            get
            {
                return GetAppSetting<string>(LastIpKey, "");
            }
            set
            {
                if (SetAppSetting(LastIpKey, value))
                {
                    NotifyPropertyChanged(LastIpKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered port that successfully connected.
        /// </summary>
        public static string LastPort
        {
            get
            {
                return GetAppSetting<string>(LastPortKey, Core.Constants.DefaultServerPort.ToString(CultureInfo.InvariantCulture));
            }
            set
            {
                if (SetAppSetting(LastPortKey, value))
                {
                    NotifyPropertyChanged(LastPortKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered SSL server that successfully connected.
        /// </summary>
        public static string LastServer
        {
            get
            {
                return GetAppSetting<string>(LastServerKey, Core.Constants.DefaultServerIdentity);
            }
            set
            {
                if (SetAppSetting(LastServerKey, value))
                {
                    NotifyPropertyChanged(LastServerKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered SSL cert hash that successfully connected.
        /// </summary>
        public static string LastHash
        {
            get
            {
                return GetAppSetting<string>(LastHashKey, Core.Constants.DefaultServerCertificateHash);
            }
            set
            {
                if (SetAppSetting(LastHashKey, value))
                {
                    NotifyPropertyChanged(LastHashKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered client SSL cert path that successfully loaded.
        /// </summary>
        public static string LastClientCertPath
        {
            get
            {
                return GetAppSetting<string>(LastClientCertPathKey, "");
            }
            set
            {
                if (SetAppSetting(LastClientCertPathKey, value))
                {
                    NotifyPropertyChanged(LastClientCertPathKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered client SSL cert password that successfully loaded.
        /// </summary>
        public static string LastClientCertPw
        {
            get
            {
                return GetAppSetting<string>(LastClientCertPwKey, "");
            }
            set
            {
                if (SetAppSetting(LastClientCertPwKey, value))
                {
                    NotifyPropertyChanged(LastClientCertPwKey);
                }
            }
        }

        /// <summary>
        /// Internal setting. Last manually entered client SSL cert as JSON string.
        /// </summary>
        public static string LastClientCertSerialized
        {
            get
            {
                return GetAppSetting<string>(LastClientCertSerializedKey, null);
            }
            set
            {
                if (SetAppSetting(LastClientCertSerializedKey, value))
                {
                    NotifyPropertyChanged(LastClientCertSerializedKey);
                }
            }
        }

        public const string ShowExternalTasksKey  = "showExternalTasks";
        public const string LastIpKey  = "lastIp";
        public const string LastHashKey  = "lastHash";
        public const string LastPortKey  = "lastPort";
        public const string LastServerKey  = "lastServer";
        public const string TrackExecutionKey  = "trackExecution";
        public const string LastClientCertPathKey = "lastClientCertPath";
        public const string LastClientCertPwKey = "lastClientCertPw";
        public const string LastClientCertSerializedKey = "lastClientCertX509";
    }
}
