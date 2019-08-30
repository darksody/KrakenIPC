using KrakenIPC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenIPC
{
    internal abstract class AsyncNamedPipe
    {
        protected string pipeName;
        protected ManualResetEventSlim stopEvent;
        protected Thread pipeThread;

        internal event Action<Exception> OnPipeException;
        internal event Action<bool> OnPipeConnectionChanged;
        internal event Action<PipeMessage> OnMessageReceived;

        protected AsyncNamedPipe(string pipeName)
        {
            stopEvent = new ManualResetEventSlim(false);
            this.pipeName = pipeName;
        }

        protected void FireExceptionEvent(Exception e)
        {
            OnPipeException?.Invoke(e);
        }

        protected void FireConnectionChangedEvent(bool connected)
        {
            OnPipeConnectionChanged?.Invoke(connected);
        }

        protected void FireOnMessageReceivedEvent(PipeMessage message)
        {
            OnMessageReceived?.Invoke(message);
        }

        internal void Open()
        {
            if (pipeThread != null)
            {
                return;
            }

            pipeThread = new Thread(new ThreadStart(Start));
            pipeThread.Start();
        }

        internal abstract void Close();

        internal abstract bool Send(PipeMessage message);

        protected abstract void Start();
    }
}
