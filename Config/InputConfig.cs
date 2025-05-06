using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Replicate.Net.Interfaces;
namespace DiscordBot.Config
{
    public class InputConfig : IPredictionInput
    {
        public string Prompt { get; set; } = string.Empty;
        public bool Loop { get; set; } = false;
        public int Duration { get; set; } = 5;
        public string Aspect_Ratio { get; set; } = "16:9";
    }
}
