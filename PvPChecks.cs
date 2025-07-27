using IL.Terraria.ID;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Configuration;
using TShockAPI.Hooks;

namespace PvPChecks
{
    [ApiVersion(2, 1)]
    public class PvPChecks : TerrariaPlugin
    {
        private string configPath = Path.Combine(TShock.SavePath, "pvpchecks.json");
        private ConfigFile<Config> cfg;

        public override string Name => "PvPChecks";
        public override string Author => "Johuan & Veelnyr & AgaSpace & iBelarus";
        public override string Description => "Bans weapons, buffs, accessories, projectiles and disables PvPers from using illegitimate stuff.";
        public override Version Version => new(1, 0, 0, 3);

        public PvPChecks(Main game) : base(game) { }

        public static bool PortalGun;

        public static bool SolarArmorDebuff;

        public string[] DisabledCommandsInPvp = new string[]
        {
            "back"
        };
        public override void Initialize()
        {
            cfg = new ConfigFile<Config>();
            cfg.Read(configPath, out bool write);
            if (write)
                cfg.Write(configPath);

            PortalGun = cfg.Settings.portalGunBlock;
            SolarArmorDebuff = cfg.Settings.solarArmorDebuff;
            DisabledCommandsInPvp = cfg.Settings.disabledcommandsInPvp;

            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
			GetDataHandlers.Teleport += OnTeleport;
            GetDataHandlers.TogglePvp += OnTogglePvp;
            GetDataHandlers.PlayerDamage += OnPlayerDamage;
            GetDataHandlers.NewProjectile += OnNewProjectile;

            PlayerHooks.PlayerCommand += OnPlayerCommand;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);

            GeneralHooks.ReloadEvent += OnReload;

            Commands.ChatCommands.Add(new Command(PvPItemBans, "pvpitembans"));
            Commands.ChatCommands.Add(new Command(PvPBuffBans, "pvpbuffbans"));
            Commands.ChatCommands.Add(new Command(PvPProjBans, "pvpprojbans"));
            Commands.ChatCommands.Add(new Command("pvpchecks.ban", BanItem, "banitem"));
            Commands.ChatCommands.Add(new Command("pvpchecks.ban", BanBuff, "banbuff"));
            Commands.ChatCommands.Add(new Command("pvpchecks.ban", BanProj, "banproj"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
				GetDataHandlers.Teleport -= OnTeleport;
                GetDataHandlers.TogglePvp -= OnTogglePvp;
                GetDataHandlers.PlayerDamage -= OnPlayerDamage;
                GetDataHandlers.NewProjectile -= OnNewProjectile;

                PlayerHooks.PlayerCommand -= OnPlayerCommand;
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);

                GeneralHooks.ReloadEvent -= OnReload;
            }
            base.Dispose(disposing);
        }
        private async void OnReload(ReloadEventArgs args) // релоад @iBelarus
        {
            cfg = new ConfigFile<Config>();
            cfg.Read(configPath, out bool write);
            if (write)
                cfg.Write(configPath);

            PortalGun = cfg.Settings.portalGunBlock;
            SolarArmorDebuff = cfg.Settings.solarArmorDebuff;
            DisabledCommandsInPvp = cfg.Settings.disabledcommandsInPvp;

            args.Player.SendSuccessMessage("[PvPChecks] Reloaded PVP config!");
        }

        private void OnPlayerCommand(PlayerCommandEventArgs args) // Перенёс с Essentials+ сюда запрет команд в пвп
        {
            if (args.Handled || args.Player == null)
            {
                return;
            }

            Command command = args.CommandList.FirstOrDefault();
            if (command == null || (command.Permissions.Any() && !command.Permissions.Any(s => args.Player.Group.HasPermission(s))))
            {
                return;
            }

            if (args.Player.TPlayer.hostile &&
                command.Names.Select(s => s.ToLowerInvariant())
                    .Intersect(DisabledCommandsInPvp.Select(s => s.ToLowerInvariant()))
                    .Any())
            {
                args.Player.SendErrorMessage("This command is blocked while in PvP!");
                args.Handled = true;
                return;
            }
        }

        DateTime[] WarningMsgCooldown = new DateTime[256];
        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            TSPlayer player = TShock.Players[args.PlayerId];

            //If the player isn't in pvp or using an item, skip pvp checking

