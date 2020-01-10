using KrakenIPC.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenIPC
{
    /// <summary>
    /// Client implementation over named pipes protocol
    /// </summary>
    /// <typeparam name="T">Represents the contract interface</typeparam>
    public class PipeClient<T> where T : class
    {
        private AsyncNamedPipeClient client;
        private T proxy;
        private ManualResetEventSlim responseEvent = new ManualResetEventSlim(false);
        private PipeMessage responseMessage = null;
        private int receiveTimeout;

        public event Action<bool> OnPipeConnectionChanged;
        public event Action<Exception> OnPipeException;

        /// <summary>
        /// Get an instance of PipeClient of the specified contract interface
        /// </summary>
        /// <param name="receiveTimeout">Receive timeout in milliseconds</param>
        public PipeClient(int receiveTimeout = 3000)
        {
            this.receiveTimeout = receiveTimeout;
            this.client = new AsyncNamedPipeClient(typeof(T).ToString());
            this.client.Open();
            this.client.OnMessageReceived += Client_OnMessageReceived;
            this.client.OnPipeConnectionChanged += Client_OnPipeConnectionChanged;
            this.client.OnPipeException += Client_OnPipeException;

            this.proxy = ProxyHelper.GetInstance<T>(OnMethodCallback);
        }

        /// <summary>
        /// Uses the proxy instance to call the server
        /// </summary>
        /// <param name="callback">Delegate to use the proxy class</param>
        public void UseProxy(Action<T> callback)
        {
            callback(proxy);
        }

        /// <summary>
        /// Get the proxy class so you can make calls to the server
        /// </summary>
        /// <returns>Proxy instance that respects the contract</returns>
        public T GetProxy()
        {
            return this.proxy;
        }

        private object OnMethodCallback(string methodName, List<object> parameterValues, List<object> parameterTypes, Type returnType)
        {
            //call server here, get response if needed
            MethodCallRequest request = new MethodCallRequest()
            {
                MethodName = methodName,
                ParameterTypes = parameterTypes.OfType<Type>().ToList(),
                ParameterValues = parameterValues,
                ReturnType = returnType
            };

            var requestMessage = new PipeMessage<MethodCallRequest>(request);
            var json = JsonConvert.SerializeObject(requestMessage);
            byte[] requestBytes = Encoding.ASCII.GetBytes(json);
            this.client.Send(requestBytes);
            //eventhandler waitone
            responseEvent.Wait(TimeSpan.FromMilliseconds(receiveTimeout));
            if (responseEvent.IsSet)
            {
                //get the response stored, reset the object
                var localResponse = responseMessage;
                responseMessage = null;
                responseEvent.Reset();
                //parse the response and return it
                //deserialize it to MethodCallResponse
                var result = localResponse.GetPayload<MethodCallResponse>();
                if (result.ReturnValue == null)
                {
                    return null;
                }
                else
                {
                    if (result.ReturnType == typeof(ServerException))
                    {
                        throw new ServerException(result.ReturnValue.ToString());
                    }
                    if (result.ReturnValue.ToString().StartsWith("{") == false && result.ReturnValue.ToString().StartsWith("[") == false)
                    {
                        //not a json, it's a primitive
                        if (result.ReturnType.BaseType == typeof(Enum))
                        {
                            return Convert.ChangeType(result.ReturnValue, typeof(int));
                        }
                        return Convert.ChangeType(result.ReturnValue, result.ReturnType);
                    }
                    return JsonConvert.DeserializeObject(result.ReturnValue.ToString(), returnType);
                }
            }
            else
            {
                throw new ServerException("Server timeout reached");
            }
        }

        private void Client_OnPipeException(Exception ex)
        {
            OnPipeException?.Invoke(ex);
        }

        private void Client_OnPipeConnectionChanged(bool state)
        {
            OnPipeConnectionChanged?.Invoke(state);
        }

        private void Client_OnMessageReceived(byte[] bytes)
        {
            var json = Encoding.ASCII.GetString(bytes);
            PipeMessage message = JsonConvert.DeserializeObject<PipeMessage>(json);
            responseMessage = message;
            responseEvent.Set();
        }
    }
}
