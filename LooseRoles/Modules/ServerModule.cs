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
    private const string BUTTON_ID_1 = "button-id-1";
    private const string BUTTON_ID_2 = "button-id-2";
    private const string BUTTON_ID_3 = "button-id-3";
    // private const string SELF_ASSIGN_MESSAGE_ID_TOKEN = "discord:selfAssignMessageId";

    private readonly InteractionHandler _handler = handler;
    private readonly IConfiguration _configuration = configuration;
    private readonly Logger<ServerModule> _logger = new();
    
    private ulong[] configuredRoles = [];

    // [SlashCommand("get-self-assign-message-id", "Should return stored message ID if present")]
    // public async Task GetSelfAssignMessageId()
    // {
    //     var selfAssignMessageId = _configuration[SELF_ASSIGN_MESSAGE_ID_TOKEN];
    //     var logMessage = $"\"_configuration[SELF_ASSIGN_MESSAGE_ID_TOKEN]\" = \"{selfAssignMessageId}\"";

    //     _logger.LogInformation(logMessage);
    //     await RespondAsync(logMessage);
    // }

    [SlashCommand("show-discord-settings", "FOR TESTING PURPOSES, WILL BE REMOVED!")]
    private async Task ShowDiscordSettings()
    {
        if (Context.User.Id != (await Context.Client.GetApplicationInfoAsync()).Owner.Id)
        {
            await RespondAsync("You are not my owner, and therefore are not entitled to using this command.");
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(Context.User.ToString(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithTitle("Discord JSON content:")
            .WithDescription(_configuration["discord"] ?? "Nothing to show :(")
            .WithColor(Color.Purple)
            .WithCurrentTimestamp();

        await RespondAsync(embed: embedBuilder.Build(), ephemeral: true);
    }

    [SlashCommand("load-configured-roles", "FOR TESTING PURPOSES, WILL BE REMOVED!")]
    private async Task LoadConfiguredRoles()
    {
        string? foundRoles = _configuration["discord:roleIds"];
        await RespondAsync($"foundRoles = {foundRoles}");

        // return true;
    }

    [SlashCommand("send-self-assign-message", "Sends a message to this channel with buttons to self assign configured roles")]
    public async Task SendSelfAssignRolesMessage()
    {
        await RespondAsync("Sending self-assign message...", ephemeral: true); // ephemeral true means only the initiator can see the message

        if (configuredRoles.Length == 0)
        {
            await LoadConfiguredRoles();

            if (configuredRoles.Length == 0)
            {
                await RespondAsync("Configured roles could not be loaded! Aborting command.", ephemeral: true);
            }
        }

        EmbedBuilder embedBuilder = new EmbedBuilder()
            .WithAuthor(Context.User.ToString(), Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl())
            .WithTitle("Sign up for squad mentions!")
            .WithDescription("Click on one of the reactions below this message to assign or unassign the corresponding squad-role to or from yourself.\nMostly used to mention specific squads ") // TODO: Add roles in question to embed as well
            .WithColor(Color.Gold);
        // .WithCurrentTimestamp();

        var componentBuilder = new ComponentBuilder()
            .WithButton("Label-1", BUTTON_ID_1)
            .WithButton("Label-2", BUTTON_ID_2)
            .WithButton("Label-3", BUTTON_ID_3);

        // _configuration[SELF_ASSIGN_MESSAGE_ID_TOKEN] = (await Context.Channel.SendMessageAsync(embed: embedBuilder.Build())).Id.ToString();
        // _logger.LogInformation($"\"{SELF_ASSIGN_MESSAGE_ID_TOKEN}\" is assigned \"{_configuration[SELF_ASSIGN_MESSAGE_ID_TOKEN]}\"");

        // await Respond

        // Context.Client.ButtonExecuted += OnButtonPressed;

        await ReplyAsync(embed: embedBuilder.Build(), components: componentBuilder.Build());
    }

    [ComponentInteraction(BUTTON_ID_1)]
    public async Task Button1()
    {
        await RespondAsync($"Button with customID {BUTTON_ID_1} clicked", ephemeral: true);
    }

    [ComponentInteraction(BUTTON_ID_2)]
    public async Task Button2()
    {
        await RespondAsync($"Button with customID {BUTTON_ID_2} clicked", ephemeral: true);
    }

    // public async Task OnButtonPressed(SocketMessageComponent component)
    // {
    //     await ReplyAsync($"component.Data.CustomId = {component.Data.CustomId}");
    //     // switch(component.Data.CustomId)
    //     // {
    //     //     case BUTTON_ID_1:
    //     //         await component.RespondAsync($"{component.User.Mention} clicked button 1");
    //     //         break;
    //     //     case BUTTON_ID_2:
    //     //         await component.RespondAsync($"{component.User.Mention} clicked button 2");
    //     //         break;
    //     // }
    // }

    // [SlashCommand("add-self-assign-role", "Read the command ya dummy")]
    // public async Task AddSelfAssignRole(
    //     string emoteName,
    //     ulong roleId
    // )
    // {
    //     if (_configuration[SELF_ASSIGN_MESSAGE_ID_TOKEN] == null)
    //     {
    //         await SendSelfAssignRolesMessage();
    //     }


    // }

    // [SlashCommand("remove-self-assign-role", "Read the command ya dummy")]
    // public async Task RemoveSelfAssignRole(
    //     string emoteName
    // )
    // {
    //     await Task.Run(() => _logger.LogError("RemoveSelfAssignRole() is not implemented yet!"));
    // }
}
