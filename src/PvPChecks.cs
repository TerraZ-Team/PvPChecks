using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Configuration;
using TShockAPI.Hooks;

namespace PvPChecks;

/// <summary>Точка входа и жизненный цикл обработчиков.</summary>
[ApiVersion(2, 1)]
public sealed class PvPChecks : TerrariaPlugin
{
    private readonly string configPath = Path.Combine(TShock.SavePath, "pvpchecks.json");
    private ConfigFile<Config> cfg = null!;
    private PvPEnforcementHandler enforcement = null!;
    private PvPCommandBlockHandler commandBlock = null!;
    private BanCommandHandler banCommands = null!;
    private readonly List<Command> registeredCommands = new();

    public override string Name => "PvPChecks";
    public override string Author => "Johuan & Veelnyr & AgaSpace";
    public override string Description => "PvP restrictions for items, combat and commands.";
    public override Version Version => new(1, 1, 0, 0);

    public PvPChecks(Main game) : base(game) { }

    public override void Initialize()
    {
        cfg = new ConfigFile<Config>();
        cfg.Read(configPath, out _);
        cfg.Write(configPath); // Добавить новые настройки в существующие конфиги.

        enforcement = new PvPEnforcementHandler(cfg);
        commandBlock = new PvPCommandBlockHandler(cfg);
        banCommands = new BanCommandHandler(cfg, configPath);

        GetDataHandlers.PlayerUpdate += enforcement.OnPlayerUpdate;
        GetDataHandlers.Teleport += enforcement.OnTeleport;
        GetDataHandlers.TogglePvp += enforcement.OnTogglePvp;
        GetDataHandlers.PlayerDamage += enforcement.OnPlayerDamage;
        ServerApi.Hooks.NetSendData.Register(this, enforcement.OnSendData);
        ServerApi.Hooks.NetGetData.Register(this, enforcement.OnGetData);
        PlayerHooks.PlayerCommand += commandBlock.OnPlayerCommand;

        Register(new Command(banCommands.PvPItemBans, "pvpitembans"));
        Register(new Command(banCommands.PvPBuffBans, "pvpbuffbans"));
        Register(new Command(banCommands.PvPProjBans, "pvpprojbans"));
        Register(new Command("pvpchecks.ban", banCommands.BanItem, "banitem"));
        Register(new Command("pvpchecks.ban", banCommands.BanBuff, "banbuff"));
        Register(new Command("pvpchecks.ban", banCommands.BanProj, "banproj"));
    }

    private void Register(Command command)
    {
        Commands.ChatCommands.Add(command);
        registeredCommands.Add(command);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && enforcement is not null)
        {
            GetDataHandlers.PlayerUpdate -= enforcement.OnPlayerUpdate;
            GetDataHandlers.Teleport -= enforcement.OnTeleport;
            GetDataHandlers.TogglePvp -= enforcement.OnTogglePvp;
            GetDataHandlers.PlayerDamage -= enforcement.OnPlayerDamage;
            ServerApi.Hooks.NetSendData.Deregister(this, enforcement.OnSendData);
            ServerApi.Hooks.NetGetData.Deregister(this, enforcement.OnGetData);
            PlayerHooks.PlayerCommand -= commandBlock.OnPlayerCommand;
            foreach (Command command in registeredCommands)
                Commands.ChatCommands.Remove(command);
            registeredCommands.Clear();
        }
        base.Dispose(disposing);
    }
}
