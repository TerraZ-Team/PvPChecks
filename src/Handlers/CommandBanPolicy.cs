namespace PvPChecks;

/// <summary>Чистое сопоставление команды с запретами, включая алиасы.</summary>
internal static class CommandBanPolicy
{
    public static bool IsBlocked(string? enteredCommand, IEnumerable<string>? blockedCommands,
        IEnumerable<string>? aliases = null)
    {
        if (string.IsNullOrWhiteSpace(enteredCommand) || blockedCommands is null)
            return false;

        var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? name in blockedCommands)
        {
            string normalized = Normalize(name);
            if (normalized.Length != 0)
                blocked.Add(normalized);
        }

        if (blocked.Count == 0)
            return false;

        if (blocked.Contains(Normalize(enteredCommand)))
            return true;

        return aliases?.Any(alias => blocked.Contains(Normalize(alias))) == true;
    }

    private static string Normalize(string? command)
        => (command ?? string.Empty).Trim().TrimStart('/', '.');
}
