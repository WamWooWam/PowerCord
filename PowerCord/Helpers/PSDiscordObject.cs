using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Helpers
{
    public abstract class PSDiscordObject : IDisposable
    {
        public abstract Task ProcessAsync(PSDiscordCmdletContext ctx);
        public abstract void Dispose();
    }
}
