using JKang.IpcServiceFramework.IO;
using JKang.IpcServiceFramework.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace JKang.IpcServiceFramework
{
    public abstract class IpcServiceEndpoint
    {
        protected IpcServiceEndpoint(string name, IServiceProvider serviceProvider)
        {
            Name = name;
            ServiceProvider = serviceProvider;
        }

        public string Name { get; }
        public IServiceProvider ServiceProvider { get; }

        public abstract Task ListenAsync(CancellationToken cancellationToken = default(CancellationToken));
    }

    public abstract class IpcServiceEndpoint<TContract> : IpcServiceEndpoint
        where TContract : class
    {
        private readonly IValueConverter _converter;
        private readonly IIpcMessageSerializer _serializer;

        protected IpcServiceEndpoint(string name, IServiceProvider serviceProvider)
            : base(name, serviceProvider)
        {
            _converter = serviceProvider.GetRequiredService<IValueConverter>();
            _serializer = serviceProvider.GetRequiredService<IIpcMessageSerializer>();
        }

        protected async Task ProcessAsync(Stream server, ILogger logger, CancellationToken cancellationToken)
        {
            using (var writer = new IpcWriter(server, _serializer, leaveOpen: true))
            using (var reader = new IpcReader(server, _serializer, leaveOpen: true))
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    logger?.LogDebug($"[thread {Thread.CurrentThread.ManagedThreadId}] client connected, reading request...");
                    IpcRequest request = await reader.ReadIpcRequestAsync(cancellationToken).ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();

                    logger?.LogDebug($"[thread {Thread.CurrentThread.ManagedThreadId}] request received, invoking '{request.MethodName}'...");
                    IpcResponse response;
                    using (IServiceScope scope = ServiceProvider.CreateScope())
                    {
                        response = await GetReponse(request, scope).ConfigureAwait(false);
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    logger?.LogDebug($"[thread {Thread.CurrentThread.ManagedThreadId}] sending response...");

                    // Attempt to serialize the exception, if it fails to serialize, just send the string version
                    if (!response.Succeed && response.Data != null)
                    {
                        try
                        {
                            _serializer.SerializeResponse(response);
                        }
                        catch (Exception)
                        {
                            response = IpcResponse.Fail(response.Failure);
                        }
                    }

                    await writer.WriteAsync(response, cancellationToken).ConfigureAwait(false);

                    logger?.LogDebug($"[thread {Thread.CurrentThread.ManagedThreadId}] done.");
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, ex.Message);

                    try
                    {
                        // If there was an Exception due to a communication issue sending the error message to the client might throw an Exception as well.
                        // If it does, catch & discard it so the service listener thread doesn't crash.
                        await writer.WriteAsync(IpcResponse.Fail($"{AllExceptionsToString(ex)}"), cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        logger?.LogError($"Unable to send IPCResponse to client after Exception occurred. Reason: {AllExceptionsToString(ex)}");
                    }
                }
            }
        }

        protected async Task<IpcResponse> GetReponse(IpcRequest request, IServiceScope scope)
        {
            object service = scope.ServiceProvider.GetService<TContract>();
            if (service == null)
            {
                return IpcResponse.Fail($"No implementation of interface '{typeof(TContract).FullName}' found.");
            }

            MethodInfo method = GetUnambiguousMethod(request, service);

            if (method == null)
            {
                return IpcResponse.Fail($"Method '{request.MethodName}' not found in interface '{typeof(TContract).FullName}'.");
            }

            ParameterInfo[] paramInfos = method.GetParameters();
            if (paramInfos.Length != request.Parameters.Length)
            {
                return IpcResponse.Fail($"Parameter mismatch.");
            }

            Type[] genericArguments = method.GetGenericArguments();
            if (genericArguments.Length != request.GenericArguments.Length)
            {
                return IpcResponse.Fail($"Generic arguments mismatch.");
            }

            object[] args = new object[paramInfos.Length];
            for (int i = 0; i < args.Length; i++)
            {
                object origValue = request.Parameters[i];
                Type destType = paramInfos[i].ParameterType;
                if (destType.IsGenericParameter)
                {
                    destType = request.GenericArguments[destType.GenericParameterPosition];
                }

                if (_converter.TryConvert(origValue, destType, out object arg))
                {
                    args[i] = arg;
                }
                else
                {
                    return IpcResponse.Fail($"Cannot convert value of parameter '{paramInfos[i].Name}' ({origValue}) from {origValue.GetType().Name} to {destType.Name}.");
                }
            }

            try
            {
                if (method.IsGenericMethod)
                {
                    method = method.MakeGenericMethod(request.GenericArguments);
                }

                object @return = method.Invoke(service, args);

                if (@return is Task)
                {
                    await ((Task)@return).ConfigureAwait(false);

                    var resultProperty = @return.GetType().GetProperty("Result");
                    return IpcResponse.Success(resultProperty?.GetValue(@return));
                }
                else
                {
                    return IpcResponse.Success(@return);
                }
            }
            catch (TargetInvocationException ex)
            {
                return IpcResponse.Fail($"{AllExceptionsToString(ex.InnerException)}", ex.InnerException);
            }
            catch (Exception ex)
            {
                return IpcResponse.Fail($"Internal server error: {AllExceptionsToString(ex)}", ex);
            }
        }

        /// <summary>
        /// Get the method that matches the requested signature
        /// </summary>
        /// <param name="request">The service call request</param>
        /// <param name="service">The service</param>
        /// <returns>The disambiguated service method</returns>
        public static MethodInfo GetUnambiguousMethod(IpcRequest request, object service)
        {
            if (request == null || service == null)
            {
                return null;
            }

            MethodInfo method = null;
            var types = service.GetType().GetInterfaces(); // Get all interfaces on the service
            var allMethods = types.SelectMany(t => t.GetMethods()); // Get all methods on the interfaces
            var serviceMethods = allMethods.Where(t => t.Name == request.MethodName).ToList();

            // disambiguate by matching parameter types
            foreach (var serviceMethod in serviceMethods)
            {
                var serviceMethodParameters = serviceMethod.GetParameters();
                var parameterTypeMatches = 0;

                if (serviceMethodParameters.Length == request.Parameters.Length && serviceMethod.GetGenericArguments().Length == request.GenericArguments.Length)
                {
                    for (int parameterIndex = 0; parameterIndex < serviceMethodParameters.Length; parameterIndex++)
                    {
                        Type serviceParameterType = serviceMethodParameters[parameterIndex].ParameterType.IsGenericParameter ?
                                            request.GenericArguments[serviceMethodParameters[parameterIndex].ParameterType.GenericParameterPosition] :
                                            serviceMethodParameters[parameterIndex].ParameterType;

                        if (serviceParameterType == request.ParameterTypes[parameterIndex])
                        {
                            parameterTypeMatches++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (parameterTypeMatches == serviceMethodParameters.Length)
                    {
                        method = serviceMethod;        // signatures match so assign
                        break;
                    }
                }
            }

            return method;
        }

        public static string AllExceptionsToString(Exception ex)
        {
            string ret = "";
            var exc = ex;
            while (exc != null)
            {
                if (ret.Length > 0)
                {
                    ret += " -> ";
                }
                if (exc.Message != null)
                {
                    ret += $"{exc.GetType().ToString()}:{exc.Message}";
                }
                else
                {
                    ret += $"{exc.GetType().ToString()}";
                }

                exc = exc.InnerException;
            }

            return ret;
        }
    }
}
