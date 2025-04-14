using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class BotConfig
    {
        public string DiscordToken { get; set; } = string.Empty;
        public string ReplicateToken { get; set; } = string.Empty;
        public string ServerID { get; set; } = string.Empty;
    }
}