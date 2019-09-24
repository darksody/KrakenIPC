using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace KrakenIPC
{
    public partial class PipeServer<T, U> where T : class, new()
    {
        public void SetSecurity(PipeSecurity pipeSecurity)
        {
            this.server.SetSecurity(pipeSecurity);
        }

        public void SetDefaultSecurity()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            SecurityIdentifier everyoneSID = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            PipeAccessRule accessRule = new PipeAccessRule(everyoneSID, PipeAccessRights.ReadWrite, AccessControlType.Allow);
            pipeSecurity.AddAccessRule(accessRule);
            this.server.SetSecurity(pipeSecurity);
        }
    }
}
