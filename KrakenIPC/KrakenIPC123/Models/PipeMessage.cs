using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KrakenIPC.Models
{
    public class PipeMessage
    {
        public string Payload { get; set; }
        public Type Type { get; protected set; }

        public T GetPayload<T>()
        {
            return JsonConvert.DeserializeObject<T>(Payload);
        }
    }

    public class PipeMessage<T> : PipeMessage
    {
        public PipeMessage(T obj)
        {
            this.Type = typeof(T);
            this.Payload = JsonConvert.SerializeObject(obj);
        }
    }
}