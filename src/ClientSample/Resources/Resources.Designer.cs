// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.FactoryOrchestrator.ClientSample {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.FactoryOrchestrator.ClientSample.Resources.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} &lt;IP Address of DUT&gt; &lt;Folder on this PC with test content and (Optional) FactoryOrchestratorXML files&gt; &lt;Destination folder on DUT&gt; &lt;Destination folder on this PC to save logs&gt;
        ///
        ///OR
        ///
        ///{0} --Discover &lt;seconds to search for devices running Factory Orchestrator&gt;.
        /// </summary>
        public static string ClientSampleUsage {
            get {
                return ResourceManager.GetString("ClientSampleUsage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copied test files, ran tests, and gathered logs in {0}.
        /// </summary>
        public static string Completed {
            get {
                return ResourceManager.GetString("Completed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Connected to {0}..
        /// </summary>
        public static string ConnectedToIp {
            get {
                return ResourceManager.GetString("ConnectedToIp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copied {0} bytes to the device..
        /// </summary>
        public static string CopyComplete {
            get {
                return ResourceManager.GetString("CopyComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Copying latest binaries and TaskLists from {0} to {1} on device....
        /// </summary>
        public static string CopyingFiles {
            get {
                return ResourceManager.GetString("CopyingFiles", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Creating TaskList from files in {0} directory....
        /// </summary>
        public static string CreatingListFromDirectory {
            get {
                return ResourceManager.GetString("CreatingListFromDirectory", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Fatal Exeption!.
        /// </summary>
        public static string Exception {
            get {
                return ResourceManager.GetString("Exception", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Executing TaskList {0}....
        /// </summary>
        public static string ExecutingTaskList {
            get {
                return ResourceManager.GetString("ExecutingTaskList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed!.
        /// </summary>
        public static string Failed {
            get {
                return ResourceManager.GetString("Failed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Found the following devices running Factory Orchestrator:.
        /// </summary>
        public static string FoundServices {
            get {
                return ResourceManager.GetString("FoundServices", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installing {0}. This may take a few minutes....
        /// </summary>
        public static string InstallingApp {
            get {
                return ResourceManager.GetString("InstallingApp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid directory path!.
        /// </summary>
        public static string InvalidDir {
            get {
                return ResourceManager.GetString("InvalidDir", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid IP address!.
        /// </summary>
        public static string InvalidIp {
            get {
                return ResourceManager.GetString("InvalidIp", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid integer!.
        /// </summary>
        public static string InvalidSeconds {
            get {
                return ResourceManager.GetString("InvalidSeconds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading TaskList(s) from FactoryOrchestratorXML file(s)....
        /// </summary>
        public static string LoadingFOXML {
            get {
                return ResourceManager.GetString("LoadingFOXML", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Logs copied to.
        /// </summary>
        public static string LogsCopiedTo {
            get {
                return ResourceManager.GetString("LogsCopiedTo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Looking for apps in subfolders of {0}....
        /// </summary>
        public static string LookingForApps {
            get {
                return ResourceManager.GetString("LookingForApps", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Looking for devices running Factory Orchestrator on your local network using DNS-SD....
        /// </summary>
        public static string LookingForServices {
            get {
                return ResourceManager.GetString("LookingForServices", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No devices running Factory Orchestrator were found on your local network!.
        /// </summary>
        public static string NoDevicesFound {
            get {
                return ResourceManager.GetString("NoDevicesFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Specify IP, test content, dest dir, and log output folder!.
        /// </summary>
        public static string NotEnoughArgs {
            get {
                return ResourceManager.GetString("NotEnoughArgs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Overall result.
        /// </summary>
        public static string OverallResult {
            get {
                return ResourceManager.GetString("OverallResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Passed!.
        /// </summary>
        public static string Passed {
            get {
                return ResourceManager.GetString("Passed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Results for.
        /// </summary>
        public static string ResultsFor {
            get {
                return ResourceManager.GetString("ResultsFor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running for {0} seconds.
        /// </summary>
        public static string RunningFor {
            get {
                return ResourceManager.GetString("RunningFor", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running Tasks:.
        /// </summary>
        public static string RunningTasks {
            get {
                return ResourceManager.GetString("RunningTasks", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskList Execution Summary.
        /// </summary>
        public static string SummaryHeader {
            get {
                return ResourceManager.GetString("SummaryHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskList {0} is finished!.
        /// </summary>
        public static string TaskListComplete {
            get {
                return ResourceManager.GetString("TaskListComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Task took {0} seconds..
        /// </summary>
        public static string TaskTime {
            get {
                return ResourceManager.GetString("TaskTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to More than one app found in {0}!.
        /// </summary>
        public static string TooManyApps {
            get {
                return ResourceManager.GetString("TooManyApps", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to More than one certificate found in {0}!.
        /// </summary>
        public static string TooManyCerts {
            get {
                return ResourceManager.GetString("TooManyCerts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Total size: {0} bytes..
        /// </summary>
        public static string TotalSize {
            get {
                return ResourceManager.GetString("TotalSize", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Total time to load and execute all TaskLists:.
        /// </summary>
        public static string TotalTime {
            get {
                return ResourceManager.GetString("TotalTime", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Usage:.
        /// </summary>
        public static string Usage {
            get {
                return ResourceManager.GetString("Usage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for Factory Orchestrator Service on {0}....
        /// </summary>
        public static string WaitingForService {
            get {
                return ResourceManager.GetString("WaitingForService", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to with exit code.
        /// </summary>
        public static string WithExitCode {
            get {
                return ResourceManager.GetString("WithExitCode", resourceCulture);
            }
        }
    }
}
