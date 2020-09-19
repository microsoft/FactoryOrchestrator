﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.FactoryOrchestrator.Core {
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
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Terminated app: {0}.
        /// </summary>
        public static string AppTerminated {
            get {
                return ResourceManager.GetString("AppTerminated", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Attempting to run the Task in the container....
        /// </summary>
        public static string AttemptingContainerTaskRun {
            get {
                return ResourceManager.GetString("AttemptingContainerTaskRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} found, attempting to load....
        /// </summary>
        public static string AttemptingFileLoad {
            get {
                return ResourceManager.GetString("AttemptingFileLoad", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to AUMID is not valid!.
        /// </summary>
        public static string AUMIDNotValidError {
            get {
                return ResourceManager.GetString("AUMIDNotValidError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks cannot have a retry value!.
        /// </summary>
        public static string BackgroundRetryException {
            get {
                return ResourceManager.GetString("BackgroundRetryException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks must be ExecutableTask, PowerShellTask, or BatchFileTask!.
        /// </summary>
        public static string BackgroundTaskTypeException {
            get {
                return ResourceManager.GetString("BackgroundTaskTypeException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to BackgroundTasks cannot have a timeout value!.
        /// </summary>
        public static string BackgroundTimeoutException {
            get {
                return ResourceManager.GetString("BackgroundTimeoutException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service boot tasks are still executing! Wait for them to finish or call AbortAll()..
        /// </summary>
        public static string BootTasksExecutingError {
            get {
                return ResourceManager.GetString("BootTasksExecutingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service is done executing boot tasks..
        /// </summary>
        public static string BootTasksFinished {
            get {
                return ResourceManager.GetString("BootTasksFinished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service is executing boot tasks....
        /// </summary>
        public static string BootTasksStarted {
            get {
                return ResourceManager.GetString("BootTasksStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checking for {0}....
        /// </summary>
        public static string CheckingForFile {
            get {
                return ResourceManager.GetString("CheckingForFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start connection first!.
        /// </summary>
        public static string ClientNotConnected {
            get {
                return ResourceManager.GetString("ClientNotConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service is connected to a container running a compatible version of Factory Orchestrator Service..
        /// </summary>
        public static string ContainerConnected {
            get {
                return ResourceManager.GetString("ContainerConnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service is disconnected from a container running a compatible version of Factory Orchestrator Service..
        /// </summary>
        public static string ContainerDisconnected {
            get {
                return ResourceManager.GetString("ContainerDisconnected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to get file from container!.
        /// </summary>
        public static string ContainerFileGetFailed {
            get {
                return ResourceManager.GetString("ContainerFileGetFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to send file to container!.
        /// </summary>
        public static string ContainerFileSendFailed {
            get {
                return ResourceManager.GetString("ContainerFileSendFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to complete TraskRun in container.
        /// </summary>
        public static string ContainerTaskRunFailed {
            get {
                return ResourceManager.GetString("ContainerTaskRunFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not create {0} directory!.
        /// </summary>
        public static string CreateDirectoryFailed {
            get {
                return ResourceManager.GetString("CreateDirectoryFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to delete file or folder {0} in container!.
        /// </summary>
        public static string DeleteContainerPathFailed {
            get {
                return ResourceManager.GetString("DeleteContainerPathFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to External TaskRun {0} received a {1} result and is finished..
        /// </summary>
        public static string DoneWaitingForExternalTaskRun {
            get {
                return ResourceManager.GetString("DoneWaitingForExternalTaskRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicate Guid(s) {0}in FactoryOrchestratorXML!.
        /// </summary>
        public static string DuplicateGuidInXml {
            get {
                return ResourceManager.GetString("DuplicateGuidInXml", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskRun {0} could not be loaded from file as it already exists!.
        /// </summary>
        public static string DuplicateTaskRunGuid {
            get {
                return ResourceManager.GetString("DuplicateTaskRunGuid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Enabling UWP local loopback for {0}....
        /// </summary>
        public static string EnablingLoopback {
            get {
                return ResourceManager.GetString("EnablingLoopback", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to enable UWP local loopback for {0}! You may not be able to communicate with the Factory Orchestrator Service from {0}..
        /// </summary>
        public static string EnablingLoopbackFailed {
            get {
                return ResourceManager.GetString("EnablingLoopbackFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to End Container Process Output.
        /// </summary>
        public static string EndContainerOutput {
            get {
                return ResourceManager.GetString("EndContainerOutput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A FactoryOrchestratorClient completed the TaskRun with Status: {0}, Exit code: {1}.
        /// </summary>
        public static string EndWaitingForExternalResult {
            get {
                return ResourceManager.GetString("EndWaitingForExternalResult", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Every boot TaskLists already complete..
        /// </summary>
        public static string EveryBootAlreadyComplete {
            get {
                return ResourceManager.GetString("EveryBootAlreadyComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Every boot TaskLists complete..
        /// </summary>
        public static string EveryBootComplete {
            get {
                return ResourceManager.GetString("EveryBootComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to complete every boot TaskLists!.
        /// </summary>
        public static string EveryBootFailed {
            get {
                return ResourceManager.GetString("EveryBootFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running every boot TaskList {0}....
        /// </summary>
        public static string EveryBootRunningTaskList {
            get {
                return ResourceManager.GetString("EveryBootRunningTaskList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for every boot TaskLists to complete... (Mark tests as BackgroundTasks if you do not expect them to ever exit.).
        /// </summary>
        public static string EveryBootWaiting {
            get {
                return ResourceManager.GetString("EveryBootWaiting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to communicate with Factory Orchestrator Service on {0}.
        /// </summary>
        public static string FactoryOrchestratorConnectionException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorConnectionException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot perform operation because one or more TaskLists are actively running!.
        /// </summary>
        public static string FactoryOrchestratorTaskListRunningException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorTaskListRunningException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Cannot perform operation because TaskList {0} is actively running!.
        /// </summary>
        public static string FactoryOrchestratorTaskListRunningExceptionWithGuid {
            get {
                return ResourceManager.GetString("FactoryOrchestratorTaskListRunningExceptionWithGuid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Guid is not valid!.
        /// </summary>
        public static string FactoryOrchestratorUnkownGuidException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not valid!.
        /// </summary>
        public static string FactoryOrchestratorUnkownGuidExceptionWithGuid {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidExceptionWithGuid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid {1}!.
        /// </summary>
        public static string FactoryOrchestratorUnkownGuidExceptionWithGuidAndType {
            get {
                return ResourceManager.GetString("FactoryOrchestratorUnkownGuidExceptionWithGuidAndType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service on {ip} has version {serviceVersion} which is incompatable with FactoryOrchestratorClient version {clientVersion}! Use Connect(true) or TryConnect(true) to ignore this error when connecting..
        /// </summary>
        public static string FactoryOrchestratorVersionMismatchException {
            get {
                return ResourceManager.GetString("FactoryOrchestratorVersionMismatchException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Successfully loaded {0}..
        /// </summary>
        public static string FileLoadSucceeded {
            get {
                return ResourceManager.GetString("FileLoadSucceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} not found..
        /// </summary>
        public static string FileNotFound {
            get {
                return ResourceManager.GetString("FileNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} does not exist!.
        /// </summary>
        public static string FileNotFoundException {
            get {
                return ResourceManager.GetString("FileNotFoundException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Target file {0} could not be saved!.
        /// </summary>
        public static string FileSaveError {
            get {
                return ResourceManager.GetString("FileSaveError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finish.
        /// </summary>
        public static string Finish {
            get {
                return ResourceManager.GetString("Finish", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to First boot TaskList already complete!.
        /// </summary>
        public static string FirstBootAlreadyComplete {
            get {
                return ResourceManager.GetString("FirstBootAlreadyComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to First boot TaskLists complete..
        /// </summary>
        public static string FirstBootComplete {
            get {
                return ResourceManager.GetString("FirstBootComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to complete first boot TaskLists!.
        /// </summary>
        public static string FirstBootFailed {
            get {
                return ResourceManager.GetString("FirstBootFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Running first boot TaskList {0}....
        /// </summary>
        public static string FirstBootRunningTaskList {
            get {
                return ResourceManager.GetString("FirstBootRunningTaskList", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for first boot TaskLists to complete... (Mark tests as BackgroundTasks if you do not expect them to ever exit.).
        /// </summary>
        public static string FirstBootWaiting {
            get {
                return ResourceManager.GetString("FirstBootWaiting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not load {0} as FactoryOrchestratorXML!.
        /// </summary>
        public static string FOXMLFileLoadException {
            get {
                return ResourceManager.GetString("FOXMLFileLoadException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is not a valid file or folder!.
        /// </summary>
        public static string InvalidPathError {
            get {
                return ResourceManager.GetString("InvalidPathError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskRun has an invalid TaskType!.
        /// </summary>
        public static string InvalidTaskRunTypeException {
            get {
                return ResourceManager.GetString("InvalidTaskRunTypeException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WARNING: Log File {0} could not be created.
        /// </summary>
        public static string LogFileCreationFailed {
            get {
                return ResourceManager.GetString("LogFileCreationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not move log folder!.
        /// </summary>
        public static string LogFolderMoveFailed {
            get {
                return ResourceManager.GetString("LogFolderMoveFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to move file or folder {0} to {1} in container!.
        /// </summary>
        public static string MoveContainerPathFailed {
            get {
                return ResourceManager.GetString("MoveContainerPathFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service network access is disabled..
        /// </summary>
        public static string NetworkAccessDisabled {
            get {
                return ResourceManager.GetString("NetworkAccessDisabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service network access is enabled..
        /// </summary>
        public static string NetworkAccessEnabled {
            get {
                return ResourceManager.GetString("NetworkAccessEnabled", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No valid container Id found!.
        /// </summary>
        public static string NoContainerIdFound {
            get {
                return ResourceManager.GetString("NoContainerIdFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No valid container IP address found!.
        /// </summary>
        public static string NoContainerIpFound {
            get {
                return ResourceManager.GetString("NoContainerIpFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not find method with name {0}.
        /// </summary>
        public static string NoMethodFound {
            get {
                return ResourceManager.GetString("NoMethodFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No TaskLists to save!.
        /// </summary>
        public static string NoTaskListsException {
            get {
                return ResourceManager.GetString("NoTaskListsException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not open NonMutable registry key!.
        /// </summary>
        public static string OpenNonMutableKeyFailed {
            get {
                return ResourceManager.GetString("OpenNonMutableKeyFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not open Volatile registry key!.
        /// </summary>
        public static string OpenVolatileKeyFailed {
            get {
                return ResourceManager.GetString("OpenVolatileKeyFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process exited..
        /// </summary>
        public static string ProcessExited {
            get {
                return ResourceManager.GetString("ProcessExited", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Process failed to start!.
        /// </summary>
        public static string ProcessStartError {
            get {
                return ResourceManager.GetString("ProcessStartError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service is ready to communicate with client(s)..
        /// </summary>
        public static string ReadyToCommunicate {
            get {
                return ResourceManager.GetString("ReadyToCommunicate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is a Win32 GUI program. Redirecting to run via RunAsExplorerUser....
        /// </summary>
        public static string RedirectingToRunAsExplorerUser {
            get {
                return ResourceManager.GetString("RedirectingToRunAsExplorerUser", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is a UWP app targeted to run in the container. Redirecting to run in the container via RunAsExplorerUser....
        /// </summary>
        public static string RedirectingUWPToRunAs {
            get {
                return ResourceManager.GetString("RedirectingUWPToRunAs", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to WARNING: {0} is a Win32 GUI program. It may not run properly as SYSTEM.
        /// </summary>
        public static string RunningGuiAsSystemWarning {
            get {
                return ResourceManager.GetString("RunningGuiAsSystemWarning", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FactoryOrchestratorService already created! Only one instance allowed..
        /// </summary>
        public static string ServiceAlreadyCreatedError {
            get {
                return ResourceManager.GetString("ServiceAlreadyCreatedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service {0} errored with exception.
        /// </summary>
        public static string ServiceErrored {
            get {
                return ResourceManager.GetString("ServiceErrored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} exited with {1}!.
        /// </summary>
        public static string ServiceProcessExitedWithError {
            get {
                return ResourceManager.GetString("ServiceProcessExitedWithError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} never started!.
        /// </summary>
        public static string ServiceProcessStartFailed {
            get {
                return ResourceManager.GetString("ServiceProcessStartFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} did not exit before {1}ms!.
        /// </summary>
        public static string ServiceProcessTimedOut {
            get {
                return ResourceManager.GetString("ServiceProcessTimedOut", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service {0} started.
        /// </summary>
        public static string ServiceStarted {
            get {
                return ResourceManager.GetString("ServiceStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service {0} starting.
        /// </summary>
        public static string ServiceStarting {
            get {
                return ResourceManager.GetString("ServiceStarting", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service {0} stopped.
        /// </summary>
        public static string ServiceStopped {
            get {
                return ResourceManager.GetString("ServiceStopped", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Factory Orchestrator Service stopped..
        /// </summary>
        public static string ServiceStoppedWithName {
            get {
                return ResourceManager.GetString("ServiceStoppedWithName", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Service {0} stopping.
        /// </summary>
        public static string ServiceStopping {
            get {
                return ResourceManager.GetString("ServiceStopping", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to save TaskLists on service stop!.
        /// </summary>
        public static string ServiceStopSaveError {
            get {
                return ResourceManager.GetString("ServiceStopSaveError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Source directory does not exist or could not be found:.
        /// </summary>
        public static string SourceDirectoryNotFound {
            get {
                return ResourceManager.GetString("SourceDirectoryNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start.
        /// </summary>
        public static string Start {
            get {
                return ResourceManager.GetString("Start", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Start Container Process Output.
        /// </summary>
        public static string StartContainerOutput {
            get {
                return ResourceManager.GetString("StartContainerOutput", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to invoke TE.exe to validate possible TAEF test: {0}.
        /// </summary>
        public static string TaefCheckFailed {
            get {
                return ResourceManager.GetString("TaefCheckFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TE.exe returned error {0} when trying to validate possible TAEF test: {1}.
        /// </summary>
        public static string TaefCheckReturnedError {
            get {
                return ResourceManager.GetString("TaefCheckReturnedError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TE.exe timed out trying to validate possible TAEF test: {0}.
        /// </summary>
        public static string TaefCheckTimeout {
            get {
                return ResourceManager.GetString("TaefCheckTimeout", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unable to validate possible TAEF test: {0}.
        /// </summary>
        public static string TaefValidationFailed {
            get {
                return ResourceManager.GetString("TaefValidationFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to deserialize an unknown task type!.
        /// </summary>
        public static string TaskBaseDeserializationException {
            get {
                return ResourceManager.GetString("TaskBaseDeserializationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Trying to serialize an unknown task type!.
        /// </summary>
        public static string TaskBaseSerializationException {
            get {
                return ResourceManager.GetString("TaskBaseSerializationException", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Server has {0} TaskLists but new order has only {1} GUIDs!.
        /// </summary>
        public static string TaskListCountMismatch {
            get {
                return ResourceManager.GetString("TaskListCountMismatch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskList with guid {0} already exists!.
        /// </summary>
        public static string TaskListExistsAlready {
            get {
                return ResourceManager.GetString("TaskListExistsAlready", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Could not save TaskLists to {0}!.
        /// </summary>
        public static string TaskListSaveFailed {
            get {
                return ResourceManager.GetString("TaskListSaveFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskList {0} ({1}) with status {2}.
        /// </summary>
        public static string TaskListToString {
            get {
                return ResourceManager.GetString("TaskListToString", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to More than one method with name {0}.
        /// </summary>
        public static string TooManyMethodsFound {
            get {
                return ResourceManager.GetString("TooManyMethodsFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unsupported guid type to poll!.
        /// </summary>
        public static string UnsupportedGuidType {
            get {
                return ResourceManager.GetString("UnsupportedGuidType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskRun {0} is waiting on a result run by the container..
        /// </summary>
        public static string WaitingForContainerTaskRun {
            get {
                return ResourceManager.GetString("WaitingForContainerTaskRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to TaskRun {0} is waiting on an external result..
        /// </summary>
        public static string WaitingForExternalTaskRun {
            get {
                return ResourceManager.GetString("WaitingForExternalTaskRun", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Failed to launch AUMID: {0}.
        /// </summary>
        public static string WDPAppLaunchFailed {
            get {
                return ResourceManager.GetString("WDPAppLaunchFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Device Portal is required for app launch and may not be running on the system..
        /// </summary>
        public static string WDPAppLaunchFailed2 {
            get {
                return ResourceManager.GetString("WDPAppLaunchFailed2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: If it is running, the AUMID may be incorrect..
        /// </summary>
        public static string WDPAppLaunchFailed3 {
            get {
                return ResourceManager.GetString("WDPAppLaunchFailed3", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Sucessfully launched app with AUMID: {0}.
        /// </summary>
        public static string WDPAppLaunchSucceeded {
            get {
                return ResourceManager.GetString("WDPAppLaunchSucceeded", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Device Portal failed with error.
        /// </summary>
        public static string WDPError {
            get {
                return ResourceManager.GetString("WDPError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Device Portal failed with HTTP error.
        /// </summary>
        public static string WDPHttpError {
            get {
                return ResourceManager.GetString("WDPHttpError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Device Portal must be running to call GetInstalledApps!.
        /// </summary>
        public static string WDPNotRunningError {
            get {
                return ResourceManager.GetString("WDPNotRunningError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} is only supported on Windows!.
        /// </summary>
        public static string WindowsOnlyError {
            get {
                return ResourceManager.GetString("WindowsOnlyError", resourceCulture);
            }
        }
    }
}
