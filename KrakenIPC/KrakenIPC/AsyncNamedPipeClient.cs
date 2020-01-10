using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenIPC
{
    internal class AsyncNamedPipeClient : AsyncNamedPipe
    {
        private NamedPipeClientStream PipeClientStream;

        internal AsyncNamedPipeClient(string pipeName)
            : base(pipeName)
        {
        }

        internal override void Close()
        {
            stopEvent.Set();

            if (PipeClientStream != null)
            {
                try
                {
                    PipeClientStream.Close();
                    PipeClientStream.Dispose();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }

                PipeClientStream = null;
            }

            if (pipeThread != null)
            {
                pipeThread.Join();
                pipeThread = null;
            }
        }

        internal override bool Send(byte[] message)
        {
            if (PipeClientStream != null)
            {
                try
                {
                    PipeClientStream.Write(message, 0, message.Length);
                    return true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }
            }

            return false;
        }

        protected override void Start()
        {
            stopEvent.Reset();
            while (!stopEvent.IsSet)
            {
                if (PipeClientStream == null)
                {
                    CreatePipe();
                }
                WaitForData();
            }
        }

        protected void CreatePipe()
        {
            if (stopEvent.IsSet)
            {
                return;
            }

            try
            {
                PipeClientStream = new NamedPipeClientStream(
                    ".",
                    pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                while (!stopEvent.IsSet)
                {
                    try
                    {
                        PipeClientStream.Connect(1);
                        PipeClientStream.ReadMode = PipeTransmissionMode.Message;
                        FireConnectionChangedEvent(true);
                        return;
                    }
                    catch (TimeoutException e)
                    {
                        System.Diagnostics.Trace.WriteLine(e.Message);
                    }
                    catch (Exception e)
                    {
                        FireExceptionEvent(e);
                    }

                    Thread.Sleep(100);
                }
            }
            catch (ObjectDisposedException e)
            {
                System.Diagnostics.Trace.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                FireExceptionEvent(e);
            }
        }

        protected void WaitForData()
        {
            byte[] readBuffer = new byte[1024 * 16];
            MemoryStream readMemoryStream = new MemoryStream();

            while (!stopEvent.IsSet)
            {
                try
                {
                    int count = PipeClientStream.Read(readBuffer, 0, readBuffer.Length);
                    if (count != 0)
                    {
                        readMemoryStream.Write(readBuffer, 0, count);

                        if (PipeClientStream.IsMessageComplete)
                        {
                            MemoryStream memoryStream = readMemoryStream;
                            readMemoryStream = new MemoryStream();
                            var bytes = memoryStream.ToArray();

                            FireOnMessageReceivedEvent(bytes);
                        }
                    }
                    else
                    {
                        try
                        {
                            PipeClientStream.Close();
                            PipeClientStream.Dispose();
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.WriteLine(e.Message);
                        }
                        finally
                        {
                            PipeClientStream = null;
                        }
                        FireConnectionChangedEvent(false);
                        break;
                    }
                }
                catch (ObjectDisposedException e)
                {
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    FireExceptionEvent(e);
                }
            }
        }
    }
}
