// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// Application install status
    /// </summary>
    /// <exclude/>
    public enum ApplicationInstallStatus
    {
        /// <summary>
        /// No install status
        /// </summary>
        None,

        /// <summary>
        /// Installation is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Installation is completed
        /// </summary>
        Completed,

        /// <summary>
        /// Installation failed
        /// </summary>
        Failed
    }

    /// <summary>
    /// Install phase
    /// </summary>
    /// <exclude/>
    public enum ApplicationInstallPhase
    {
        /// <summary>
        /// Idle phase
        /// </summary>
        Idle,

        /// <summary>
        /// Uninstalling the previous version
        /// </summary>
        UninstallingPreviousVersion,

        /// <summary>
        /// Copying the package file
        /// </summary>
        CopyingFile,

        /// <summary>
        /// Installing the package
        /// </summary>
        Installing
    }

    /// <summary>
    /// Helper APIs for Windows Device Portal.
    /// </summary>
    /// <exclude/>
    public static class WDPHelpers
    {
        /// <summary>
        /// The Windows Device Portal HTTP client.
        /// </summary>
        public static readonly HttpClient WdpHttpClient = new HttpClient();

        private static HttpMultipartFileContent CreateAppInstallContent(string appFilePath, List<string> dependentAppsFilePaths, string certFilePath)
        {
            var content = new HttpMultipartFileContent();
            content.Add(appFilePath);

            if (dependentAppsFilePaths != null)
            {
                content.AddRange(dependentAppsFilePaths);
            }

            if (certFilePath != null)
            {
                content.Add(certFilePath);
            }

            return content;
        }

        /// <summary>
        /// Builds the application installation Uri and generates a unique boundary string for the multipart form data.
        /// </summary>
        /// <param name="packageName">The name of the application package.</param>
        /// <param name="ipAddress">The ip address of the device to install the app on</param>
        /// <param name="port">The port for WDP on the target device.</param>
        /// <param name="uri">The endpoint for the install request.</param>
        /// <param name="boundaryString">Unique string used to separate the parts of the multipart form data.</param>
        private static void CreateAppInstallEndpointAndBoundaryString(
            string packageName,
            string ipAddress,
            int port,
            out Uri uri,
            out string boundaryString)
        {
            uri = BuildEndpoint(
                new Uri($"http://{ipAddress}:{port}"),
                "api/app/packagemanager/package",
                $"package={packageName}");

            boundaryString = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Constructs a fully formed REST API endpoint uri.
        /// </summary>
        /// <param name="baseUri">The base uri (typically, just scheme and authority).</param>
        /// <param name="path">The path to the REST API method (ex: api/control/restart).</param>
        /// <param name="payload">Parameterized data required by the REST API.</param>
        /// <returns>Uri object containing the complete path and query string required to issue the REST API call.</returns>
        private static Uri BuildEndpoint(
            Uri baseUri,
            string path,
            string payload = null)
        {
            string relativePart = !string.IsNullOrWhiteSpace(payload) ?
                                    $"{path}?{payload}" : path;
            return new Uri(baseUri, relativePart);
        }

        private static async Task<ApplicationInstallStatus> GetInstallStatusAsync(string ipAddress = "localhost", int port = 80)
        {
            ApplicationInstallStatus status = ApplicationInstallStatus.None;

            Uri uri = BuildEndpoint(
                new Uri($"http://{ipAddress}:{port}"),
                "api/app/packagemanager/state");

            using (HttpResponseMessage response = await WdpHttpClient.GetAsync(uri).ConfigureAwait(false))
            {
                if (response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        // Status code: 200
                        if (response.Content == null)
                        {
                            status = ApplicationInstallStatus.Completed;
                        }
                        else
                        {
                            // If we have a response body, it's possible this was an error
                            // (even though we got an HTTP 200).


                            using (Stream dataStream = new MemoryStream())
                            {
                                using (HttpContent content = response.Content)
                                {
                                    await content.CopyToAsync(dataStream).ConfigureAwait(false);

                                    // Ensure we point the stream at the origin.
                                    dataStream.Position = 0;
                                }

                                if (dataStream.Length > 0)
                                {
                                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(HttpErrorResponse));

                                    HttpErrorResponse errorResponse = (HttpErrorResponse)serializer.ReadObject(dataStream);

                                    if (errorResponse.Success)
                                    {
                                        status = ApplicationInstallStatus.Completed;
                                    }
                                    else
                                    {
                                        throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.WDPError, errorResponse.Reason));
                                    }
                                }
                                else
                                {
                                    throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.WDPHttpError, response.StatusCode));
                                }
                            }
                        }
                    }
                    else if (response.StatusCode == HttpStatusCode.NoContent)
                    {
                        // Status code: 204
                        status = ApplicationInstallStatus.InProgress;
                    }
                }
                else
                {
                    throw new Exception(string.Format(CultureInfo.CurrentCulture, Resources.WDPHttpError, response.StatusCode));
                }
            }

            return status;
        }

        /// <summary>
        /// Installs an app package application with Windows Device Portal.
        /// </summary>
        /// <param name="appFilePath">The app package file path.</param>
        /// <param name="dependentAppsFilePaths">The dependent app packages file paths.</param>
        /// <param name="certFilePath">The certificate file path.</param>
        /// <param name="ipAddress">The ip address of the device to install the app on.</param>
        /// <param name="port">The port for WDP on the target device.</param>
        /// <exception cref="FileNotFoundException">
        /// </exception>
        public static async Task InstallAppWithWDP(string appFilePath, List<string> dependentAppsFilePaths, string certFilePath, string ipAddress = "localhost", int port = 80)
        {
            ApplicationInstallStatus status = ApplicationInstallStatus.InProgress;

            if (!File.Exists(appFilePath))
            {
                throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, appFilePath));
            }

            if (dependentAppsFilePaths != null)
            {
                foreach (var app in dependentAppsFilePaths)
                {
                    if (!File.Exists(app))
                    {
                        throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, app));
                    }
                }
            }

            if (certFilePath != null)
            {
                if (!File.Exists(certFilePath))
                {
                    throw new FileNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.FileNotFoundException, certFilePath));
                }
            }

            CreateAppInstallEndpointAndBoundaryString(Path.GetFileName(appFilePath), ipAddress, port, out var uri, out _);
            using (var content = CreateAppInstallContent(appFilePath, dependentAppsFilePaths, certFilePath))
            {
                await WdpHttpClient.PostAsync(uri, content);
            }

            while (status == ApplicationInstallStatus.InProgress)
            {
                await Task.Delay(500);
                status = await GetInstallStatusAsync(ipAddress, port);
            }
        }


        /// <summary>
        /// Closes a running app package application with Windows Device Portal.
        /// </summary>
        /// <param name="app">The app package to exit .</param>
        /// <param name="ipAddress">The ip address of the device to exit the app on.</param>
        /// <param name="port">The port for WDP on the target device.</param>
        /// <exception cref="ArgumentException">
        /// </exception>
        public static async Task CloseAppWithWDP(string app, string ipAddress = "localhost", int port = 80)
        {
            if (string.IsNullOrWhiteSpace(app))
            {
                throw new ArgumentException(Resources.WDPError, nameof(app));
            }

            var uri = BuildEndpoint(new Uri($"http://{ipAddress}:{port}"),
                                "api/taskmanager/app",
                                $"package={Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(app))}");

            await WdpHttpClient.DeleteAsync(uri);
        }

        /// <summary>
        /// Gets the collection of applications installed on the device.
        /// </summary>
        /// <param name="ipAddress">The ip address of the device to query.</param>
        /// <param name="port">The port for WDP on the target device.</param>
        /// <returns>AppPackages object containing the list of installed application packages.</returns>
        public static async Task<AppPackages> GetInstalledAppPackagesAsync(string ipAddress = "localhost", int port = 80)
        {
            Uri uri = BuildEndpoint(
                new Uri($"http://{ipAddress}:{port}"),
                "api/app/packagemanager/packages");

            var resp = await WdpHttpClient.GetAsync(uri).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(Resources.WDPNotRunningError);
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(AppPackages));
            return (AppPackages)serializer.ReadObject(await resp.Content.ReadAsStreamAsync());
        }

        /// <summary>
        /// Gets the collection of running processes on the device.
        /// </summary>
        /// <param name="ipAddress">The ip address of the device to query.</param>
        /// <param name="port">The port for WDP on the target device.</param>
        /// <returns>RunningProcesses object containing the list of running processses.</returns>
        public static async Task<RunningProcesses> GetRunningProcessesAsync(string ipAddress = "localhost", int port = 80)
        {
            Uri uri = BuildEndpoint(
                new Uri($"http://{ipAddress}:{port}"),
                "api/resourcemanager/processes");

            var resp = await WdpHttpClient.GetAsync(uri).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(Resources.WDPNotRunningError);
            }

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(RunningProcesses));
            return (RunningProcesses)serializer.ReadObject(await resp.Content.ReadAsStreamAsync());
        }
    }


    /// <exclude/>
    internal sealed class HttpMultipartFileContent : HttpContent
    {
        /// <summary>
        /// List of items to transfer
        /// </summary>
        private readonly List<string> items = new List<string>();

        /// <summary>
        /// Boundary string
        /// </summary>
        private readonly string boundaryString;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMultipartFileContent" /> class.
        /// </summary>
        public HttpMultipartFileContent() : this(Guid.NewGuid().ToString())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpMultipartFileContent" /> class.
        /// </summary>
        /// <param name="boundary">The boundary string for file content.</param>
        public HttpMultipartFileContent(string boundary)
        {
            this.boundaryString = boundary;
            Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={this.boundaryString}");
        }

        /// <summary>
        /// Adds a file to the list of items to transfer
        /// </summary>
        /// <param name="filename">The name of the file to add</param>
        public void Add(string filename)
        {
            if (filename != null)
            {
                this.items.Add(filename);
            }
        }

        /// <summary>
        /// Adds a range of files to the list of items to transfer
        /// </summary>
        /// <param name="filenames">List of files to add</param>
        public void AddRange(IEnumerable<string> filenames)
        {
            if (filenames != null)
            {
                this.items.AddRange(filenames);
            }
        }

        /// <summary>
        /// Serializes the stream.
        /// </summary>
        /// <param name="outStream">Serialized Stream</param>
        /// <param name="context">The Transport Context</param>
        /// <returns>Task tracking progress</returns>
        protected override async Task SerializeToStreamAsync(Stream outStream, TransportContext context)
        {
            var boundary = Encoding.ASCII.GetBytes($"--{boundaryString}\r\n");
            var newline = Encoding.ASCII.GetBytes("\r\n");
            foreach (var item in this.items)
            {
                outStream.Write(boundary, 0, boundary.Length);
                var headerdata = GetFileHeader(new FileInfo(item));
                outStream.Write(headerdata, 0, headerdata.Length);

                using (var file = File.OpenRead(item))
                {
                    await file.CopyToAsync(outStream);
                }

                outStream.Write(newline, 0, newline.Length);
                await outStream.FlushAsync();
            }

            // Close the installation request data.
            boundary = Encoding.ASCII.GetBytes($"--{boundaryString}--\r\n");
            outStream.Write(boundary, 0, boundary.Length);
            await outStream.FlushAsync();
        }

        /// <summary>
        /// Computes required length for the transfer.
        /// </summary>
        /// <param name="length">The computed length value</param>
        /// <returns>Whether or not the length was successfully computed</returns>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            var boundaryLength = Encoding.ASCII.GetBytes($"--{this.boundaryString}\r\n").Length;
            foreach (var item in this.items)
            {
                var headerdata = GetFileHeader(new FileInfo(item));
                length += boundaryLength + headerdata.Length + new FileInfo(item).Length + 2;
            }

            length += boundaryLength + 2;
            return true;
        }

        /// <summary>
        /// Gets the file header for the transfer
        /// </summary>
        /// <param name="info">Information about the file</param>
        /// <returns>A byte array with the file header information</returns>
        private static byte[] GetFileHeader(FileInfo info)
        {
            string contentType = "application/octet-stream";
            if (info.Extension.ToUpperInvariant() == ".CER")
            {
                contentType = "application/x-x509-ca-cert";
            }

            return Encoding.ASCII.GetBytes($"Content-Disposition: form-data; name=\"{info.Name}\"; filename=\"{info.Name}\"\r\nContent-Type: {contentType}\r\n\r\n");
        }
    }

    /// <summary>
    /// Object containing additional error information from
    /// an HTTP response.
    /// </summary>
    /// <exclude/>
    [DataContract]
    public class HttpErrorResponse
    {
        /// <summary>
        /// Gets the ErrorCode
        /// </summary>
        [DataMember(Name = "ErrorCode")]
        public int ErrorCode { get; private set; }

        /// <summary>
        /// Gets the Code (used by some endpoints instead of ErrorCode).
        /// </summary>
        [DataMember(Name = "Code")]
        public int Code { get; private set; }

        /// <summary>
        /// Gets the ErrorMessage
        /// </summary>
        [DataMember(Name = "ErrorMessage")]
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the Reason (used by some endpoints instead of ErrorMessage).
        /// </summary>
        [DataMember(Name = "Reason")]
        public string Reason { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the operation succeeded. For an error this should generally be false if present.
        /// </summary>
        [DataMember(Name = "Success")]
        public bool Success { get; private set; }
    }

    /// <summary>
    /// Object representing a list of Application Packages
    /// </summary>
    [DataContract]
    public class AppPackages
    {
        /// <summary>
        /// Gets a list of the packages
        /// </summary>
        [DataMember(Name = "InstalledPackages")]
        public List<PackageInfo> Packages { get; private set; }

        /// <summary>
        /// Presents a user readable representation of a list of AppPackages
        /// </summary>
        /// <returns>User readable list of AppPackages.</returns>
        public override string ToString()
        {
            string output = "Packages:\n";
            foreach (PackageInfo package in this.Packages)
            {
                output += package;
            }

            return output;
        }
    }

    /// <summary>
    /// object representing the package information
    /// </summary>
    [DataContract]
    public class PackageInfo
    {
        /// <summary>
        /// Gets package name
        /// </summary>
        [DataMember(Name = "Name")]
        public string Name { get; private set; }

        /// <summary>
        /// Gets package family name
        /// </summary>
        [DataMember(Name = "PackageFamilyName")]
        public string FamilyName { get; private set; }

        /// <summary>
        /// Gets package full name
        /// </summary>
        [DataMember(Name = "PackageFullName")]
        public string FullName { get; private set; }

        /// <summary>
        /// Gets package relative Id
        /// </summary>
        [DataMember(Name = "PackageRelativeId")]
        public string AppId { get; private set; }

        /// <summary>
        /// Gets package publisher
        /// </summary>
        [DataMember(Name = "Publisher")]
        public string Publisher { get; private set; }

        /// <summary>
        /// Gets package version
        /// </summary>
        [DataMember(Name = "Version")]
        public PackageVersion Version { get; private set; }

        /// <summary>
        /// Gets package origin, a measure of how the app was installed. 
        /// PackageOrigin_Unknown            = 0,
        /// PackageOrigin_Unsigned           = 1,
        /// PackageOrigin_Inbox              = 2,
        /// PackageOrigin_Store              = 3,
        /// PackageOrigin_DeveloperUnsigned  = 4,
        /// PackageOrigin_DeveloperSigned    = 5,
        /// PackageOrigin_LineOfBusiness     = 6
        /// </summary>
        [DataMember(Name = "PackageOrigin")]
        public int PackageOrigin { get; private set; }

        /// <summary>
        /// Helper method to determine if the app was sideloaded and therefore can be used with e.g. GetFolderContentsAsync
        /// </summary>
        /// <returns> True if the package is sideloaded. </returns>
        public bool IsSideloaded()
        {
            return this.PackageOrigin == 4 || this.PackageOrigin == 5;
        }

        /// <summary>
        /// Get a string representation of the package
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "\t{0}\n\t\t{1}\n", this.FullName, this.AppId);
        }
    }

    /// <summary>
    /// Object representing a package version
    /// </summary>
    [DataContract]
    public class PackageVersion
    {
        /// <summary>
        ///  Gets version build
        /// </summary>
        [DataMember(Name = "Build")]
        public int Build { get; private set; }

        /// <summary>
        /// Gets package Major number
        /// </summary>
        [DataMember(Name = "Major")]
        public int Major { get; private set; }

        /// <summary>
        /// Gets package minor number
        /// </summary>
        [DataMember(Name = "Minor")]
        public int Minor { get; private set; }

        /// <summary>
        /// Gets package revision
        /// </summary>
        [DataMember(Name = "Revision")]
        public int Revision { get; private set; }

        /// <summary>
        /// Gets package version
        /// </summary>
        public Version Version
        {
            get { return new Version(this.Major, this.Minor, this.Build, this.Revision); }
        }

        /// <summary>
        /// Get a string representation of a version
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return Version.ToString();
        }
    }

    /// <summary>
    /// Process Info.  Contains app information if the process is an app. 
    /// </summary>
    /// <exclude/>
    [DataContract]
    public class DeviceProcessInfo
    {
        /// <summary>
        /// Gets the app name. Only present if the process is an app. 
        /// </summary>
        [DataMember(Name = "AppName")]
        public string AppName { get; private set; }

        /// <summary>
        /// Gets CPU Usage as a percentage of available CPU resources (0-100)
        /// </summary>
        [DataMember(Name = "CPUUsage")]
        public float CpuUsage { get; private set; }

        /// <summary>
        /// Gets the image name
        /// </summary>
        [DataMember(Name = "ImageName")]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the process id (pid)
        /// </summary>
        [DataMember(Name = "ProcessId")]
        public uint ProcessId { get; private set; }

        /// <summary>
        /// Gets the user the process is running as. 
        /// </summary>
        [DataMember(Name = "UserName")]
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the package full name.  Only present if the process is an app. 
        /// </summary>
        [DataMember(Name = "PackageFullName")]
        public string PackageFullName { get; private set; }

        /// <summary>
        /// Gets the Page file usage info, in bytes
        /// </summary>
        [DataMember(Name = "PageFileUsage")]
        public ulong PageFile { get; private set; }

        /// <summary>
        /// Gets the working set size, in bytes
        /// </summary>
        [DataMember(Name = "WorkingSetSize")]
        public ulong WorkingSet { get; private set; }

        /// <summary>
        /// Gets package working set, in bytes
        /// </summary>
        [DataMember(Name = "PrivateWorkingSet")]
        public ulong PrivateWorkingSet { get; private set; }

        /// <summary>
        /// Gets session id
        /// </summary>
        [DataMember(Name = "SessionId")]
        public uint SessionId { get; private set; }

        /// <summary>
        /// Gets total commit, in bytes
        /// </summary>
        [DataMember(Name = "TotalCommit")]
        public ulong TotalCommit { get; private set; }

        /// <summary>
        /// Gets virtual size, in bytes
        /// </summary>
        [DataMember(Name = "VirtualSize")]
        public ulong VirtualSize { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the app is running 
        /// (versus suspended). Only present if the process is an app.
        /// </summary>
        [DataMember(Name = "IsRunning")]
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets publisher. Only present if the process is an app.
        /// </summary>
        [DataMember(Name = "Publisher")]
        public string Publisher { get; private set; }

        /// <summary>
        /// Gets version. Only present if the process is an app.
        /// </summary>
        [DataMember(Name = "Version")]
        public AppVersion Version { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the package is a XAP 
        /// package. Only present if the process is an app.
        /// </summary>
        [DataMember(Name = "IsXAP")]
        public bool IsXAP { get; private set; }

        /// <summary>
        /// String representation of a process
        /// </summary>
        /// <returns>String representation</returns>
        public override string ToString()
        {
            return string.Format("{0} ({1})", this.AppName, this.Name);
        }
    }

    /// <summary>
    /// Object representing the app version.  Only present if the process is an app. 
    /// </summary>
    /// <exclude/>
    [DataContract]
    public class AppVersion
    {
        /// <summary>
        /// Gets the major version number
        /// </summary>
        [DataMember(Name = "Major")]
        public uint Major { get; private set; }

        /// <summary>
        /// Gets the minor version number
        /// </summary>
        [DataMember(Name = "Minor")]
        public uint Minor { get; private set; }

        /// <summary>
        /// Gets the build number
        /// </summary>
        [DataMember(Name = "Build")]
        public uint Build { get; private set; }

        /// <summary>
        /// Gets the revision number
        /// </summary>
        [DataMember(Name = "Revision")]
        public uint Revision { get; private set; }
    }

    /// <summary>
    /// Running processes
    /// </summary>
    /// <exclude/>
    [DataContract]
    public class RunningProcesses
    {
        /// <summary>
        /// Gets list of running processes.
        /// </summary>
        [DataMember(Name = "Processes")]
        public List<DeviceProcessInfo> Processes { get; private set; }

        /// <summary>
        /// Checks to see if this process Id is in the list of processes
        /// </summary>
        /// <param name="processId">Process to look for</param>
        /// <returns>whether the process id was found</returns>
        public bool Contains(int processId)
        {
            bool found = false;

            foreach (DeviceProcessInfo pi in this.Processes)
            {
                if (pi.ProcessId == processId)
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// Checks for a given package name
        /// </summary>
        /// <param name="packageName">Name of the package to look for</param>
        /// <param name="caseSensitive">Whether we should be case sensitive in our search</param>
        /// <returns>Whether the package was found</returns>
        public bool Contains(string packageName, bool caseSensitive = true)
        {
            bool found = false;

            foreach (DeviceProcessInfo pi in this.Processes)
            {
                if (string.Compare(
                        pi.PackageFullName,
                        packageName,
                        caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase) == 0)
                {
                    found = true;
                    break;
                }
            }

            return found;
        }
    }
}
