using PvPChecks;

int checks = 0;
void Verify(bool expected, string? entered, IEnumerable<string>? blocked, IEnumerable<string>? aliases = null)
{
    bool actual = CommandBanPolicy.IsBlocked(entered, blocked, aliases);
    if (expected != actual)
        throw new Exception($"Expected {expected} for '{entered ?? "<null>"}', got {actual}");
    checks++;
}

Verify(true, "heal", ["heal"]);
Verify(true, "HEAL", ["heal"]);
Verify(true, "/heal", ["heal"]);
Verify(true, "h", ["heal"], ["h", "heal"]);
Verify(true, "heal", ["/HEAL"]);
Verify(true, " home ", ["home"]);
Verify(false, "home", ["heal"]);
Verify(false, "heal", []);
Verify(false, null, ["heal"]);
Verify(false, "heal", null);
Verify(false, "h", ["heal"], ["h", "home"]);
Verify(false, "heal", [""]);
Console.WriteLine($"Passed {checks} PvP command-ban policy checks.");
