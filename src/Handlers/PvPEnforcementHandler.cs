using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Configuration;

namespace PvPChecks;

/// <summary>Проверки PvP-экипировки, телепортации и сетевых пакетов.</summary>
internal sealed class PvPEnforcementHandler
{
    private readonly ConfigFile<Config> cfg;

    public PvPEnforcementHandler(ConfigFile<Config> config) => cfg = config;

        DateTime[] WarningMsgCooldown = new DateTime[256];
        public void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
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
        }
		
		public void OnTeleport(object sender, GetDataHandlers.TeleportEventArgs args)
		{
			if (!args.Player.TPlayer.hostile) return;
			if (args.Player.HasPermission("pvpchecks.ignore")) return;
			
			args.Player.Disable("Used teleporting in pvp.", DisableFlags.None);
			args.Player.Teleport(args.Player.TPlayer.position.X, args.Player.TPlayer.position.Y);
			args.Player.SendErrorMessage("You can't teleport in pvp.");

            args.Player.SetPvP(false);
        }

        public void OnTogglePvp(object? sender, GetDataHandlers.TogglePvpEventArgs args)
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
        }

        public void OnPlayerDamage(object? sender, GetDataHandlers.PlayerDamageEventArgs args)
        {
            if (args.ID != args.Player.Index && args.PlayerDeathReason != null)
            {
                if (args.PlayerDeathReason.SourceProjectileType.HasValue)
                {
                    int proj = args.PlayerDeathReason.SourceProjectileType.Value;
                    if (cfg.Settings.projBans.Contains(proj))
                    {
                        args.Player.SendData(PacketTypes.PlayerHp, "", args.ID);
                        args.Player.SendData(PacketTypes.PlayerUpdate, "", args.ID);
                        args.Handled = true;
                        args.Player.SendErrorMessage("Projectile banned in pvp");
                    }
                }
                if (cfg.Settings.weaponBans.Contains(args.PlayerDeathReason._sourceItemType))
                {
                    args.Player.SendData(PacketTypes.PlayerHp, "", args.ID);
                    args.Player.SendData(PacketTypes.PlayerUpdate, "", args.ID);
                    args.Handled = true;
                    args.Player.SendErrorMessage("Weapon banned in pvp");
                }
            }
        }

        public void OnSendData(SendDataEventArgs args)
        {
            if (args.MsgId == PacketTypes.PlayerAddBuff)
            {
                if (TShock.Players[args.number].TPlayer.hostile)
                {
                    args.Handled = true;
                }
            }
        }
        public void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.SyncLoadout)
            {
                TSPlayer player = TShock.Players[args.Msg.whoAmI];
                if (player.TPlayer.hostile)
                    player.SetPvP(false, true);
            }
        }


}
