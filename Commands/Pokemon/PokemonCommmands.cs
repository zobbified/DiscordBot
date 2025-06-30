using Discord.Interactions;
using Discord;
using Discord.WebSocket;
using System.Drawing;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using DiscordBot.SQL;
using Color = System.Drawing.Color;

namespace DiscordBot.Commands.Pokemon
{
    [CommandContextType(InteractionContextType.BotDm, InteractionContextType.PrivateChannel)]
    [IntegrationType(ApplicationIntegrationType.UserInstall)]

    [Group("poke", "Pokemon Commands")]

    public class PokemonCommmands : InteractionModuleBase<SocketInteractionContext>
    {
        private static readonly HttpClient _client = new();
        private static readonly Helper _dbHelper = new();
        private static readonly Dictionary<ulong, List<Embed>> openPackCards = new();

        [SlashCommand("catch", "Catch a pokemon.")]
        public async Task CatchPokemon([Summary("name", "The name of the Pokémon")] string? name = null)
        {
            await DeferAsync();

            try
            {
                string url;
                if (name != null)
                {
                    url = $"https://pokeapi.co/api/v2/pokemon/{name.ToLower()}";
                }
                else
                {
                    Random rng = new Random();
                    int randomId = rng.Next(0, 1026);
                    url = $"https://pokeapi.co/api/v2/pokemon/{randomId}";
                }

                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    await FollowupAsync("❌ Pokémon not found.");
                    return;
                }

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var root = doc.RootElement;

                if (!root.TryGetProperty("name", out var nameProp))
                {
                    await FollowupAsync("❌ Pokémon not found.");
                    return;
                }

                string pokeName = nameProp.GetString() ?? "Unknown";
                int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
                //int baseXp = root.TryGetProperty("base_experience", out var xpProp) ? xpProp.GetInt32() : 0;

                string typeStr = "Unknown";
                if (root.TryGetProperty("types", out var typesProp))
                {
                    var typeNames = typesProp.EnumerateArray()
                        .Select(t => t.GetProperty("type").GetProperty("name").GetString())
                        .Where(n => n != null);
                    typeStr = string.Join(", ", typeNames);
                }

                string abilityStr = "None";
                if (root.TryGetProperty("abilities", out var abilitiesProp))
                {
                    var abilityNames = abilitiesProp.EnumerateArray()
                        .Select(a => a.GetProperty("ability").GetProperty("name").GetString())
                        .Where(n => n != null);
                    abilityStr = string.Join(", ", abilityNames);
                    if (abilityStr.Length > 1024) abilityStr = abilityStr[..1021] + "...";
                }

                string spriteUrl = "";
                if (root.TryGetProperty("sprites", out var spritesProp) &&
                    spritesProp.TryGetProperty("other", out var otherProp) &&
                    otherProp.TryGetProperty("official-artwork", out var homeProp) &&
                    homeProp.TryGetProperty("front_default", out var spriteProp))
                {
                    spriteUrl = spriteProp.GetString() ?? "";
                }

                string speciesUrl = "";
                if (root.TryGetProperty("species", out var speciesProp) &&
                    speciesProp.TryGetProperty("url", out var urlProp))
                {
                    speciesUrl = urlProp.GetString() ?? "";
                }

                string generation = "Unknown";
                string evoChainUrl = "";
                if (!string.IsNullOrEmpty(speciesUrl))
                {
                    var speciesResponse = await _client.GetAsync(speciesUrl);
                    if (speciesResponse.IsSuccessStatusCode)
                    {
                        using var speciesDoc = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
                        var speciesRoot = speciesDoc.RootElement;

                        if (speciesRoot.TryGetProperty("generation", out var genProp) &&
                            genProp.TryGetProperty("name", out var genName))
                        {
                            generation = genName.GetString() ?? "Unknown";
                        }

                        if (speciesRoot.TryGetProperty("evolution_chain", out var evoProp) &&
                            evoProp.TryGetProperty("url", out var evoUrl))
                        {
                            evoChainUrl = evoUrl.GetString() ?? "";
                        }
                    }
                }

                var evoNames = new List<string>();
                if (!string.IsNullOrWhiteSpace(evoChainUrl))
                {
                    var evoResponse = await _client.GetAsync(evoChainUrl);
                    if (evoResponse.IsSuccessStatusCode)
                    {
                        using var evoDoc = JsonDocument.Parse(await evoResponse.Content.ReadAsStringAsync());
                        if (evoDoc.RootElement.TryGetProperty("chain", out var chainRoot))
                        {
                            void Traverse(JsonElement node)
                            {
                                string evoName = node.TryGetProperty("species", out var speciesNode) &&
                                                 speciesNode.TryGetProperty("name", out var nameNode)
                                                 ? nameNode.GetString() ?? "Unknown"
                                                 : "Unknown";
                                evoNames.Add(evoName);
                                if (node.TryGetProperty("evolves_to", out var evolvesTo))
                                {
                                    foreach (var next in evolvesTo.EnumerateArray())
                                        Traverse(next);
                                }
                            }
                            Traverse(chainRoot);
                        }
                    }
                }
                string dexId = id.ToString("D4");

                if (evoNames.Count == 0) evoNames.Add("Unknown");
                generation = generation.Replace("generation-", "");
                pokeName = $"#{dexId} {pokeName.ToUpper()} ({generation.ToUpper()})";
                var embed = new EmbedBuilder()
                    .WithTitle($"{pokeName}")
                    .WithDescription($"**Type:** {typeStr.ToUpper()}")
                    .WithImageUrl(spriteUrl)
                    .AddField("Abilities", abilityStr.ToUpper(), inline: true)
                    .AddField("Evolution Chain", string.Join(" → ", evoNames).ToUpper(), inline: true)
                    .WithColor(Discord.Color.Gold)
                    .WithFooter(footer => footer.Text = $"Requested by {Context.User.Username}")
                    .Build();


                _dbHelper.SavePokemon(Context.User.Id, pokeName, 1, true, spriteUrl);
                // add logic for quantity later

                await FollowupAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex}");
                await FollowupAsync("❌ Something went wrong. Try again or check the Pokémon name.");
            }
        }

        [SlashCommand("pack", "Open a random Pokémon pack ($1000)")]
        public async Task OpenPack()
        {
            await DeferAsync();

            ulong userId = Context.User.Id;

            if (_dbHelper.GetMoney(userId) < 1000)
            {
                await FollowupAsync("Broke boy :skull:");
                return;
            }
            _dbHelper.SaveMoney(userId, -1000);

            var rand = new Random();
            var cards = new List<Embed>();

            for (int i = 0; i < 5; i++)
            {
                int pokemonId = rand.Next(1, 1026);
                var url = $"https://pokeapi.co/api/v2/pokemon/{pokemonId}";
                var response = await _client.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                var root = doc.RootElement;

                string speciesUrl = "";
                if (root.TryGetProperty("species", out var speciesProp) &&
                    speciesProp.TryGetProperty("url", out var urlProp))
                {
                    speciesUrl = urlProp.GetString() ?? "";
                }

                string generation = "Unknown";
                string evoChainUrl = "";
                if (!string.IsNullOrEmpty(speciesUrl))
                {
                    var speciesResponse = await _client.GetAsync(speciesUrl);
                    if (speciesResponse.IsSuccessStatusCode)
                    {
                        using var speciesDoc = JsonDocument.Parse(await speciesResponse.Content.ReadAsStringAsync());
                        var speciesRoot = speciesDoc.RootElement;

                        if (speciesRoot.TryGetProperty("generation", out var genProp) &&
                            genProp.TryGetProperty("name", out var genName))
                        {
                            generation = genName.GetString() ?? "Unknown";
                        }

                        if (speciesRoot.TryGetProperty("evolution_chain", out var evoProp) &&
                            evoProp.TryGetProperty("url", out var evoUrl))
                        {
                            evoChainUrl = evoUrl.GetString() ?? "";
                        }
                    }
                }

                int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
                string dexId = id.ToString("D4");


                if (!root.TryGetProperty("name", out var nameProp)) continue;
                string name = nameProp.GetString() ?? "Unknown";
                generation = generation.Replace("generation-", "");
                name = $"#{dexId} {name.ToUpper()} ({generation.ToUpper()})";
                bool isShiny = rand.NextDouble() < 0.01;

                string imageUrl = "";
                if (root.TryGetProperty("sprites", out var spritesProp) &&
                    spritesProp.TryGetProperty("other", out var otherProp) &&
                    otherProp.TryGetProperty("official-artwork", out var artProp))
                {
                    if (isShiny && artProp.TryGetProperty("front_shiny", out var shinyProp))
                    {
                        imageUrl = shinyProp.GetString() ?? "";
                    }
                    else if (artProp.TryGetProperty("front_default", out var defaultProp))
                    {
                        imageUrl = defaultProp.GetString() ?? "";
                    }
                }

                _dbHelper.SavePokemon(Context.User.Id, name, 1, true, imageUrl);

                var embed = new EmbedBuilder()
                    .WithTitle($"{(isShiny ? "✨ Shiny " : "")}{name}")
                    .WithImageUrl(imageUrl)
                    .WithColor(isShiny ? Discord.Color.Magenta : Discord.Color.Blue)
                    .WithFooter($"Card {cards.Count + 1} of 5")
                    .Build();

                cards.Add(embed);
            }

            if (cards.Count == 0)
            {
                await FollowupAsync("❌ Failed to open the pack.");
                return;
            }

            // Store cards per-user, ideally in memory or a temporary store
            openPackCards[userId] = cards;

            var component = new ComponentBuilder()
                .WithButton("⬅️ Previous", $"pack_{userId}_0", disabled: true)
                .WithButton("Next ➡️", $"pack_{userId}_1", disabled: cards.Count <= 1)
                .Build();

            await FollowupAsync(embed: cards[0], components: component);

        }

        public Stream RenderPackImage(List<(string name, string spriteUrl, bool isShiny)> pokemons)
        {
            int columns = 5;
            int rows = (int)Math.Ceiling(pokemons.Count / (double)columns);
            int cellWidth = 220;
            int cellHeight = 100;

            Bitmap bmp = new Bitmap(columns * cellWidth, rows * cellHeight);
            using Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            var font = new Font("Arial", 12);
            var brush = new SolidBrush(Color.White);
            var shinyBrush = new SolidBrush(Color.Goldenrod);

            for (int i = 0; i < pokemons.Count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                int x = col * cellWidth;
                int y = row * cellHeight;

                var (name, spriteUrl, isShiny) = pokemons[i];

                try
                {
                    if (!string.IsNullOrEmpty(spriteUrl))
                    {
                        using var wc = new WebClient();
                        using var spriteStream = wc.OpenRead(spriteUrl);
                        using var sprite = System.Drawing.Image.FromStream(spriteStream);
                        g.DrawImage(sprite, x + 10, y + 10, 64, 64);
                    }
                }
                catch
                {
                    // Ignore broken sprites
                }

                g.DrawString(name.Split(' ', 2).First(), font,
                    isShiny ? shinyBrush : brush, new PointF(x + 75, y));
                g.DrawString(isShiny ? "✨ " + name.Split(' ', 2).Last() : name.Split(' ', 2).Last(), font,
                    isShiny ? shinyBrush : brush, new PointF(x + 75, y + 15));
            }

            var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            return ms;
        }

        [SlashCommand("packs", "Open 10 Pokémon packs ($10,000)")]
        public async Task OpenTenPacks([Summary("user", "Give cards to another user!")] SocketUser? user = null)
        {
            const int PackCost = 10000;
            const int CardsPerPack = 5;
            const int TotalCards = 10 * CardsPerPack;
            const double ShinyChance = 0.01;

            await DeferAsync();

            var targetUser = user ?? Context.User;
            var userId = targetUser.Id;

            if (_dbHelper.GetMoney(Context.User.Id) < PackCost)
            {
                await FollowupAsync("You're too broke for 10 packs 💸 ($10,000 needed).");
                return;
            }

            _dbHelper.SaveMoney(Context.User.Id, -PackCost);

            var rand = new Random();
            var pokemonResults = new List<(string name, string spriteUrl, bool isShiny)>();

            for (int i = 0; i < TotalCards; i++)
            {
                int pokemonId = rand.Next(1, 1026);
                var url = $"https://pokeapi.co/api/v2/pokemon/{pokemonId}";

                try
                {
                    var response = await _client.GetAsync(url);
                    if (!response.IsSuccessStatusCode) continue;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    var root = doc.RootElement;

                    int id = root.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : -1;
                    string dexId = id.ToString("D4");
                    string name = root.TryGetProperty("name", out var nameProp)
                        ? nameProp.GetString() ?? "Unknown"
                        : "Unknown";

                    string spriteUrl = "";
                    if (root.TryGetProperty("sprites", out var spritesProp))
                    {
                        spriteUrl = spritesProp.TryGetProperty("front_default", out var spriteProp)
                            ? spriteProp.GetString() ?? ""
                            : "";
                    }

                    bool isShiny = rand.NextDouble() < ShinyChance;
                    string displayName = isShiny
                        ? $"#{dexId} SHINY {name.ToUpper()}"
                        : $"#{dexId} {name.ToUpper()}";

                    _dbHelper.SavePokemon(userId, displayName, 1, true, spriteUrl);
                    pokemonResults.Add((displayName, spriteUrl, isShiny));
                }
                catch (Exception ex)
                {
                    // Optionally log the error for debugging
                    Console.WriteLine($"Error fetching Pokémon ID {pokemonId}: {ex.Message}");
                    continue;
                }
            }

            var imageStream = RenderPackImage(pokemonResults);

            await FollowupWithFileAsync(imageStream, "packs.png",
                text: $"🎁 {targetUser.Mention} opened 10 packs and got {TotalCards} Pokémon!");
        }


        [ComponentInteraction("pack_*_*")]
        public async Task HandlePackPagination(string userIdStr, string indexStr)
        {
            if (!ulong.TryParse(userIdStr, out var userId) || userId != Context.User.Id)
            {
                await RespondAsync("❌ You can't interact with someone else's pack.", ephemeral: true);
                return;
            }

            if (!int.TryParse(indexStr, out int index)) return;
            if (!openPackCards.TryGetValue(userId, out var cards) || index < 0 || index >= cards.Count)
            {
                await RespondAsync("❌ Card not found.", ephemeral: true);
                return;
            }

            var component = new ComponentBuilder()
                .WithButton("⬅️ Previous", $"pack_{userId}_{index - 1}", disabled: index == 0)
                .WithButton("Next ➡️", $"pack_{userId}_{index + 1}", disabled: index == cards.Count - 1)
                .Build();

            await Context.Interaction.DeferAsync();  // Defer interaction to avoid timeout
            await Context.Interaction.ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = cards[index];
                msg.Components = component;
            });

        }
        public Stream GeneratePokedexPage(
        ulong userId,
        List<(string name, int amount, bool caught, string img)> caughtList,
        int page,
        int perPage,
        string avatarUrl,
        bool showAll)
        {
            const int gridCols = 5, gridRows = 5;
            const int cellSize = 128, padding = 8;
            const int maxDex = 1025;

            int imageWidth = gridCols * (cellSize + padding);
            int imageHeight = gridRows * (cellSize + padding);

            var bitmap = new Bitmap(imageWidth, imageHeight);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);

            if (showAll)
            {
                // Use dictionary for fast lookup by dex ID
                var caughtDict = caughtList.ToDictionary(
                    p => p.name.Split(' ')[0].ToLower(),  // assumes name is "#0001 Bulbasaur"
                    p => (p.img, p.caught)
                );

                int start = (page - 1) * perPage;

                for (int i = 0; i < perPage && (start + i) < maxDex; i++)
                {
                    int dexNum = start + i + 1;
                    string dexId = dexNum.ToString("D4");
                    string nameKey = $"#{dexId}".ToLower();

                    string displayName = $"#{dexId}";
                    bool caught = false;
                    string spriteUrl = "";

                    if (caughtDict.TryGetValue(nameKey, out var data))
                    {
                        spriteUrl = data.img;
                        caught = data.caught;
                    }

                    var position = new Point((i % gridCols) * (cellSize + padding), (i / gridCols) * (cellSize + padding));
                    if (caught && !string.IsNullOrWhiteSpace(spriteUrl))
                    {
                        DrawCaughtPokemon(g, spriteUrl, position, cellSize, displayName, avatarUrl);
                    }
                    else
                    {
                        DrawUnknownPokemon(g, position, cellSize, displayName);
                    }
                }
            }
            else
            {
                var pagedCaught = caughtList
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .ToList();

                for (int i = 0; i < pagedCaught.Count; i++)
                {
                    var entry = pagedCaught[i];
                    if (!entry.caught || string.IsNullOrWhiteSpace(entry.img))
                        continue;

                    var position = new Point((i % gridCols) * (cellSize + padding), (i / gridCols) * (cellSize + padding));
                    DrawCaughtPokemon(g, entry.img, position, cellSize, entry.name.ToUpper(), avatarUrl);
                }
            }

            var stream = new MemoryStream();
            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }

        void DrawCaughtPokemon(Graphics g, string spriteUrl, Point pos, int size, string name, string avatarUrl)
        {
            using var sprite = DownloadImage(spriteUrl);
            using var avatar = DownloadImage(avatarUrl);
            var font = new Font("Arial", 12, FontStyle.Bold);

            g.DrawImage(sprite, pos.X, pos.Y, size, size);
            g.DrawString($"{name.Substring(0, 5)}", font, Brushes.White, pos.X, pos.Y + size - 17);
            //g.DrawImage(avatar, pos.X + size - 24, pos.Y + size - 24, 24, 24);
        }

        void DrawUnknownPokemon(Graphics g, Point pos, int size, string number)
        {
            var rect = new Rectangle(pos.X, pos.Y, size, size);
            g.FillRectangle(Brushes.Black, rect);
            var font = new Font("Arial", 12, FontStyle.Bold);
            g.DrawString(number, font, Brushes.White, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
        Bitmap DownloadImage(string url)
        {
            using var client = new WebClient();
            using var stream = client.OpenRead(url);
            return new Bitmap(stream);
        }

        public MemoryStream GenerateProgressBarImage(int caught, int total, int width = 320, int height = 25)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);

                float progress = caught / (float)total;
                int fillWidth = (int)(width * progress);

                // Convert progress (0.0–1.0) to hue (0–360)
                float hue = progress * 360f; // red → purple
                Color fillColor = FromHsv(hue, 1f, 1f);

                using (Brush fillBrush = new SolidBrush(fillColor))
                {
                    g.FillRectangle(fillBrush, 0, 0, fillWidth, height);
                }

                using (Pen border = new Pen(Color.Black, 2))
                    g.DrawRectangle(border, 0, 0, width - 1, height - 1);

                string text = $"{caught}/{total}";
                using Font font = new Font("Arial", 12, FontStyle.Bold);
                var textSize = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.Black, (width - textSize.Width) / 2, (height - textSize.Height) / 2);
            }

            var stream = new MemoryStream();
            bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
            stream.Position = 0;
            return stream;
        }

        public static Color FromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value *= 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            return hi switch
            {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q),
            };
        }

        [SlashCommand("killpokemon", "Delete Pokémon from your Pokédex")]
        public async Task KillPokemon(
        [Summary("user", "Leave blank to delete your own Pokémon")] SocketUser? user = null,
        [Summary("names", "Enter Pokémon names to delete, or leave blank to delete all")] string? names = null)
        {
            await DeferAsync();

            var targetUser = user ?? Context.User;
            var userId = targetUser.Id;

            try
            {
                var allCaught = _dbHelper.GetPokemon(userId);

                List<string> toDelete = [];

                if (names == null || names.Length == 0)
                {
                    for (int i = 0; i < allCaught.Count; i++)
                    {
                        toDelete.Add(allCaught[i].name);

                    }
                }
                else
                {
                    // Case-insensitive match by name
                    for (int i = 0; i < names.Split(' ', 1025).Length; i++)
                    {
                        toDelete.Add(names.Split(' ', 1025)[i]);

                    }

                }

                if (toDelete.Count == 0)
                {
                    await FollowupAsync("❌ No matching Pokémon found to delete.");
                    return;
                }
                string pokeGrave = "";

                foreach (var pokemon in toDelete)
                {
                    pokeGrave += _dbHelper.GetPokemon(userId)[int.Parse(pokemon)].name + " ";
                    _dbHelper.KillPokemon(userId, pokemon);
                }

                await FollowupAsync($"✅ Killed {pokeGrave} from {targetUser.Username}'s Pokédex.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await FollowupAsync($"❌ Error: {ex.Message}");
            }
        }




        [SlashCommand("pokedex", "View a user's Pokédex")]
        public async Task PokedexCommand(
            [Summary("user", "Leave blank to view your own Pokédex")] SocketUser? user = null,

            [Summary("generation", "Select a generation to view")]
    [Choice("Gen 1", "Gen 1")]
    [Choice("Gen 2", "Gen 2")]
    [Choice("Gen 3", "Gen 3")]
    [Choice("Gen 4", "Gen 4")]
    [Choice("Gen 5", "Gen 5")]
    [Choice("Gen 6", "Gen 6")]
    [Choice("Gen 7", "Gen 7")]
    [Choice("Gen 8", "Gen 8")]
    [Choice("Gen 9", "Gen 9")]
    string? generation = null,

            int page = 1,
            bool showAll = false)
        {
            await DeferAsync();

            try
            {
                // Determine user
                var targetUser = user ?? Context.User;
                var userId = targetUser.Id;

                // Define generation ranges
                var generationRanges = new Dictionary<string, (int start, int end)>
                {
                    ["Gen 1"] = (1, 151),
                    ["Gen 2"] = (152, 251),
                    ["Gen 3"] = (252, 386),
                    ["Gen 4"] = (387, 493),
                    ["Gen 5"] = (494, 649),
                    ["Gen 6"] = (650, 721),
                    ["Gen 7"] = (722, 809),
                    ["Gen 8"] = (810, 905),
                    ["Gen 9"] = (906, 1025)
                };

                // Get all caught Pokémon
                var allCaught = _dbHelper.GetPokemon(userId);
                var caughtList = allCaught;

                // Apply generation filter if selected
                int generationStart = 1, generationEnd = 1025;
                if (generation != null && generationRanges.TryGetValue(generation, out var range))
                {
                    generationStart = range.start;
                    generationEnd = range.end;

                    caughtList = allCaught
                        .Where(p =>
                        {
                            var match = Regex.Match(p.name, @"#(\d+)");
                            if (!match.Success) return false;
                            int id = int.Parse(match.Groups[1].Value);
                            return id >= range.start && id <= range.end;
                        })
                        .ToList();
                }

                int totalCaught = caughtList.Count;
                int totalPokemon = showAll ? 1025 : (generationEnd - generationStart + 1);
                int perPage = 25;
                int maxPage = (int)Math.Ceiling(totalPokemon / (double)perPage);
                page = Math.Clamp(page, 1, maxPage);

                // Generate images
                var dexImage = GeneratePokedexPage(userId, caughtList, page, perPage, targetUser.GetAvatarUrl(), showAll);
                dexImage.Position = 0;

                var progressImage = GenerateProgressBarImage(totalCaught, totalPokemon);
                progressImage.Position = 0;

                // Embed with progress bar image
                var embed = new EmbedBuilder()
                    .WithTitle($"{targetUser.Username}'s Pokédex (Page {page}/{maxPage})")
                    .WithColor((Discord.Color)Color.Orange)
                    .WithImageUrl("attachment://progress_bar.png")
                    .WithFooter("Use /pokedex to browse more.")
                    .Build();

                var attachments = new List<FileAttachment>
        {
            new(dexImage, $"pokedex_page_{page}.png"),
            new(progressImage, "progress_bar.png")
        };

                await FollowupWithFilesAsync(
                    attachments: attachments,
                    embed: embed
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await FollowupAsync($"❌ Error: {ex.Message}");
            }
        }

    }
}
