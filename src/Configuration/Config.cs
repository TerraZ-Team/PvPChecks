namespace PvPChecks;

/// <summary>Поля совместимы со старым файлом pvpchecks.json.</summary>
public sealed class Config
{
    public List<int> weaponBans = new();
    public List<int> accsBans = new();
    public List<int> armorBans = new();
    public List<int> buffBans = new();
    public List<int> projBans = new();
    public bool portalGunBlock = true;

    public bool blockCommandsInPvP = true;
    public List<string> commandBans = new() { "heal" };
}
