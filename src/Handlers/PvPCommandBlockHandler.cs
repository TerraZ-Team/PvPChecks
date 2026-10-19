using TShockAPI;
using TShockAPI.Configuration;
using TShockAPI.Hooks;

namespace PvPChecks;

/// <summary>Перехват запрещённых команд игрока при включённом PvP.</summary>
internal sealed class PvPCommandBlockHandler
{
    private readonly ConfigFile<Config> cfg;

    public PvPCommandBlockHandler(ConfigFile<Config> config) => cfg = config;

    public void OnPlayerCommand(PlayerCommandEventArgs args)
    {
        if (args.Handled || !args.Player.RealPlayer || !args.Player.Active ||
            args.Player.TPlayer is null || !args.Player.TPlayer.hostile ||
            !cfg.Settings.blockCommandsInPvP)
            return;

        if (args.Player.HasPermission("pvpchecks.ignore") ||
            args.Player.HasPermission("pvpchecks.commands.bypass"))
            return;

        string invoked = args.CommandName;
        Command? matched = Commands.ChatCommands.FirstOrDefault(
            command => command.Names.Any(name =>
                string.Equals(name, invoked, StringComparison.OrdinalIgnoreCase)));

        if (!CommandBanPolicy.IsBlocked(invoked, cfg.Settings.commandBans, matched?.Names))
            return;

        args.Handled = true;
        args.Player.SendErrorMessage("Эту команду нельзя использовать при включённом PvP.");
    }
}
