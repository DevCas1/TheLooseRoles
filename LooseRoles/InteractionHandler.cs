using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System.Reflection;

namespace TLC.LooseRoles;

public class InteractionHandler
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _handler;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public InteractionHandler(
        DiscordSocketClient client,
        InteractionService handler,
        IServiceProvider services,
        IConfiguration config
    )
    {
        _client = client;
        _handler = handler;
        _services = services;
        _configuration = config;
    }

    public async Task InitializeAsync()
    {
        // Process when the client is ready, so we can register our commands.
        _client.Ready += ReadyAsync;
        _handler.Log += LogAsync;

        // Add the public modules that inherit InteractionModuleBase<T> to the
        // InteractionService
        await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        // Process the InteractionCreated payloads to execute Interactions
        // commands
        _client.InteractionCreated += HandleInteraction;

        // Also process the result of the command execution.
        _handler.InteractionExecuted += HandleInteractionExecute;
    }

    private Task LogAsync(LogMessage log)
    {
        Console.WriteLine(log);
        return Task.CompletedTask;
    }

    private async Task ReadyAsync()
    {
        // Register the commands globally if no guildId is present in configuration.
        // Otherwise register commands only to configured guild.

        string? guildId = _configuration["discord:guildId"] ?? throw new NullReferenceException("discord:guildId is not set!");
        
        await _handler.RegisterCommandsToGuildAsync(
            ulong.Parse(guildId),
            true
        );
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            // Create an execution context that matches the generic type
            // parameter of your InteractionModuleBase<T> modules.
            var context = new SocketInteractionContext(_client, interaction);

            // Execute the incoming command.
            var result = await _handler.ExecuteCommandAsync(context, _services);

            // Due to async nature of InteractionFramework, the result here may
            // always be success.
            // That's why we also need to handle the InteractionExecuted event.
            if (!result.IsSuccess)
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        // TODO: implement
                        break;
                    default:
                        await interaction.RespondAsync(result.ErrorReason);
                        break;
                }
        }
        catch
        {
            // If Slash Command execution fails it is most likely that the
            // original interaction acknowledgement will persist.
            //
            // It is a good idea to delete the original response, or at least
            // let the user know that something went wrong during the command
            // execution.
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync()
                    .ContinueWith(async (message) => await message.Result.DeleteAsync());
        }
    }

    private async Task HandleInteractionExecute(
        ICommandInfo commandInfo,
        IInteractionContext context,
        IResult result
    )
    {
        if (!result.IsSuccess)
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    // TODO: implement
                    break;
                default:
                    await context.Interaction.RespondAsync(result.ErrorReason);
                    break;
            }
    }
}
