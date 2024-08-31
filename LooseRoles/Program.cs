using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using TheKrystalShip.Logging;

namespace TLC.LooseRoles;

public class Program
{
    private IConfiguration _configuration;
    private IServiceProvider _services;

    private readonly Logger<Program> _logger = new();

    private readonly DiscordSocketConfig _socketConfig = new()
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages
    };

    private readonly InteractionServiceConfig _interactionServiceConfig = new()
    {
        DefaultRunMode = RunMode.Async,
        LogLevel = LogSeverity.Debug,
        ThrowOnError = true
    };

    public static void Main(string[] args) =>
        new Program().RunAsync(args).GetAwaiter().GetResult();

    public async Task RunAsync(string[] args)
    {
        _configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceCollection = new ServiceCollection()
            .AddSingleton(_configuration)
            .AddSingleton(_socketConfig)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton(x => new InteractionService(
                x.GetRequiredService<DiscordSocketClient>(),
                _interactionServiceConfig
            ))
            .AddSingleton<InteractionHandler>();
            // .AddSingleton<RolesManager>();

        _services = serviceCollection.BuildServiceProvider();

        // Here we can initialize the service that will register and execute our commands
        await _services.GetRequiredService<InteractionHandler>()
            .InitializeAsync();

        var discordClient = _services.GetRequiredService<DiscordSocketClient>();

        discordClient.Log += (logMessage) =>
        {
            _logger.LogInformation(logMessage.ToString());
            return Task.CompletedTask;
        };
        
        discordClient.Ready += async () =>
        {
            await discordClient.SetGameAsync("over roles 👀", null, ActivityType.Watching);
        };

        await discordClient.LoginAsync(TokenType.Bot, _configuration["discord:token"]);
        await discordClient.StartAsync();

        // var rolesManager = _services.GetRequiredService<RolesManager>();
        // discordClient.ReactionAdded += rolesManager.OnReactionAdded;
        // discordClient.ReactionRemoved += rolesManager.OnReactionRemoved;

        // Never quit the program until manually forced to.
        await Task.Delay(Timeout.Infinite);
    }
}
