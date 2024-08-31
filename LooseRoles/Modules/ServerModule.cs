using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using TheKrystalShip.Logging;

namespace TLC.LooseRoles.Modules;

// Interaction modules must be public and inherit from an IInteractionModuleBase
public partial class ServerModule(
    InteractionHandler handler,
    IConfiguration configuration
) : InteractionModuleBase<SocketInteractionContext>
{

    private const string ROLE_BUTTON_ID_BASE = "role_button_id_";
    private const string ROLE_BUTTON_ID_WITH_WILDCARD = ROLE_BUTTON_ID_BASE + "*";

    private readonly InteractionHandler _handler = handler;
    private readonly IConfiguration _configuration = configuration;
    private readonly Logger<ServerModule> _logger = new();

    private ulong[] configuredRoles = [];

    private bool LoadConfiguredRoles()
    {
        try
        {
            configuredRoles =
                _configuration
                    .GetSection("discord:roleIds")
                    .GetChildren()
                    .Select(x => ulong.Parse(x.Value)) //TODO: Fix possible null reference for x
                    .ToArray();
        }
        catch (InvalidCastException)
        {
            string message = "Could not cast results of to ulong!";
            _logger.LogError(message);
            // await RespondAsync(message, ephemeral: true);
            return false;
        }

        return true;
    }

    [SlashCommand("show-configured-roles", "FOR TESTING PURPOSES, MAY BE REMOVED AT SOME POINT!")]
    public async Task ShowConfiguredRoles()
    {
        if (configuredRoles.Length == 0)
        {
            LoadConfiguredRoles();

            if (configuredRoles.Length == 0)
            {
                await RespondAsync("Configured roles could not be loaded! Aborting command.", ephemeral: true);
                return;
            }
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(Context.User.ToString(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithTitle("Configured Roles")
            .WithDescription(
                configuredRoles.Length + " roles found\n" +
                string.Join("\n  ", configuredRoles.Select(x => $"{Context.Guild.GetRole(x).Mention} (ID: {x})")))
            .WithColor(Color.Purple)
            .WithCurrentTimestamp();

        await RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
    }

    [SlashCommand("send-self-assign-message", "Sends a message to this channel with buttons to self assign configured roles")]
    public async Task SendSelfAssignRolesMessage()
    {
        await RespondAsync("Sending self-assign message...", ephemeral: true);

        if (configuredRoles.Length == 0)
        {
            LoadConfiguredRoles();

            if (configuredRoles.Length == 0)
            {
                await RespondAsync("Configured roles could not be loaded! Aborting command.", ephemeral: true);
                return;
            }
        }

        ComponentBuilder componentBuilder = new ComponentBuilder();

        foreach (ulong role in configuredRoles)
        // for (int index = 0; index < configuredRoles.Length; index ++)
        {
            // ulong role = configuredRoles[index];

            componentBuilder.WithButton(
                Context.Guild.GetRole(role).Name,
                ROLE_BUTTON_ID_BASE + role
                // row: index
            );
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(Context.User.ToString(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithTitle("Sign up for squad mentions!")
            .WithDescription(
                "Click on one of the buttons to assign or unassign the corresponding squad-role to or from yourself." +
                // "\nMostly used to mention specific squads." +
                "\n\nAvailable Roles:\n" +
                string.Join("\n\t", configuredRoles.Select(x => Context.Guild.GetRole(x).Mention))
            )
            .WithColor(Color.Gold);
            // .WithCurrentTimestamp();

        await ReplyAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction(ROLE_BUTTON_ID_WITH_WILDCARD)]
    public async Task RoleButtonPressed(string role_id) // Accepts remainder of wildcard, which is the Role ID
    {
        ulong roleId = ulong.Parse(role_id);

        if (Context.User is not SocketGuildUser user)
        {
            string message = $"Could not cast user {Context.User.GlobalName} to `SocketGuildUser`!";
            _logger.LogError(message);
            await RespondAsync(message);
            return;
        }

        bool userAlreadyHasRole = user.Roles.Any(x => x.Id == roleId);
        SocketRole role = Context.Guild.GetRole(roleId);

        if (userAlreadyHasRole == false)
        {
            await user.AddRoleAsync(roleId);
            await RespondAsync($"Role {role.Mention} added to {user.Mention}", ephemeral: true);
        }
        else
        {
            await user.RemoveRoleAsync(roleId);
            await RespondAsync($"Role {role.Mention} removed from {user.Mention}", ephemeral: true);
        }
    }
}
