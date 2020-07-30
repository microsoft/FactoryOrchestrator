﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FactoryOrchestratorCoreLibrary.Resources {
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
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FactoryOrchestratorCoreLibrary.Resources.Resources", typeof(Resources).Assembly);
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
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks cannot have a retry value!.
        /// </summary>
        internal static string BackgroundRetryException {
            get {
                return ResourceManager.GetString("BackgroundRetryException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks must be ExecutableTask, PowerShellTask, or BatchFileTask!.
        /// </summary>
        internal static string BackgroundTaskTypeException {
            get {
                return ResourceManager.GetString("BackgroundTaskTypeException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks cannot have a timeout value!.
        /// </summary>
        internal static string BackgroundTimeoutException {
            get {
                return ResourceManager.GetString("BackgroundTimeoutException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot perform operation because one or more TaskLists are actively running!.
        /// </summary>
        internal static string FactoryOrchestratorTaskListRunningException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorTaskListRunningException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot perform operation because TaskList $1 is actively running!.
        /// </summary>
        internal static string FactoryOrchestratorTaskListRunningExceptionWithGuid {
            get {
                return ResourceManager.GetString("FactoryOrchestratorTaskListRunningExceptionWithGuid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Guid is not valid!.
        /// </summary>
        internal static string FactoryOrchestratorUnkownGuidException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $1 is not valid!.
        /// </summary>
        internal static string FactoryOrchestratorUnkownGuidExceptionWithGuid {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidExceptionWithGuid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $1 is not a valid $2!.
        /// </summary>
        internal static string FactoryOrchestratorUnkownGuidExceptionWithGuidAndType {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidExceptionWithGuidAndType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to $1 does not exist!.
        /// </summary>
        internal static string FileNotFoundException {
            get {
                return ResourceManager.GetString("FileNotFoundException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not load {filename} as FactoryOrchestratorXML!.
        /// </summary>
        internal static string FOXMLFileLoadException {
            get {
                return ResourceManager.GetString("FOXMLFileLoadException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskRun has an invalid TaskType!.
        /// </summary>
        internal static string InvalidTaskRunTypeException {
            get {
                return ResourceManager.GetString("InvalidTaskRunTypeException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to deserialize an unknown task type!.
        /// </summary>
        internal static string TaskBaseDeserializationException {
            get {
                return ResourceManager.GetString("TaskBaseDeserializationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to serialize an unknown task type!.
        /// </summary>
        internal static string TaskBaseSerializationException {
            get {
                return ResourceManager.GetString("TaskBaseSerializationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskList $1 ($2) with status $3.
        /// </summary>
        internal static string TaskListToString {
            get {
                return ResourceManager.GetString("TaskListToString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to fff.
        /// </summary>
        internal static string test {
            get {
                return ResourceManager.GetString("test", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Device Portal failed with error.
        /// </summary>
        internal static string WDPError {
            get {
                return ResourceManager.GetString("WDPError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Device Portal failed with HTTP error.
        /// </summary>
        internal static string WDPHttpError {
            get {
                return ResourceManager.GetString("WDPHttpError", resourceCulture);
            }
        }
    }
}
