using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class CaughtPokemon
    {
        public string Name { get; set; }
        public DateTime CaughtAt { get; set; } = DateTime.UtcNow;
        public bool IsShiny { get; set; } = false;
        public int Count { get; set; }

    }
}