            // You announced the use of the item, but the armor is checked after.
            // I've moved the item use check to where it should be.
            if (player == null) return;
            if (!player.Active) return;
			if (!player.TPlayer.hostile) return;
            if (player.HasPermission("pvpchecks.ignore")) return;

            //Check armor
            for (int a = 0; a < 3; a++)
            {
                foreach (int armorBan in cfg.Settings.armorBans)
                {
                    if (player.TPlayer.armor[a].type == armorBan)
                    {
                        player.Disable("Used banned armor in pvp.", DisableFlags.None);
                        if ((DateTime.Now - WarningMsgCooldown[player.Index]).TotalSeconds > 3)
                        {
                            player.SendErrorMessage("[i:{0}] {1} cannot be used in PvP. See /pvpitembans.", armorBan, TShock.Utils.GetItemById(armorBan).Name);
                            WarningMsgCooldown[player.Index] = DateTime.Now;
                            player.SetPvP(false);
                        }
                        return;
                    }
                }
            }

            if (SolarArmorDebuff && player.TPlayer.armor[0].type == Terraria.ID.ItemID.SolarFlareHelmet && player.TPlayer.armor[1].type == Terraria.ID.ItemID.SolarFlareBreastplate && player.TPlayer.armor[2].type == Terraria.ID.ItemID.SolarFlareLeggings)
            {
                //player.SetBuff(36, 180); // Ослабление солнечной брони @iBelarus
                player.SetBuff(Terraria.ID.BuffID.WitheredArmor, 360); // Ослабление солнечной брони - 195 buff @iBelarus
            }

            //Check accs
            for (int a = 3; a < 10; a++)
            {
                foreach (int accBan in cfg.Settings.accsBans)
                {
                    if (player.TPlayer.armor[a].type == accBan)
                    {
                        player.Disable("Used banned accessory in pvp.", DisableFlags.None);
                        if ((DateTime.Now - WarningMsgCooldown[player.Index]).TotalSeconds > 3)
                        {
                            player.SendErrorMessage("[i:{0}] {1} cannot be used in PvP. See /pvpitembans.", accBan, TShock.Utils.GetItemById(accBan).Name);
                            WarningMsgCooldown[player.Index] = DateTime.Now;
                            player.SetPvP(false);
                        }
                        return;
                    }
                }
            }

            //Checks buffs
            foreach (int buff in cfg.Settings.buffBans)
            {
                foreach (int playerbuff in player.TPlayer.buffType)
                {
                    if (playerbuff == buff)
                    {
                        player.Disable("Used banned buff.", DisableFlags.None);
                        if ((DateTime.Now - WarningMsgCooldown[player.Index]).TotalSeconds > 3)
                        {
                            player.SendErrorMessage(TShock.Utils.GetBuffName(playerbuff) + " cannot be used in PvP. See /pvpbuffbans.");
                            WarningMsgCooldown[player.Index] = DateTime.Now;
                            player.SetPvP(false);
                        }
                        return;
                    }
                }
            }

            //Checks whether a player is wearing duplicate accessories/armor
            List<int> duplicate = new List<int>();
            foreach (Item equip in player.TPlayer.armor)
            {
                if (duplicate.Contains(equip.type))
                {
                    player.Disable("Used duplicate accessories.", DisableFlags.None);
                    if ((DateTime.Now - WarningMsgCooldown[player.Index]).TotalSeconds > 3)
                    {
                        player.SendErrorMessage("Please remove the duplicate accessory for PvP: " + equip.Name);
                        WarningMsgCooldown[player.Index] = DateTime.Now;
                        player.SetPvP(false);
                    }
                    return;
                }
                else if (equip.type != 0)
                {
                    duplicate.Add(equip.type);
                }
            }

