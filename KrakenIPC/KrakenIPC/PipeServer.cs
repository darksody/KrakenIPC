using KrakenIPC.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace KrakenIPC
{
    /// <summary>
    /// Server using the named pipes protocol behind the scenes.
    /// </summary>
    /// <typeparam name="T">Represents the implementation of the server. Must be a class with a parameterless constructor</typeparam>
    /// <typeparam name="U">Represents the contract interface. The previous type param MUST implement this interface</typeparam>
    public class PipeServer<T, U> where T : class, new()
    {
        private AsyncNamedPipeServer server;
        private T instance;

        public event Action<Exception> OnPipeException;
        public event Action<bool> OnPipeConnectionChanged;

        /// <summary>
        /// Get an instance of PipeServer. T represents the service implementation and U its interface
        /// </summary>
        public PipeServer()
        {
            Console.WriteLine(typeof(U).ToString() + " started...");
            this.server = new AsyncNamedPipeServer(typeof(U).ToString());
            this.server.OnMessageReceived += Server_OnMessageReceived;
            this.server.OnPipeConnectionChanged += Server_OnPipeConnectionChanged;
            this.server.OnPipeException += Server_OnPipeException;
            this.server.Open();

            this.instance = new T();
        }

        private void Server_OnMessageReceived(PipeMessage e)
        {
            //get method call data
            //call implementation and get response
            //send response (if error goes here, need to throw something to the client. Also need to time this out on the client side)
            var request = e.GetPayload<MethodCallRequest>();

            object result = null;
            try
            {
                MethodInfo invokeMethod = typeof(T).GetMethod(request.MethodName);
                for (int i = 0; i < request.ParameterValues.Count; i++)
                {
                    var jObject = request.ParameterValues[i] as JObject;
                    if (jObject != null)
                    {
                        request.ParameterValues[i] = jObject.ToObject(request.ParameterTypes[i]);
                    }
                    else
                    {
                        request.ParameterValues[i] = Convert.ChangeType(request.ParameterValues[i], request.ParameterTypes[i]);
                    }
                }
                result = invokeMethod.Invoke(instance, request.ParameterValues.ToArray());
            }
            catch (Exception ex)
            {
                request.ReturnType = typeof(ServerException);
                result = ex.InnerException?.Message;
            }

            var response = new MethodCallResponse()
            {
                MethodName = request.MethodName,
                ReturnType = request.ReturnType,
                ReturnValue = result
            };
            server.Send(new PipeMessage<MethodCallResponse>(response));
        }

        private void Server_OnPipeException(Exception ex)
        {
            OnPipeException?.Invoke(ex);
        }

        private void Server_OnPipeConnectionChanged(bool state)
        {
            OnPipeConnectionChanged?.Invoke(state);
        }
    }
}
