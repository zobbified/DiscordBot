using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Commands.Pokemon
{
    public class CaughtPokemon
    {
        public string Name { get; set; } = "MissingNo";
        public DateTime CaughtAt { get; set; } = DateTime.UtcNow;
        public bool IsShiny { get; set; } = false;
        public int Count { get; set; }

    }
}