            cfg.Settings.weaponBans.ForEach(delegate (ValueTuple<int, int, bool> weapon)
            {
                if (player.SelectedItem.type == weapon.Item1 && (weapon.Item2 != (int)player.SelectedItem.prefix || weapon.Item2 == 0) && weapon.Item3 && args.Control.IsUsingItem)
                {
                    player.Disable("Used banned weapon in pvp.", DisableFlags.None);
                    if ((DateTime.Now - WarningMsgCooldown[player.Index]).TotalSeconds > 3.0)
                    {
                        player.SendErrorMessage("[i:{0}] {1} is banned in PvP. See /pvpitembans.", weapon.Item1, TShock.Utils.GetItemById(weapon.Item1).Name); 
                        WarningMsgCooldown[player.Index] = DateTime.Now;
                    }
                }
            });
        }
		
		private void OnTeleport(object sender, GetDataHandlers.TeleportEventArgs args)
		{
			if (!args.Player.TPlayer.hostile) return;
			if (args.Player.HasPermission("pvpchecks.ignore")) return;
			
			args.Player.Disable("Used teleporting in pvp.", DisableFlags.None);
			args.Player.Teleport(args.Player.TPlayer.position.X, args.Player.TPlayer.position.Y);
			args.Player.SendErrorMessage("You can't teleport in pvp.");

            args.Player.SetPvP(false);
        }

        private void OnTogglePvp(object? sender, GetDataHandlers.TogglePvpEventArgs args)
        {
            for (int i = 0; i < Main.projectile.Length; i++)
            {
                if (Main.projectile[i]?.owner == args.Player.Index && Main.projectile[i].active)
                {
                    Main.projectile[i].type = 0;
                    Main.projectile[i].active = false;
                    NetMessage.SendData(27, -1, -1, null, i);
                }
            }

            if (SolarArmorDebuff && !args.Player.HasPermission("pvpchecks.ignore") && !args.Player.TPlayer.hostile && args.Player.TPlayer.armor[0].type == Terraria.ID.ItemID.SolarFlareHelmet && args.Player.TPlayer.armor[1].type == Terraria.ID.ItemID.SolarFlareBreastplate && args.Player.TPlayer.armor[2].type == Terraria.ID.ItemID.SolarFlareLeggings)
            {
                args.Player.SendErrorMessage("Solar Flare Armor ([i:2763][i:2764][i:2765]) applies a debuff Withered Armor (Defense is cut in half)."); // сообщение о дебаффе солнечной брони @iBelarus
            }
        }
        private void OnNewProjectile(object sender, GetDataHandlers.NewProjectileEventArgs args)
        {
            TSPlayer tsplayer = TShock.Players[args.Owner];
            if (tsplayer.TPlayer.hostile && !tsplayer.HasPermission("pvpchecks.ignore"))
            {
                //bool flag2 = cfg.Settings.projBans.Any((ValueTuple<int, bool> projectile) => projectile.Item1 == (int)args.Type && projectile.Item2);
                if (cfg.Settings.projBans.Any((ValueTuple<int, bool> projectile) => projectile.Item1 == (int)args.Type && projectile.Item2))
                {
                    tsplayer.Disable("Used banned projectile in pvp.", DisableFlags.None);
                    tsplayer.SendErrorMessage("Projectile " + args.Type.ToString() + " is banned in PvP. See /pvpprojbans.");
                    args.Player.RemoveProjectile(args.Identity, args.Owner);
                    args.Handled = true;
                }

                if (PortalGun && (args.Type == Terraria.ID.ProjectileID.PortalGunBolt || args.Type == Terraria.ID.ProjectileID.PortalGunGate)) // убирать снаряды портал гана @iBelarus
                {
                    args.Player.RemoveProjectile(args.Identity, args.Owner);
                    args.Handled = true;
                }
            }
        }

        private void OnPlayerDamage(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            if (args.ID != args.Player.Index && args.PlayerDeathReason != null)
            {
                if (args.PlayerDeathReason.SourceProjectileType.HasValue)
                {
                    int proj = args.PlayerDeathReason.SourceProjectileType.Value;
                    if (cfg.Settings.projBans.Any((ValueTuple<int, bool> projectile) => projectile.Item1 == proj && !projectile.Item2))
                    {
                        args.Player.SendData(PacketTypes.PlayerHp, "", args.ID);
                        args.Player.SendData(PacketTypes.PlayerUpdate, "", args.ID);
                        args.Handled = true;
                        //args.Player.SendErrorMessage("Projectile banned in pvp");
                    }
                }
                if (cfg.Settings.weaponBans.Any((ValueTuple<int, int, bool> item) => item.Item1 == args.PlayerDeathReason._sourceItemType && (item.Item2 != args.PlayerDeathReason._sourceItemPrefix || item.Item2 == 0) && !item.Item3))
                {
                    args.Player.SendData(PacketTypes.PlayerHp, "", args.ID);
                    args.Player.SendData(PacketTypes.PlayerUpdate, "", args.ID);
                    args.Handled = true;
                    args.Player.SendErrorMessage("[i:{0}] {1} is banned in PvP. It deals 0 damage! See /pvpitembans.", args.PlayerDeathReason._sourceItemType, TShock.Utils.GetItemById(args.PlayerDeathReason._sourceItemType).Name);
                }
            }
        }

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.MsgId == PacketTypes.PlayerAddBuff)
            {
                if (TShock.Players[args.number].TPlayer.hostile && args.number2 != 149f && args.number2 != 195f) //это уже было, типо запрещает баффы накидывать командой во время пвп. 149 и 195 - это окаменение (окаменение за запретку) и withered armor (SolarArmorDebuff)
                {
                    args.Handled = true;
                }
            }
        }
        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.SyncLoadout)
            {
                TSPlayer player = TShock.Players[args.Msg.whoAmI];
                if (player.TPlayer.hostile)
                {
                    player.SetPvP(false, true);
                    player.SendErrorMessage("Loadout swapping is not allowed in PvP.");
                }
            }

            if (PortalGun && args.MsgID == PacketTypes.PlayerTeleportPortal && TShock.Players[args.Msg.whoAmI].TPlayer.hostile)
            {
                TShock.Players[args.Msg.whoAmI].SetPvP(false, true);
                TShock.Players[args.Msg.whoAmI].SendErrorMessage("Portal Gun is not allowed in PvP."); // Запрет на телепорт в порталгановские порталы @iBelarus
            }
        }

        private void BanItem(CommandArgs args)
        {
            TSPlayer plr = args.Player;

            if (args.Parameters.Count <= 1 || args.Parameters.Count > 4)
            {
                plr.SendErrorMessage("Usage: /banitem <add/del> <item name/ID> [c/38bf38:<prefix name/ID> <true/false> (for weapons)]");
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    //List<Item> foundAddItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).Where(i => i.ammo == 0 || i.type == 3384).ToList();

                    List<Item> foundAddItems = (from i in TShock.Utils.GetItemByIdOrName(args.Parameters[1])
                                                where i.ammo == 0 || i.type == 3384
                                                select i).ToList<Item>();

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
                            int prefix = 0;
                            bool froze = false;
                            if (args.Parameters.Count >= 3)
                            {
                                List<int> prefixByIdOrName = TShock.Utils.GetPrefixByIdOrName(args.Parameters[2]);
                                if (prefixByIdOrName.Count == 1)
                                {
                                    prefix = prefixByIdOrName[0];
                                }
                                if (args.Parameters.Count > 3)
                                {
                                    froze = args.Parameters[3].Equals("true", StringComparison.OrdinalIgnoreCase);
                                }
                            }

                            if (!cfg.Settings.weaponBans.Any((ValueTuple<int, int, bool> item) => item.Item1 == i.type))
                            {
                                cfg.Settings.weaponBans.Add(new ValueTuple<int, int, bool>(i.type, prefix, froze));
                            }
                        }
                        else
                        {
                            plr.SendErrorMessage("No items found by that name/ID.");
                            break;
                        }
                        cfg.Write(configPath);
                        args.Player.SendSuccessMessage("Banned {0} in PvP.", i.Name);
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
                    //List<Item> foundDelItems = TShock.Utils.GetItemByIdOrName(args.Parameters[1]).Where(i => (cfg.Settings.weaponBans.Contains(i.type) || cfg.Settings.accsBans.Contains(i.type) || cfg.Settings.armorBans.Contains(i.type)) && i.ammo == 0).ToList();
                    List<Item> foundDelItems = (from i in TShock.Utils.GetItemByIdOrName(args.Parameters[1])
                                                where (cfg.Settings.weaponBans.Any((ValueTuple<int, int, bool> item) => item.Item1 == i.type) || cfg.Settings.accsBans.Contains(i.type) || cfg.Settings.armorBans.Contains(i.type)) && i.ammo == 0
                                                select i).ToList<Item>();

                    if (foundDelItems.Count == 1)
                    {
                        Item i = foundDelItems[0];

                        if (cfg.Settings.weaponBans.Remove(cfg.Settings.weaponBans.FirstOrDefault((ValueTuple<int, int, bool> item) => item.Item1 == i.type)) || cfg.Settings.accsBans.Remove(i.type) || cfg.Settings.armorBans.Remove(i.type))
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

        private void BanBuff(CommandArgs args)
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

        private void BanProj(CommandArgs args)
        {
            TSPlayer plr = args.Player;

            if (args.Parameters.Count > 3 || args.Parameters.Count < 2)
            {
                plr.SendErrorMessage("Usage: /banproj <add/del> <projectile ID> <true/false>");
                return;
            }

            switch (args.Parameters[0].ToLower())
            {
                case "add":
                    int addid;
                    bool froze = false;
                    if (int.TryParse(args.Parameters[1], out addid) && addid > 0 && addid <= Terraria.ID.ProjectileID.Count)
                    {
                        if (!cfg.Settings.projBans.Any((ValueTuple<int, bool> proj) => proj.Item1 == addid))
                        {
                            if (args.Parameters.Count == 3)
                            {
                                froze = args.Parameters[2].Equals("true", StringComparison.OrdinalIgnoreCase);
                            }
                            cfg.Settings.projBans.Add(new ValueTuple<int, bool>(addid, froze));
                            cfg.Write(configPath);
                        }
                        args.Player.SendSuccessMessage("Banned projectile {0} in pvp.", addid);
                        break;
                    }
                    plr.SendErrorMessage("Invalid projectile ID.");
                    break;

                case "del":
                    int delid;
                    if (int.TryParse(args.Parameters[1], out delid) && delid > 0 && delid <= (int)Terraria.ID.ProjectileID.Count)
                    {
                        if (cfg.Settings.projBans.Any((ValueTuple<int, bool> proj) => proj.Item1 == delid))
                        {
                            if (cfg.Settings.projBans.FirstOrDefault((ValueTuple<int, bool> projectile) => projectile.Item1 == delid).Item1 == delid)
                            {
                                cfg.Settings.projBans.Remove(cfg.Settings.projBans.FirstOrDefault((ValueTuple<int, bool> projectile) => projectile.Item1 == delid));
                            }
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

        private void PvPItemBans(CommandArgs args)
        {
            int pageNumber;
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                return;
            IEnumerable<string> itemNames = cfg.Settings.weaponBans.Select(delegate (ValueTuple<int, int, bool> weapon)
            {
                string name = TShock.Utils.GetItemById(weapon.Item1).Name;
                string str = weapon.Item3 ? " (will freeze if used" : " (deals 0 damage";
                string str2 = (weapon.Item2 == 0) ? ")" : ("; allowed when [c/38bf38:" + TShock.Utils.GetPrefixById(weapon.Item2) + "])");
                return name + str + str2;
            }).Concat((from armor in cfg.Settings.armorBans
                       select TShock.Utils.GetItemById(armor).Name).Concat(from accs in cfg.Settings.accsBans
                                                                           select TShock.Utils.GetItemById(accs).Name));
            IEnumerable<string> enumerable = (from armor in cfg.Settings.armorBans
                select TShock.Utils.GetItemById(armor).Name).Concat((from accs in cfg.Settings.accsBans
                select TShock.Utils.GetItemById(accs).Name).Concat(from item in cfg.Settings.weaponBans
                select (item.Item2 == 0) ? TShock.Utils.GetItemById(item.Item1).Name : (TShock.Utils.GetItemById(item.Item1).Name + " (allowed when [c/38bf38:" + TShock.Utils.GetPrefixById(item.Item2) + "])")).ToList<string>());
            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(itemNames, maxCharsPerLine: 75),
                new PaginationTools.Settings
                {
                    HeaderFormat = "The following items cannot be used in PvP:",
                    FooterFormat = "Type /pvpitembans {0} for more.",
                    NothingToDisplayString = "There are currently no banned items."
                });
        }
        private void PvPBuffBans(CommandArgs args)
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
        private void PvPProjBans(CommandArgs args)
        {
            int pageNumber;
            if (!PaginationTools.TryParsePageNumber(args.Parameters, 0, args.Player, out pageNumber))
                return;
            IEnumerable<string> projectiles = from proj in cfg.Settings.projBans
                select proj.Item2 ? (proj.Item1.ToString() + " (will freeze if spawned)") : (proj.Item1.ToString() + " (deals 0 damage)");

            PaginationTools.SendPage(args.Player, pageNumber, PaginationTools.BuildLinesFromTerms(projectiles, maxCharsPerLine: 75),
                new PaginationTools.Settings
                {
                    HeaderFormat = "The following projectiles cannot be used in PvP:",
                    FooterFormat = "Type /pvpprojbans {0} for more.",
                    NothingToDisplayString = "There are currently no banned projectiles."
                });
        }
    }
}
