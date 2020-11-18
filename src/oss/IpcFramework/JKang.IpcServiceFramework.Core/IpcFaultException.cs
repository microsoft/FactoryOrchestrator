using System;

namespace JKang.IpcServiceFramework
{
    /// <summary>
    /// An exception that can be transfered from server to client
    /// </summary>
#pragma warning disable CA1032 // Implement standard exception constructors
    public class IpcFaultException : IpcException
#pragma warning restore CA1032 // Implement standard exception constructors
    {
        public IpcFaultException(IpcStatus status)
        {
            Status = status;
        }

        public IpcFaultException(IpcStatus status, string message)
            : base(message)
        {
            Status = status;
        }

        public IpcFaultException(IpcStatus status, string message, Exception innerException)
            : base(message, innerException)
        {
            Status = status;
        }

        public IpcStatus Status { get; }
    }
}
