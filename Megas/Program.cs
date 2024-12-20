using DSharpPlus;
using DSharpPlus.Entities;
using System.Text;

class Program
{
    static async Task Main(string[] args)
    {
        var discord = new DiscordClient(new DiscordConfiguration
        {
            Token = "MTMxOTY2MjYyOTAxODY2OTEyNg.GcxeaG.HrKyGFY83H1MBU38DXQInQt8Hd0_FVg6YXcnlc",
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All
        });

        var dungeonManager = new DungeonManager();

        // Adiciona comandos para dungeons
        dungeonManager.AddDungeon("!zerk", "Montando Grupo para Zerk", new Dictionary<string, (string, int)>
        {
            { "🛡️DA", ("Tank DA", 1) },
            { "⚔️DA", ("DPS DA", 1) },
            { "🛡️VA", ("Tank VA", 1) },
            { "⚔️VA", ("DPS VA", 1) }
        });

        dungeonManager.AddDungeon("!r13", "Montando Grupo para Colo Hero R13", new Dictionary<string, (string, int)>
        {
            { "⚔️", ("DPS", 4) }
        });

        dungeonManager.AddDungeon("!r4", "Montando Grupo para Colo Ex R4", new Dictionary<string, (string, int)>
        {
            { "🛡️", ("Tank", 1) },
            { "⚔️", ("DPS", 3) }
        });

        discord.MessageCreated += async (s, e) => await dungeonManager.HandleCommandAsync(e);
        discord.MessageReactionAdded += async (s, e) => await dungeonManager.HandleReactionAddedAsync(e);
        discord.MessageReactionRemoved += async (s, e) => await dungeonManager.HandleReactionRemovedAsync(e);

        await discord.ConnectAsync();
        await Task.Delay(-1);
    }
}

public class DungeonManager
{
    private readonly Dictionary<string, Dungeon> _dungeons = new();

    public void AddDungeon(string command, string title, Dictionary<string, (string Role, int Max)> roles)
    {
        _dungeons[command] = new Dungeon(title, roles);
    }

    public async Task HandleCommandAsync(DSharpPlus.EventArgs.MessageCreateEventArgs e)
    {
        if (_dungeons.ContainsKey(e.Message.Content.ToLower()))
        {
            var dungeon = _dungeons[e.Message.Content.ToLower()];
            var embed = dungeon.CreateEmbed();
            var message = await e.Message.RespondAsync(embed);

            // Adiciona reações
            foreach (var emoji in dungeon.Roles.Keys)
            {
                await message.CreateReactionAsync(DiscordEmoji.FromUnicode(emoji));
            }

            dungeon.MessageId = message.Id; // Salva o ID da mensagem para rastrear reações
        }
    }

    public async Task HandleReactionAddedAsync(DSharpPlus.EventArgs.MessageReactionAddEventArgs e)
    {
        if (e.User.IsBot) return;

        var dungeon = _dungeons.Values.FirstOrDefault(d => d.MessageId == e.Message.Id);
        if (dungeon != null)
        {
            var emoji = e.Emoji.GetDiscordName();
            if (dungeon.Roles.ContainsKey(emoji))
            {
                var result = dungeon.AddMember(emoji, e.User.Id);
                if (!string.IsNullOrEmpty(result))
                {
                    await e.Channel.SendMessageAsync(result);
                }
            }
        }
    }

    public async Task HandleReactionRemovedAsync(DSharpPlus.EventArgs.MessageReactionRemoveEventArgs e)
    {
        if (e.User.IsBot) return;

        var dungeon = _dungeons.Values.FirstOrDefault(d => d.MessageId == e.Message.Id);
        if (dungeon != null)
        {
            var emoji = e.Emoji.GetDiscordName();
            if (dungeon.Roles.ContainsKey(emoji))
            {
                var result = dungeon.RemoveMember(emoji, e.User.Id);
                if (!string.IsNullOrEmpty(result))
                {
                    await e.Channel.SendMessageAsync(result);
                }
            }
        }
    }
}

public class Dungeon
{
    public string Title { get; }
    public Dictionary<string, (string Role, int Max, List<ulong> Members)> Roles { get; } = new();
    public ulong MessageId { get; set; }

    public Dungeon(string title, Dictionary<string, (string Role, int Max)> roles)
    {
        Title = title;
        foreach (var role in roles)
        {
            Roles[role.Key] = (role.Value.Role, role.Value.Max, new List<ulong>());
        }
    }

    public DiscordEmbed CreateEmbed()
    {
        var description = new StringBuilder("Clique nas reações para entrar no grupo:\n\n");
        foreach (var role in Roles)
        {
            description.AppendLine($"{role.Key} - {role.Value.Role} ({role.Value.Members.Count}/{role.Value.Max} vagas)");
        }

        return new DiscordEmbedBuilder
        {
            Title = Title,
            Description = description.ToString(),
            Color = DiscordColor.Azure
        };
    }

    public string AddMember(string emoji, ulong userId)
    {
        var role = Roles[emoji];
        if (role.Members.Count < role.Max && !role.Members.Contains(userId))
        {
            role.Members.Add(userId);
            Roles[emoji] = (role.Role, role.Max, role.Members);
            return $"<@{userId}> entrou como **{role.Role}**! ({role.Members.Count}/{role.Max})";
        }
        else if (role.Members.Contains(userId))
        {
            return $"<@{userId}>, você já está no grupo como **{role.Role}**!";
        }
        else
        {
            return $"<@{userId}>, a vaga para **{role.Role}** já foi preenchida!";
        }
    }

    public string RemoveMember(string emoji, ulong userId)
    {
        var role = Roles[emoji];
        if (role.Members.Contains(userId))
        {
            role.Members.Remove(userId);
            Roles[emoji] = (role.Role, role.Max, role.Members);
            return $"<@{userId}> saiu da vaga de **{role.Role}**! ({role.Members.Count}/{role.Max})";
        }
        return string.Empty;
    }
}
