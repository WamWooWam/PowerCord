using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerCord.Attributes
{

    public interface IChecksAttribute
    {
        string ErrorMessage { get; }

        Task<bool> RunChecksAsync(PSDiscordCmdlet cmdlet, PSDiscordCmdletContext ctx);
    }
}
