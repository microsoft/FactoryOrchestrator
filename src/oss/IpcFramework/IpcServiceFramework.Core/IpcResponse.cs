using System;
using Newtonsoft.Json;

namespace JKang.IpcServiceFramework
{
    public class IpcResponse
    {
        [JsonConstructor]
        private IpcResponse(bool succeed, object data, string failure, Type exceptionType)
        {
            Succeed = succeed;
            Data = data;
            Failure = failure;
            ExceptionType = exceptionType;
        }

        public static IpcResponse Fail(string failure, Exception exception = null)
        {
            return new IpcResponse(false, exception, failure, exception?.GetType());
        }

        public static IpcResponse Success(object data)
        {
            return new IpcResponse(true, data, null, null);
        }

        public bool Succeed { get; }
        public object Data { get; }
        public string Failure { get; }
        public Type ExceptionType { get; }
    }
}
