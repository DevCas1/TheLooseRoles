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

    private ulong[] loadedRoleIDs = [];

    private bool LoadConfiguredRoleIDs()
    {
        loadedRoleIDs = _configuration.GetSection("discord:roleIds")
                                      .GetChildren()
                                      .Select(role =>
                                      {
                                          if (ulong.TryParse(role.Value, out ulong parsedRoleID))
                                              return (ulong?)parsedRoleID;
                                          else
                                          {
                                              _logger.LogInformation($"Could not parse configured role ID \"{role.Value}\" to ulong, skipping entry.");
                                              return null;
                                          }
                                      })
                                      .Where(id => id.HasValue)
#pragma warning disable CS8629 // Nullable value type may be null.
                                      .Select(id => id.Value)
#pragma warning restore CS8629 // id.Value cannot be null due to Where(id => id.HasValue) above it, compiler disagrees
                                      .ToArray();

        _logger.LogInformation($"Parsed the following configured roles: [{string.Join(", ", loadedRoleIDs)}]");

        return true;
    }

    [SlashCommand("show-configured-roles", "FOR TESTING PURPOSES, MAY BE REMOVED AT SOME POINT!")]
    public async Task ShowConfiguredRoles()
    {
        if (loadedRoleIDs.Length == 0)
        {
            LoadConfiguredRoleIDs();

            if (loadedRoleIDs.Length == 0)
            {
                await RespondAsync("Configured roles could not be loaded! Aborting command.", ephemeral: true);
                return;
            }
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            // .WithAuthor(Context.User.ToString(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithTitle("Configured Roles")
            .WithDescription(
                loadedRoleIDs.Length + " roles found\n" +
                string.Join("\n  ", loadedRoleIDs.Select(x => $"{Context.Guild.GetRole(x).Mention} (ID: {x})")))
            .WithColor(Color.Purple)
            .WithCurrentTimestamp();

        await RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
    }

    [SlashCommand("send-self-assign-message", "Sends a message to this channel with buttons to self assign configured roles")]
    public async Task SendSelfAssignRolesMessage()
    {
        await RespondAsync("Sending self-assign message...", ephemeral: true);

        if (loadedRoleIDs.Length == 0)
        {
            LoadConfiguredRoleIDs();

            if (loadedRoleIDs.Length == 0)
            {
                await RespondAsync("Configured roles could not be loaded! Aborting command.", ephemeral: true);
                return;
            }
        }

        ComponentBuilder componentBuilder = new ComponentBuilder();

        foreach (ulong role in loadedRoleIDs)
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
                string.Join("\n\t", loadedRoleIDs.Select(x => Context.Guild.GetRole(x).Mention))
            )
            .WithColor(Color.Gold);
            // .WithCurrentTimestamp();

        await ReplyAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction(ROLE_BUTTON_ID_WITH_WILDCARD)]
    public async Task RoleButtonPressed(string role_ID) // Accepts remainder of wildcard, which is the Role ID
    {
        ulong roleID = ulong.Parse(role_ID);

        if (Context.User is not SocketGuildUser user)
        {
            string message = $"Could not cast user {Context.User.GlobalName} to `SocketGuildUser`!";
            _logger.LogError(message);
            await RespondAsync(message);
            return;
        }

        bool userAlreadyHasRole = user.Roles.Any(x => x.Id == roleID);
        SocketRole role = Context.Guild.GetRole(roleID);

        if (userAlreadyHasRole == false)
        {
            await user.AddRoleAsync(roleID);
            await RespondAsync($"Role {role.Mention} added to {user.Mention}", ephemeral: true);
        }
        else
        {
            await user.RemoveRoleAsync(roleID);
            await RespondAsync($"Role {role.Mention} removed from {user.Mention}", ephemeral: true);
        }
    }
}
