// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.FactoryOrchestrator.Core
{
    /// <summary>
    /// Application install status
    /// </summary>
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
        /// <param name="uri">The endpoint for the install request.</param>
        /// <param name="boundaryString">Unique string used to separate the parts of the multipart form data.</param>
        private static void CreateAppInstallEndpointAndBoundaryString(
            string packageName,
            out Uri uri,
            out string boundaryString)
        {
            CreateAppInstallEndpointAndBoundaryString(packageName, "localhost", out uri, out boundaryString);
        }

        /// <summary>
        /// Builds the application installation Uri and generates a unique boundary string for the multipart form data.
        /// </summary>
        /// <param name="packageName">The name of the application package.</param>
        /// <param name="ipAddress">The ip address of the device to install the app on</param>
        /// <param name="uri">The endpoint for the install request.</param>
        /// <param name="boundaryString">Unique string used to separate the parts of the multipart form data.</param>
        private static void CreateAppInstallEndpointAndBoundaryString(
            string packageName,
            string ipAddress,
            out Uri uri,
            out string boundaryString)
        {
            uri = BuildEndpoint(
                new Uri($"http://{ipAddress}"),
                "api/app/packagemanager/package",
                string.Format("package={0}", packageName));

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
                                    string.Format("{0}?{1}", path, payload) : path;
            return new Uri(baseUri, relativePart);
        }

        private static async Task<ApplicationInstallStatus> GetInstallStatusAsync(string ipAddress = "localhost")
        {
            ApplicationInstallStatus status = ApplicationInstallStatus.None;

            Uri uri = BuildEndpoint(
                new Uri($"http://{ipAddress}"),
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
                            Stream dataStream = null;
                            using (HttpContent content = response.Content)
                            {
                                dataStream = new MemoryStream();

                                await content.CopyToAsync(dataStream).ConfigureAwait(false);

                                // Ensure we point the stream at the origin.
                                dataStream.Position = 0;
                            }

                            if (dataStream != null)
                            {
                                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(HttpErrorResponse));

                                HttpErrorResponse errorResponse = (HttpErrorResponse)serializer.ReadObject(dataStream);

                                if (errorResponse.Success)
                                {
                                    status = ApplicationInstallStatus.Completed;
                                }
                                else
                                {
                                    throw new Exception($"Windows Device Portal failed with error: {errorResponse.Reason}.");
                                }
                            }
                            else
                            {
                                throw new Exception($"Windows Device Portal failed with HTTP error {response.StatusCode}.");
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
                    throw new Exception($"Windows Device Portal failed with HTTP error {response.StatusCode}.");
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
        /// <exception cref="FileNotFoundException">
        /// </exception>
        public static async Task InstallAppWithWDP(string appFilePath, List<string> dependentAppsFilePaths, string certFilePath, string ipAddress = "localhost")
        {
            Uri uri;
            string boundaryString;
            ApplicationInstallStatus status = ApplicationInstallStatus.InProgress;

            if (!File.Exists(appFilePath))
            {
                throw new FileNotFoundException($"{appFilePath} does not exist!");
            }

            if (dependentAppsFilePaths != null)
            {
                foreach (var app in dependentAppsFilePaths)
                {
                    if (!File.Exists(app))
                    {
                        throw new FileNotFoundException($"{app} does not exist!");
                    }
                }
            }

            if (certFilePath != null)
            {
                if (!File.Exists(certFilePath))
                {
                    throw new FileNotFoundException($"{certFilePath} does not exist!");
                }
            }

            CreateAppInstallEndpointAndBoundaryString(Path.GetFileName(appFilePath), ipAddress, out uri, out boundaryString);
            var content = CreateAppInstallContent(appFilePath, dependentAppsFilePaths, certFilePath);
            await WdpHttpClient.PostAsync(uri, content);

            while (status == ApplicationInstallStatus.InProgress)
            {
                await Task.Delay(500);
                status = await GetInstallStatusAsync(ipAddress);
            }
        }
    }

    internal sealed class HttpMultipartFileContent : HttpContent
    {
        /// <summary>
        /// List of items to transfer
        /// </summary>
        private List<string> items = new List<string>();

        /// <summary>
        /// Boundary string
        /// </summary>
        private string boundaryString;

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
            Headers.TryAddWithoutValidation("Content-Type", string.Format("multipart/form-data; boundary={0}", this.boundaryString));
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
            var boundaryLength = Encoding.ASCII.GetBytes(string.Format("--{0}\r\n", this.boundaryString)).Length;
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
            if (info.Extension.ToLower() == ".cer")
            {
                contentType = "application/x-x509-ca-cert";
            }

            return Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{0}\"\r\nContent-Type: {1}\r\n\r\n", info.Name, contentType));
        }
    }

    /// <summary>
    /// Object containing additional error information from
    /// an HTTP response.
    /// </summary>
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
}
