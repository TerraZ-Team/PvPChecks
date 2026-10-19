using Microsoft.Xna.Framework;
using Terraria;
using TShockAPI;
using TShockAPI.Configuration;

namespace PvPChecks;

/// <summary>Изменение списков ограничений и отображение запрещённого контента.</summary>
internal sealed class BanCommandHandler
{
    private readonly ConfigFile<Config> cfg;
    private readonly string configPath;

    public BanCommandHandler(ConfigFile<Config> config, string path)
    {
        cfg = config;
        configPath = path;
    }

        public void BanItem(CommandArgs args)
        {
            TSPlayer plr = args.Player;

            if (args.Parameters.Count != 2)
            {
                plr.SendErrorMessage("Usage: /banitem <add/del> <item name/ID>");
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    List<Item> foundAddItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).Where(i => i.ammo == 0 || i.type == 3384).ToList();

                    if (foundAddItems.Count == 1)
                    {
                        Item i = foundAddItems[0];

                        if (i.accessory)
                        {
                            if (!cfg.Settings.accsBans.Contains(i.type))
                            {
                                cfg.Settings.accsBans.Add(i.type);
                            }
                        }
                        else if (i.headSlot >= 0 || i.bodySlot >= 0 || i.legSlot >= 0) //armor
                        {
                            if (!cfg.Settings.armorBans.Contains(i.type))
                            {
                                cfg.Settings.armorBans.Add(i.type);
                            }
                        }
                        else if (i.damage > 0 || i.type == 3384) //weapon
                        {
                            if (!cfg.Settings.weaponBans.Contains(i.type))
                            {
                                cfg.Settings.weaponBans.Add(i.type);
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage("No items found by that name/ID.");
                            break;
                        }
                        cfg.Write(configPath);
                        args.Player.SendSuccessMessage("Banned {0} in pvp.", i.Name);
                    }
                    else if (foundAddItems.Count > 1)
                    {
						IEnumerable<string> itemNames = from foundItem in foundAddItems select TShock.Utils.GetItemById(foundItem.type).Name;
						PaginationTools.SendPage(plr, 0, PaginationTools.BuildLinesFromTerms(itemNames),
						new PaginationTools.Settings
                        {
                            HeaderTextColor = Color.Red,
                            IncludeFooter = false,
                            HeaderFormat = "More than one item found:"
                        });
                    }
                    else
                    {
                        plr.SendErrorMessage("No items found by that name/ID.");
                    }
                    break;

                case "del":
                    List<Item> foundDelItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).Where(i => (cfg.Settings.weaponBans.Contains(i.type) || cfg.Settings.accsBans.Contains(i.type) || cfg.Settings.armorBans.Contains(i.type)) && i.ammo == 0).ToList();

                    if (foundDelItems.Count == 1)
                    {
                        Item i = foundDelItems[0];

                        if (cfg.Settings.weaponBans.Remove(i.type) || cfg.Settings.accsBans.Remove(i.type) || cfg.Settings.armorBans.Remove(i.type))
                        {
                            cfg.Write(configPath);
                            args.Player.SendSuccessMessage("Unbanned {0} in pvp.", i.Name);
                        }
                    }
                    else if (foundDelItems.Count > 1)
                    {
						IEnumerable<string> itemNames = from foundItem in foundDelItems select TShock.Utils.GetItemById(foundItem.type).Name;
                        PaginationTools.SendPage(plr, 0, PaginationTools.BuildLinesFromTerms(itemNames),
                        new PaginationTools.Settings
                        {
                            HeaderTextColor = Color.Red,
                            IncludeFooter = false,
                            HeaderFormat = "More than one item found:"
                        });
                    }
                    else
                    {
                        plr.SendErrorMessage("No items found by that name/ID in ban list.");
                    }
                    break;

                default:
                    plr.SendErrorMessage("Invalid syntax! /banitem <add/del> <item name/ID>");
                    break;
            }
        }

        public void BanBuff(CommandArgs args)
        {
            TSPlayer plr = args.Player;

            if (args.Parameters.Count != 2)
            {
                plr.SendErrorMessage("Usage: /banbuff <add/del> <buff name/ID>");
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    int addid;
                    if (!int.TryParse(args.Parameters[1], out addid))
                    {
                        var found = TShock.Utils.GetBuffByName(args.Parameters[1]);
                        if (found.Count == 0)
                        {
                            plr.SendErrorMessage("No buffs found by that name/ID.");
                            return;
                        }
                        else if (found.Count > 1)
                        {
                            IEnumerable<string> buffNames = from foundBuff in found select TShock.Utils.GetBuffName(foundBuff);
                            PaginationTools.SendPage(plr, 0, PaginationTools.BuildLinesFromTerms(buffNames),
                                new PaginationTools.Settings
                                {
                                    HeaderTextColor = Color.Red,
                                    IncludeFooter = false,
                                    HeaderFormat = "More than one buff found:"
                                });
                            return;
                        }
                        addid = found[0];
                    }

                    if (addid > 0 && addid < Terraria.ID.BuffID.Count)
                    {
                        if (!cfg.Settings.buffBans.Contains(addid))
                        {
                            cfg.Settings.buffBans.Add(addid);
                            cfg.Write(configPath);
                        }
                        args.Player.SendSuccessMessage("Banned {0} in pvp.", Lang.GetBuffName(addid));
                    }
                    else
                    {
                        plr.SendErrorMessage("Invalid buff ID.");
                    }
                    break;

                case "del":
                    int delid;
                    if (!int.TryParse(args.Parameters[1], out delid))
                    {
                        var found = TShock.Utils.GetBuffByName(args.Parameters[1]).Where(b => cfg.Settings.buffBans.Contains(b)).ToList();
                        if (found.Count == 0)
                        {
                            plr.SendErrorMessage("No buffs found by that name/ID in ban list.");
                            return;
                        }
                        else if (found.Count > 1)
                        {
                            IEnumerable<string> buffNames = from foundBuff in found select TShock.Utils.GetBuffName(foundBuff);
                            PaginationTools.SendPage(plr, 0, PaginationTools.BuildLinesFromTerms(buffNames),
                                new PaginationTools.Settings
                                {
                                    HeaderTextColor = Color.Red,
                                    IncludeFooter = false,
                                    HeaderFormat = "More than one buff found:"
                                });
                            return;
                        }
                        delid = found[0];
                    }

                    if (delid > 0 && delid < Terraria.ID.BuffID.Count)
                    {
                        if (cfg.Settings.buffBans.Contains(delid))
                        {
                            cfg.Settings.buffBans.Remove(delid);
                            cfg.Write(configPath);
                            args.Player.SendSuccessMessage("Unbanned {0} in pvp.", Lang.GetBuffName(delid));
                            break;
                        }
                        plr.SendErrorMessage("No buffs found by that name/ID in ban list.");
                    }
                    else
                    {
                        plr.SendErrorMessage("Invalid buff ID.");
                    }
                    break;

                default:
                    plr.SendErrorMessage("Invalid syntax! /banbuff <add/del> <buff name/ID>");
                    break;
            }
        }

        public void BanProj(CommandArgs args)
        {
            TSPlayer plr = args.Player;

            if (args.Parameters.Count != 2)
            {
                plr.SendErrorMessage("Usage: /banproj <add/del> <projectile ID>");
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    int addid;
                    if (int.TryParse(args.Parameters[1], out addid) && addid > 0 && addid <= Terraria.ID.ProjectileID.Count)
                    {
                        if (!cfg.Settings.projBans.Contains(addid))
                        {
                            cfg.Settings.projBans.Add(addid);
                            cfg.Write(configPath);
                        }
                        args.Player.SendSuccessMessage("Banned projectile {0} in pvp.", addid);
                        break;
                    }
                    plr.SendErrorMessage("Invalid projectile ID.");
                    break;

                case "del":
                    int delid;
                    if (int.TryParse(args.Parameters[1], out delid) && delid > 0 && delid <= 950)
                    {
                        if (cfg.Settings.projBans.Contains(delid))
                        {
                            cfg.Settings.projBans.Remove(delid);
                            cfg.Write(configPath);
                        }
                        args.Player.SendSuccessMessage("Unbanned projectile {0} in pvp.", delid);
                        break;
                    }
                    plr.SendErrorMessage("Invalid projectile ID.");
                    break;

                default:
                    plr.SendErrorMessage("Invalid syntax! /banproj <add/del> <projectile ID>");
                    break;
            }
        }

        public void PvPItemBans(CommandArgs args)
        {
            int pageNumber;
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                return;
            IEnumerable<string> itemNames = from itemBan in cfg.Settings.weaponBans.Concat(cfg.Settings.armorBans).Concat(cfg.Settings.accsBans)
                                            select TShock.Utils.GetItemById(itemBan).Name;
            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(itemNames, maxCharsPerLine: 75),
                new PaginationTools.Settings
                {
                    HeaderFormat = "The following items cannot be used in PvP:",
                    FooterFormat = "Type /pvpitembans {0} for more.",
                    NothingToDisplayString = "There are currently no banned items."
                });
        }
        public void PvPBuffBans(CommandArgs args)
        {
            int pageNumber;
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                return;
            IEnumerable<string> buffNames = from buffBan in cfg.Settings.buffBans
                                            select TShock.Utils.GetBuffName(buffBan);
            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(buffNames, maxCharsPerLine: 75),
                new PaginationTools.Settings
                {
                    HeaderFormat = "The following buffs cannot be used in PvP:",
                    FooterFormat = "Type /pvpbuffbans {0} for more.",
                    NothingToDisplayString = "There are currently no banned buffs."
                });
        }
        public void PvPProjBans(CommandArgs args)
        {
            int pageNumber;
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                return;
            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(cfg.Settings.projBans, maxCharsPerLine: 75),
                new PaginationTools.Settings
                {
                    HeaderFormat = "The following projectiles cannot be used in PvP:",
                    FooterFormat = "Type /pvpprojbans {0} for more.",
                    NothingToDisplayString = "There are currently no banned projectiles."
                });
        }
}
