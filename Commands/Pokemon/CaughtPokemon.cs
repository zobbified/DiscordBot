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
        public int Amount { get; set; }
        public bool Caught { get; set; }
        public string? ImageUrl { get; set; }

    }
}