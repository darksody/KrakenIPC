using KrakenIPC.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KrakenIPC
{
    internal class AsyncNamedPipeServer : AsyncNamedPipe
    {
        private NamedPipeServerStream PipeServerStream;

        internal AsyncNamedPipeServer(string pipeName)
            : base(pipeName)
        {
        }

        internal override void Close()
        {
            stopEvent.Set();

            if (PipeServerStream != null)
            {
                try
                {
                    PipeServerStream.Close();
                    PipeServerStream.Dispose();
                }
                catch (Exception e)
                {
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }

                PipeServerStream = null;
            }

            if (pipeThread != null)
            {
                pipeThread.Join();
                pipeThread = null;
            }
        }

        internal override bool Send(PipeMessage message)
        {
            if (PipeServerStream != null)
            {
                try
                {
                    var json = JsonConvert.SerializeObject(message);
                    byte[] bytes = Encoding.ASCII.GetBytes(json);
                    PipeServerStream.Write(bytes, 0, bytes.Length);
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
                if (PipeServerStream == null)
                {
                    CreatePipe();
                }
                WaitForData();
            }
        }

        private void CreatePipe()
        {
            if (stopEvent.IsSet)
            {
                return;
            }

            try
            {
                PipeSecurity pipeSecurity = new PipeSecurity();
                SecurityIdentifier everyoneSID = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                PipeAccessRule accessRule = new PipeAccessRule(everyoneSID, PipeAccessRights.ReadWrite, AccessControlType.Allow);
                pipeSecurity.AddAccessRule(accessRule);

                PipeServerStream = new NamedPipeServerStream(
                    pipeName,
                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    1,
                    1,
                    pipeSecurity);

                WaitForConnection();
                FireConnectionChangedEvent(true);
            }
            catch (ObjectDisposedException e)
            {
                /* Thread terminated - user wants to exit thread */
                System.Diagnostics.Trace.WriteLine(e.Message);
            }
            catch (NullReferenceException e)
            {
                /* Thread terminated - user wants to exit thread */
                System.Diagnostics.Trace.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                FireExceptionEvent(e);
            }
        }

        private void WaitForData()
        {
            byte[] readBuffer = new byte[1024 * 16];
            MemoryStream readMemoryStream = new MemoryStream();

            while (!stopEvent.IsSet)
            {
                try
                {
                    int count = PipeServerStream.Read(readBuffer, 0, readBuffer.Length);
                    if (count != 0)
                    {
                        readMemoryStream.Write(readBuffer, 0, count);

                        if (PipeServerStream.IsMessageComplete)
                        {
                            MemoryStream memoryStream = readMemoryStream;
                            readMemoryStream = new MemoryStream();

                            var bytes = memoryStream.ToArray();
                            var json = Encoding.ASCII.GetString(bytes);
                            PipeMessage message = JsonConvert.DeserializeObject<PipeMessage>(json);

                            FireOnMessageReceivedEvent(message);
                        }
                    }
                    else
                    {
                        try
                        {
                            PipeServerStream.Close();
                            PipeServerStream.Dispose();
                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Trace.WriteLine(e.Message);
                        }
                        finally
                        {
                            PipeServerStream = null;
                        }

                        FireConnectionChangedEvent(false);
                        return;
                    }
                }
                catch (ObjectDisposedException e)
                {
                    /* Pipe is closed - method Close() has been closed */
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }
                catch (NullReferenceException e)
                {
                    /* Pipe is closed - method Close() has been closed */
                    System.Diagnostics.Trace.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    FireExceptionEvent(e);
                }
            }
        }

        private void WaitForConnection()
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            Exception exception = null;

            try
            {
                if (stopEvent.IsSet)
                {
                    return;
                }

                PipeServerStream.BeginWaitForConnection(ar =>
                {
                    try
                    {
                        PipeServerStream.EndWaitForConnection(ar);
                    }
                    catch (Exception e)
                    {
                        exception = e;
                    }

                    autoResetEvent.Set();
                }, null);
            }
            catch (Exception e)
            {
                throw e;
            }

            autoResetEvent.WaitOne();

            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
