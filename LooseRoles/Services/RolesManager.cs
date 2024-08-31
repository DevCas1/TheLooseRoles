using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TheKrystalShip.Logging;

namespace TLC.LooseRoles;

public class RolesManager(IConfiguration configuration, DiscordSocketClient discordSocketClient)
{
    private readonly IConfiguration _configuration = configuration;
    private readonly DiscordSocketClient _discordClient = discordSocketClient;
    private readonly Logger<RolesManager> _logger = new();

    // Records reacion name and role ID pairings
    private Dictionary<string, ulong> configuredReactions = [];

    public void ReloadConfiguredRoles()
    {
        var roles = _configuration["discord:roleIds"];
        Console.WriteLine($"roles:\n{roles}");
        // configuredReactions
    }

    /// <summary>
    /// Updates user roles according to the received reaction.
    /// </summary>
    /// <param name="message">The message this reaction was added to.</param>
    /// <param name="channel">The channel containing the message where this reaction was added to.</param>
    /// <param name="reaction">The reaction added to the message.</param>
    /// <returns></returns>
    public async Task OnReactionAdded(
        Cacheable<IUserMessage, ulong> message, 
        Cacheable<IMessageChannel, ulong> channel, 
        SocketReaction reaction
    )
    {
        // If the reaction is added by the bot itself, no further action should be taken.
        if (reaction.UserId == _discordClient.CurrentUser.Id)
            return;

        // Get roleID associated with reaction-emote name
        ulong? roleToAdd = configuredReactions[reaction.Emote.Name];

        if (roleToAdd == null)
        {
            _logger.LogInformation($"Could not find a role associated with Emote name {reaction.Emote.Name}.");

            if (message.HasValue)
            {
                _logger.LogInformation($"Removing unconfigured reaction");
                await message.Value.RemoveReactionAsync(reaction.Emote, reaction.User.Value);
            }
            return;
        }

        if (reaction.User.GetValueOrDefault() is not SocketGuildUser userToModify)
        {
            _logger.LogError($"Could not get retreive user from reaction!");
            return;
        }

        await userToModify.AddRoleAsync(roleToAdd.Value);
        // var guild = Context
        // await userToModify.AddRoleAsync;
    }

    /// <summary>
    /// Updates user roles according to the removed reaction.
    /// </summary>
    /// <param name="message">The message this reaction was added to.</param>
    /// <param name="channel">The channel containing the message where this reaction was added to.</param>
    /// <param name="reaction">The reaction added to the message.</param>
    /// <returns></returns>
    public async Task OnReactionRemoved(
        Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel,
        SocketReaction reaction
    )
    {
        // If the reaction is removed by the bot itself, no further action should be taken.
        if (reaction.UserId == _discordClient.CurrentUser.Id)
            return;

        // Get roleID associated with reaction-emote name
        ulong? roleToAdd = configuredReactions[reaction.Emote.Name];

        if (roleToAdd == null)
        {
            _logger.LogInformation($"Could not find a role associated with Emote name {reaction.Emote.Name}.");
            return;
        }

        if (reaction.User.GetValueOrDefault() is not SocketGuildUser userToModify)
        {
            _logger.LogError($"Could not get retreive user from reaction!");
            return;
        }

        await userToModify.RemoveRoleAsync(roleToAdd.Value);
    }
}
