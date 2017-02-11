using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("DoomChat", "SoftDevAndy & wski", "2.2.2")]
    [Description("Custom Chat Plugin for DoomTown Rust Server")]
    class DoomChat : RustPlugin
    {
        private HashSet<string> list_FilteredWords = new HashSet<string>();
        private HashSet<string> list_ModeratorIDs = new HashSet<string>();

        private Dictionary<string, string> list_LastRepliedTo = new Dictionary<string, string>();
        private Dictionary<string, string> list_UserToClanTags = new Dictionary<string, string>();

        private MutedData allMutedPlayers = new MutedData();
        private ClanData allClans = new ClanData();
        private InviteData allInvites = new InviteData();
        private TradeChatData allTradeChatIDs = new TradeChatData();

        private System.Random rnd = new System.Random();

        private const int MINTAGSIZE = 3;
        private const int MAXTAGSIZE = 5;

        private const string CLANERRORMSG = "Please follow the format. Example /clan_create BACCA #ffffff";
        private const string ERROR = "You couldn't update the configuration file for some reason...";
        private const string UNKNOWN = "Unknown Command: ";
        private string YOUAREMUTED = "You are muted, please contact one of the admins.";

        private string Tag_Warning = "[CENSORED]";
        private string Tag_Muted = "[MUTED]";
        private string Tag_PrivateMessage = "[PM]";
        private string Message_ScoldText = "I said a naughty word... I'm looking to get banned..";

        private string Color_WordFilterTag = "#ff0707";
        private string Color_MutedFilterTag = "#fcfc71";
        private string Color_PrivateMessageTag = "#660066";
        private string Color_AdminName = "#bef25e";
        private string Color_PlayerName = "#797a79";
        private string Color_GlobalText = "#e5e5e5";
        private string Color_TradeText = "#ff99dd";

        private bool autoMute = false;

        private int metric_privateMessages = 0;
        private int metric_clanMessages = 0;
        private int metric_tradeMessages = 0;
        private int metric_allMessages = 0;
        private int metric_pokes = 0;

        /* End of Global Variables */

        #region Command List

        string CommandsText_Admin()
        {
            string msg = "";
            msg += "<color=grey>[DoomTown Admin Chat Commands]</color>";
            msg += "\n\n/cmd - Lists admin commands.";
            msg += "\n/cmd_player - Lists player commands.";
            msg += "\n/cmd_mute - Lists all mute commands.";
            msg += "\n/cmd_filters - Lists all word filter commands.";
            msg += "\n/cmd_clan - Lists all word filter commands.";
            msg += "\n";
            msg += "\n/t <text> - Post in trade chat.";
            msg += "\n/unsub - Unsub from trade chat so you no longer see trades.";
            msg += "\n/poke <playername> - Check if a user if online or offline.";
            msg += "\n/pm <playername> - Private message a player if they are online.";
            msg += "\n/r - Reply to the last person who messaged you.";
            msg += "\n/automute <bool> - Switches automute on or off.";
            msg += "\n/metrics  - Shows Chat Metrics.";
            msg += "\n\n<color=grey>[DoomTown Admin Chat Commands]</color>";
            return msg;
        }

        string Metrics_Text()
        {
            string msg = "";
            msg += "<color=grey>[DoomTown Metrics]</color>";
            msg += "\n\n - Private Messages: " + metric_privateMessages;
            msg += "\n\n - Clan Messages: " + metric_clanMessages;
            msg += "\n\n - Trade Messages: " + metric_tradeMessages;
            msg += "\n\n - All Messages: " + metric_allMessages;
            msg += "\n\n - Pokes: " + metric_pokes;
            msg += "\n\n<color=grey>[DoomTown Metrics]</color>";
            return msg;
        }

        string CommandsText_Mute()
        {
            string msg = "";
            msg += "<color=grey>[Admin Mute Commands]</color>\n";
            msg += "\n/cmd_mute - Lists all mute commands.";
            msg += "\n/mute <list>/<username> - Lists muted users or mutes user.";
            msg += "\n/unmute <username> - Unmutes the user.";
            msg += "\n/mutefun <username> - Mutes user and makes them moo.";
            msg += "\n\n<color=grey>[Admin Mute Commands]</color>";
            return msg;
        }

        string CommandsText_Filters()
        {
            string msg = "";
            msg += "<color=grey>[Admin Filter Commands]</color>\n";
            msg += "\n/filter list - Lists filtered words.";
            msg += "\n/filter add <text> - Adds word to filter list.";
            msg += "\n/filter remove <text> - Removes word from filter list.";
            msg += "\n\n<color=grey>[Admin Filter Commands]</color>";
            return msg;
        }

        string CommandsText_Clans(bool admin)
        {
            string msg = "";
            string adminline = "\n/clan dismantle <text> - Dismantles/remove a clan. \n/clans <pagenumber> - Lists clans. e.g /clans 2";

            if (admin)
                msg += "<color=grey>[Admin Clan Commands]</color>\n";
            else
                msg += "<color=grey>[Clan Commands]</color>\n";

            msg += "\n/c <text> - Talk in clan chat.";

            if (admin)
                msg += adminline;
            msg += "\n/clan create <tag letters> <hex colour> - Create a clan.";
            msg += "\n/clan invite <playername> - Invites player to clan.";
            msg += "\n/clan - Shows current invite if any.";
            msg += "\n/clan accept - Join clan if you have an invite.";
            msg += "\n/clan decline - Join clan if you have an invite.";
            msg += "\n/clan leave - Leave a clan if you are in one.";
            if (admin)
                msg += "\n\n<color=grey>[Admin Clan Commands]</color>";
            else
                msg += "\n\n<color=grey>[Clan Commands]</color>";
            return msg;
        }

        string CommandsText_Player()
        {
            string msg = "";
            msg += "<color=grey>[DoomTown Chat Commands]</color>";
            msg += "\n\n/cmd - Lists commands.";
            msg += "\n\n<color=red>Clans</color>";
            msg += "\n/cmd_clan - Lists all the clan commands.";
            msg += "\n\n<color=red>Private Messages</color>";
            msg += "\n/pm <playername> - Private message a player.";
            msg += "\n/r - Reply to the last person you messaged/messaged you.";
            msg += "\n\n<color=red>Online Checker</color>";
            msg += "\n/poke <playername> - Check if a user if online or offline.";
            msg += "\n\n<color=red>Trade Chat</color>";
            msg += "\n/t <text> - Post in trade chat.";
            msg += "\n/unsub - Unsubscribe from trade chat.";
            msg += "\n\n<color=grey>[DoomTown Chat Commands]</color>";
            return msg;
        }

        [ChatCommand("cmd")]
        void cmd_ShowCommands(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                PrintToChat(player, CommandsText_Admin());
            }
            else
                PrintToChat(player, CommandsText_Player());
        }

        [ChatCommand("cmd_player")]
        void cmd_ShowCommands_Player(BasePlayer player, string cmd, string[] args)
        {
            PrintToChat(player, CommandsText_Player());
        }

        [ChatCommand("cmd_mute")]
        void cmd_ShowCommands_Mute(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                PrintToChat(player, CommandsText_Mute());
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("cmd_filters")]
        void cmd_ShowCommands_Filters(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                PrintToChat(player, CommandsText_Filters());
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("cmd_clan")]
        void cmd_ShowCommands_Clans(BasePlayer player, string cmd, string[] args)
        {
            PrintToChat(player, CommandsText_Clans(isAdmin(player.UserIDString)));
        }
        #endregion

        #region System Hooks

        void Loaded()
        {
            LoadDefaultConfig();
            FindAddMods();

            allClans = Interface.Oxide.DataFileSystem.ReadObject<ClanData>("DoomChat_Clans_Data");
            allInvites = Interface.Oxide.DataFileSystem.ReadObject<InviteData>("DoomChat_Clans_Invites");
            allMutedPlayers = Interface.Oxide.DataFileSystem.ReadObject<MutedData>("DoomChat_MutedPlayers");
            allTradeChatIDs = Interface.Oxide.DataFileSystem.ReadObject<TradeChatData>("DoomChat_TradeChat");

            var Online = BasePlayer.activePlayerList as List<BasePlayer>;

            foreach (BasePlayer player in Online)
            {
                if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "moderator"))
                {
                    list_ModeratorIDs.Add(player.UserIDString);
                }

                if (allClans.isInClan(player.UserIDString))
                {
                    list_UserToClanTags.Add(player.UserIDString, allClans.getClanTag(player.UserIDString));
                }

                string clanName = allClans.getPlayerClan(player.UserIDString);

                if (clanName != "" && list_UserToClanTags.ContainsKey(player.UserIDString) == false)
                    list_UserToClanTags.Add(player.UserIDString, clanName);
            }

            Puts("Server just loaded");

        }// Loading in the values from the configuration file and registering all moderators to the moderatorid list

        void OnServerSave()
        {
            SaveInviteData();
            SaveClanData();
            SaveConfigurationChanges();
            SaveMuteList();
            SaveTradeChat();

            Puts("Server just saved. DoomChat data files and it's configuration file was also saved.");
        }

        protected override void LoadDefaultConfig()
        {
            List<string> bwords = new List<string>();

            bwords = Config.Get<List<string>>("Badwords");

            Message_ScoldText = Config.Get<string>("MessageCensored");
            Tag_Warning = Config.Get<string>("MessageWarning");
            Tag_Muted = Config.Get<string>("MessageMuted");

            Color_AdminName = Config.Get<string>("ColorAdminName");
            Color_WordFilterTag = Config.Get<string>("ColorAdminWarning");
            Color_GlobalText = Config.Get<string>("ColorGlobalText");
            Color_MutedFilterTag = Config.Get<string>("ColorMutedWarning");
            Color_PlayerName = Config.Get<string>("ColorPlayerName");
            Color_TradeText = Config.Get<string>("ColorTradeChat");
            Color_PrivateMessageTag = Config.Get<string>("ColorPMTag");
            autoMute = Config.Get<bool>("AutoMute");

            metric_privateMessages = Config.Get<int>("Metric_PrivateMessages");
            metric_clanMessages = Config.Get<int>("Metric_ClanMessages");
            metric_tradeMessages = Config.Get<int>("Metric_TradeMessages");
            metric_allMessages = Config.Get<int>("Metric_AllMessages");
            metric_pokes = Config.Get<int>("Metric_Pokes");

            foreach (string w in bwords)
            {
                list_FilteredWords.Add(w.ToUpper());
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "moderator"))
            {
                list_ModeratorIDs.Add(player.UserIDString);
            }

            if (allClans.isInClan(player.UserIDString))
            {
                list_UserToClanTags.Add(player.UserIDString, allClans.getClanTag(player.UserIDString));
            }

            string clanName = allClans.getPlayerClan(player.UserIDString);

            if (clanName != "")
            {
                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    if (IsOnlineAndValid(player, member))
                    {
                        var foundPlayer = rust.FindPlayer(member);

                        if (foundPlayer != null && foundPlayer != player)
                            PrintToChat(foundPlayer, "<color=green>" + player.displayName + "</color> came online.");
                    }
                }
            }

        }// Checking if the player is a moderator or admin and adding them to the moderator list.

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (list_ModeratorIDs.Contains(player.UserIDString))
                list_ModeratorIDs.Remove(player.UserIDString);

            if (list_UserToClanTags.ContainsKey(player.UserIDString))
                list_UserToClanTags.Remove(player.UserIDString);

            string clanName = allClans.getPlayerClan(player.UserIDString);

            if (clanName != "")
            {
                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    if (IsOnlineAndValid(player, member))
                    {
                        var foundPlayer = rust.FindPlayer(member);

                        if (foundPlayer != null && foundPlayer != player)
                            PrintToChat(foundPlayer, "<color=red>" + player.displayName + "</color> went offline.");
                    }
                }
            }

        }// If the player was a moderator or admin and was registed on the moderator list, they are removed on disconnect.

        object OnUserChat(IPlayer player, string message)
        {
            string styled = "";
            string colouredClanTag = allClans.getClanTagColoured(player.Id);

            if (allMutedPlayers.isMuted(player.Id) == false)
            {
                string msg = CleanMsg(player.Id, player.Name, message);

                if (Message_ScoldText == msg)
                {
                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + Message_ScoldText + "</color>";

                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player.Id, styled });

                    TellMods(player.Name, colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, false);
                }
                else
                {
                    styled = "";

                    if (isAdmin(player.Id))
                        styled = colouredClanTag + "<color=" + Color_AdminName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + message + "</color>";
                    else
                        styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + message + "</color>";

                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player.Id, styled });
                }

                Puts(player.Name + ": " + message);
            }
            else
            {
                if (allMutedPlayers.mutedStatus(player.Id) == false)
                {
                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + muteText(message) + "</color>";
                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player.Id, styled });
                }

                TellMods(player.Name, colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, true);
            }

            metric_allMessages++;

            return true;
        }

        #endregion            

        #region Word Filter System

        [ChatCommand("filter")]
        void cmd_WordFilters(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                #region args 1
                if (argsCheck(args, 1))
                {
                    string choice = args[0].ToUpper();

                    if (choice == "LIST")
                    {
                        string clist = "Filter List";

                        foreach (string w in list_FilteredWords)
                        {
                            clist = clist + " | " + w;
                        }

                        PrintToChat(player, clist);
                    }
                }
                #endregion

                #region args 2
                else if (argsCheck(args, 2))
                {
                    string choice = args[0].ToUpper();

                    if (choice == "ADD")
                    {
                        string tmp = args[1].ToUpper();
                        string msg = tmp + " added to the DoomChat config.";

                        if (!list_FilteredWords.Contains(tmp))
                        {
                            list_FilteredWords.Add(tmp);
                            SaveConfigurationChanges();
                            PrintToChat(player, msg);
                        }
                        else
                        {
                            PrintToChat(player, "Problem adding " + args[1].ToUpper() + " to the DoomChat config.");
                        }

                    }

                    if (choice == "REMOVE")
                    {
                        string tmp = args[1].ToUpper();
                        string msg = tmp + " removed from the DoomChat config.";

                        if (list_FilteredWords.Contains(tmp))
                        {
                            list_FilteredWords.Remove(tmp);
                            SaveConfigurationChanges();
                            PrintToChat(player, msg);
                        }
                        else
                        {
                            PrintToChat(player, "Problem removing " + args[1].ToUpper() + " to the DoomChat config.");
                        }
                    }
                }
                #endregion
                else
                {
                    PrintToChat(player, "Please use the correct filter format.\n/filter <list><add><remove>");
                }
            }
            else
                NoPerms(player, args[0]);
        }

        #endregion System System

        #region Metrics

        [ChatCommand("metrics")]
        void cmd_Metrics(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
                PrintToChat(player, Metrics_Text());
            else
                NoPerms(player, args[0]);
        }

        #endregion

        #region Mute Player System

        [ChatCommand("automute")]
        void cmd_AutoMuteToggle(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (argsCheck(args, 1))
                {
                    string status = args[0].ToUpper();
                    bool valid = false;

                    if (status == "ON" || status == "TRUE")
                    {
                        PrintToChat(player, "Switched AutoMute ON.");
                        autoMute = true;
                        valid = true;
                    }

                    if (status == "OFF" || status == "FALSE")
                    {
                        PrintToChat(player, "Switched AutoMute OFF.");
                        autoMute = false;
                        valid = true;
                    }

                    if (valid)
                        SaveConfigurationChanges();
                    else
                        PrintToChat(player, "Bads automute argument. Try /automute on or /automute true etc.");
                }
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("mute")]
        void cmd_MuteAdd(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null && args.Length > 0)
                {
                    if (args[0].ToUpper() == "LIST")
                    {
                        string mlist = "Mute List : " + allMutedPlayers.getMutedCount();

                        foreach (MutedPlayer p in allMutedPlayers.playerList)
                        {
                            if (IsOnlineAndValid(player, p.displayName))
                            {
                                mlist = mlist + "\n- " + p.displayName;
                            }
                        }

                        PrintToChat(player, mlist);
                    }
                    else if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString) == false)
                        {
                            allMutedPlayers.addMutedPlayer_Manual(foundPlayer.UserIDString, foundPlayer.displayName, true);
                            PrintToChat(player, "Added " + foundPlayer.displayName + " to mute list.");
                            Puts("[MUTED] Player " + foundPlayer.displayName + " .");
                            SaveMuteList();
                        }
                        else
                            PrintToChat(player, args[0] + " already added to mute list.");
                    }
                    else
                    {
                        PrintToChat(player, args[0] + " not found.");
                    }
                }
            }
            else
                NoPerms(player, cmd);
        }

        [ChatCommand("unmute")]
        void cmd_Unmute(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null && args.Length > 0)
                {
                    string pName = "";

                    for (int i = 0; i < args.Length; i++)
                    {
                        string tmp = "";

                        if (i > 0)
                            tmp = " ";

                        pName += tmp + args[i];
                    }

                    int count = allMutedPlayers.getMutedCount();
                    string name = "";

                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString))
                        {
                            name = foundPlayer.displayName;
                            allMutedPlayers.removeMutedPlayer(foundPlayer.UserIDString);

                            SaveMuteList();
                        }
                    }

                    if (count != allMutedPlayers.getMutedCount())
                        PrintToChat(player, "Removed " + name + " from the mute list.");
                    else
                        PrintToChat(player, "Couldn't remove " + args[0] + " from the mute list.");
                }
            }
            else
                NoPerms(player, cmd);
        }

        [ChatCommand("mutefun")]
        void cmd_MuteAdd_Fun(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null && args.Length > 0)
                {
                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString) == false)
                        {
                            allMutedPlayers.addMutedPlayer_Manual(foundPlayer.UserIDString, foundPlayer.displayName, false);

                            PrintToChat(player, "Added " + foundPlayer.displayName + " to mute list.");
                            SaveMuteList();
                        }
                        else
                            PrintToChat(player, args[0] + " already added to mute list");
                    }
                    else
                    {
                        PrintToChat(player, args[0] + " not found.");
                    }
                }
            }
            else
                NoPerms(player, cmd);
        }

        class MutedPlayer
        {
            public string userID { get; set; }
            public string displayName { get; set; }
            public bool muteStatus { get; set; }
            public string offendingMessage { get; set; }
            public string offendingWord { get; set; }

            public MutedPlayer() { }

            public MutedPlayer(string userID)
            {
                this.userID = userID;
            }

            public MutedPlayer(string userID, string displayName, bool muteStatus, string offendingMessage, string offendingWord)
            {
                this.userID = userID;
                this.displayName = displayName;
                this.muteStatus = muteStatus;
                this.offendingMessage = offendingMessage;
                this.offendingWord = offendingWord;
            }

            public override bool Equals(object obj)
            {
                var item = obj as MutedPlayer;

                if (userID != null && item != null)
                {
                    if (userID != "")
                    {
                        if (this.userID == item.userID)
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return userID.GetHashCode();
            }
        }

        class MutedData
        {
            public HashSet<MutedPlayer> playerList { get; set; }

            public MutedData()
            {
                playerList = new HashSet<MutedPlayer>();
            }

            public MutedData(HashSet<MutedPlayer> playerList)
            {
                this.playerList = playerList;
            }

            public int getMutedCount()
            {
                return playerList.Count;
            }

            public void addMutedPlayer_Manual(string userID, string displayName, bool status)
            {
                string offendingMessage = "Manual Mute by admin";

                MutedPlayer mutedPlayer = new MutedPlayer(userID, displayName, status, offendingMessage, offendingMessage);

                if (playerList.Contains(mutedPlayer) == false)
                    playerList.Add(mutedPlayer);
            }

            public void addMutedPlayer_Logged(string userID, string displayName, bool status, string offendingMessage, string offendingWord)
            {
                MutedPlayer mutedPlayer = new MutedPlayer(userID, displayName, status, offendingMessage, offendingWord);

                if (playerList.Contains(mutedPlayer) == false)
                    playerList.Add(mutedPlayer);
            }

            public void removeMutedPlayer(string userID)
            {
                MutedPlayer mutedPlayer = new MutedPlayer(userID);

                if (playerList.Contains(mutedPlayer) != false)
                    playerList.Remove(mutedPlayer);
            }

            public bool mutedStatus(string userID)
            {
                MutedPlayer muted = new MutedPlayer(userID);

                if (playerList.Contains(muted))
                {
                    foreach (MutedPlayer p in playerList)
                    {
                        if (p.userID == muted.userID)
                            return p.muteStatus;
                    }

                    return false;
                }

                return false;

            }

            public bool isMuted(string userID)
            {
                if (playerList.Contains(new MutedPlayer(userID)))
                    return true;
                else
                    return false;
            }
        }

        void SaveMuteList()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_MutedPlayers", allMutedPlayers);
        }

        #endregion

        #region Trade System

        [ChatCommand("unsub")]
        void cmd_UnsubTradeChat(BasePlayer player, string cmd, string[] args)
        {
            if (allTradeChatIDs.doesExist(player.UserIDString) == false)
            {
                allTradeChatIDs.addPlayer(player.UserIDString);
                PrintToChat(player, "Unsubbed from trade chat.");
            }
            else
                PrintToChat(player, "You aren't signed up to tradechat");
        }

        [ChatCommand("t")]
        void cmd_PostIntoTradeChat(BasePlayer player, string cmd, string[] args)
        {
            if (allMutedPlayers.isMuted(player.UserIDString) == false)
            {
                if (anyArgsCheck(args))
                {
                    if (allTradeChatIDs.doesExist(player.UserIDString))
                    {
                        allTradeChatIDs.removePlayer(player.UserIDString);
                        PrintToChat(player, "Subscribed to trade chat\nTo unsubscribe type /unsub");
                    }

                    string msg = "";

                    for (int i = 0; i < args.Length; i++)
                    {
                        string tmp = "";

                        if (i > 0)
                            tmp = " ";

                        msg += tmp + args[i];
                    }

                    string fullMsg = " <color=" + Color_TradeText + ">[Trade] " + "</color><color=" + Color_PlayerName + ">" + player.displayName + ": </color>" + msg;

                    Puts("[Trade Chat] " + player.displayName + ": " + msg);

                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        if (allTradeChatIDs.doesExist(p.UserIDString) == false)
                        {
                            var foundPlayer = rust.FindPlayer(p.UserIDString);

                            if (foundPlayer != null)
                            {
                                rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                            }
                        }
                    }

                    metric_tradeMessages++;
                }

            }// If not on the mute list
            else
            {
                PrintToChat(player, YOUAREMUTED);

                if (anyArgsCheck(args))
                {
                    string msg = "";

                    for (int i = 0; i < args.Length; i++)
                    {
                        string tmp = "";

                        if (i > 0)
                            tmp = " ";

                        msg += tmp + args[i];
                    }

                    TellMods(player.displayName, "<color=" + Color_PlayerName + ">" + player.displayName + ": </color>", msg, true);
                }
            }
        }

        class TradeChatData
        {
            public HashSet<string> list_TradeChatIDs { get; set; }

            public TradeChatData()
            {
                list_TradeChatIDs = new HashSet<string>();
            }

            public bool doesExist(string userid)
            {
                if (list_TradeChatIDs.Contains(userid))
                    return true;
                else
                    return false;
            }

            public void addPlayer(string userid)
            {
                if (list_TradeChatIDs.Contains(userid) == false)
                    list_TradeChatIDs.Add(userid);
            }

            public void removePlayer(string userid)
            {
                if (list_TradeChatIDs.Contains(userid))
                    list_TradeChatIDs.Remove(userid);
            }
        }

        #endregion

        #region Clan System

        void SaveTradeChat()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_TradeChat", allTradeChatIDs);
        }

        void SaveInviteData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_Clans_Invites", allInvites);
        }

        void SaveClanData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_Clans_Data", allClans);
        }

        bool CheckForInvite(string userID)
        {
            if (allInvites.pendingInvites.ContainsKey(userID))
                return true;
            else
                return false;
        }

        [ChatCommand("c")]
        void cmd_ClanChat(BasePlayer player, string cmd, string[] args)
        {
            if (allClans.isInClan(player.UserIDString))
            {
                string msg = "";

                for (int i = 0; i < args.Length; i++)
                {
                    string tmp = "";

                    if (i > 0)
                        tmp = " ";

                    msg += tmp + args[i];
                }

                string clanName = allClans.getClanTag(player.UserIDString);
                string colouredTag = allClans.getClanByTag(clanName).tagColor;
                string fullMsg = "<color=" + colouredTag + ">" + "[CLAN CHAT] </color>" + "<color=" + Color_PlayerName + ">" + player.displayName + ":</color> " + msg;

                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    if (IsOnlineAndValid(player, member))
                    {
                        var foundPlayer = rust.FindPlayer(member);

                        if (foundPlayer != null)
                            rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                    }
                }

                metric_clanMessages++;

                Puts("[CLAN CHAT] " + player.displayName + ": " + msg);
            }
            else
                PrintToChat(player, "You are not in a clan.");
        }

        [ChatCommand("clans")]
        void cmd_ClanList(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                const int PAGESIZE = 10;
                bool valid = true;

                int page = 0;

                if (anyArgsCheck(args))
                {
                    valid = int.TryParse(args[0], out page);

                    if (valid)
                        page = Convert.ToInt32(args[0]);
                }

                int count = 0;
                int startCount = 0;
                bool paged = false;

                if (page == 0 || page == 1)
                    page = 0;
                else
                    page = page - 1;

                startCount = page * PAGESIZE;

                string clist = "Clan List -- " + allClans.clansList.Count + " clans exist " + "\nPage " + (page + 1) + " Showing " + startCount + " / " + (startCount + 10);

                foreach (ClanObj clan in allClans.clansList)
                {
                    if (count >= startCount && count <= (startCount + PAGESIZE) && count < allClans.clansList.Count)
                        clist = clist + "\n<color=" + clan.tagColor + ">" + clan.tag + "</color> ---- Members: " + clan.members.Count;

                    ++count;
                }

                PrintToChat(player, clist);
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("clan")]
        void cmd_ClanDecision(BasePlayer player, string cmd, string[] args)
        {
            bool isOwner = allClans.isOwner(player.UserIDString);
            bool InClan = allClans.isInClan(player.UserIDString);
            bool left = false;

            if (args.Length == 0)
            {
                #region args 0
                if (allInvites.pendingInvites.ContainsKey(player.UserIDString))
                {
                    PrintToChat(player, "You have a pending invite from the clan " + allInvites.pendingInvites[player.UserIDString] + " .");
                }
                else
                    PrintToChat(player, "You have no pending invites.");
                #endregion
            }
            else if (argsCheck(args, 1))
            {
                #region args 1
                string choice = args[0].ToUpper();
                bool action = false;

                if (allInvites.pendingInvites.ContainsKey(player.UserIDString))
                {
                    #region Clan Invite / Clan Decline
                    PrintToChat(player, "You have a pending invite from clan " + allInvites.pendingInvites[player.UserIDString]);

                    if (choice == "ACCEPT")
                    {
                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " accepted!");

                        allClans.getClanByTag(allInvites.pendingInvites[player.UserIDString]).members.Add(player.UserIDString);

                        list_UserToClanTags.Add(player.UserIDString, allInvites.pendingInvites[player.UserIDString]);

                        allInvites.pendingInvites.Remove(player.UserIDString);

                        action = true;
                        SaveInviteData();
                        SaveClanData();
                    }

                    if (choice == "DECLINE")
                    {
                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " declined!");
                        allInvites.pendingInvites.Remove(player.UserIDString);

                        action = true;
                        SaveInviteData();
                        SaveClanData();
                    }
                    #endregion
                }

                #region Clan Leave
                if (choice == "LEAVE")
                {
                    string clanname = list_UserToClanTags[player.UserIDString];

                    if (isOwner)
                    {
                        ClanObj c = new ClanObj(clanname);

                        if (allClans.clansList.Contains(c))
                        {
                            foreach (KeyValuePair<string, string> kv in allInvites.pendingInvites)
                            {
                                if (kv.Value == args[0])
                                {
                                    allInvites.pendingInvites.Remove(kv.Value);
                                }
                            }

                            foreach (string member in allClans.getClanByTag(clanname).members)
                            {
                                if (list_UserToClanTags.ContainsKey(member))
                                    list_UserToClanTags.Remove(member);
                            }

                            ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + "has been dismantled by " + player.displayName + "." });

                            Puts("Clan " + c.tag + " has been dismantled by " + player.displayName + ".");

                            allClans.clansList.Remove(c);

                            action = true;
                            SaveClanData();
                            SaveInviteData();
                        }
                    }
                    else
                    {
                        PrintToChat(player, "You have left the clan " + clanname + " .");

                        allClans.leaveClan(player.UserIDString);

                        if (list_UserToClanTags.ContainsKey(player.UserIDString))
                            list_UserToClanTags.Remove(player.UserIDString);

                        action = true;
                        SaveClanData();
                    }
                }

                if (!action)
                {
                    PrintToChat(player, "Bad or missing arguements: /clan " + args[0]);
                }

                #endregion

                #endregion
            }
            else if (argsCheck(args, 2))
            {
                #region args 2

                string choice = args[0].ToUpper();
                string tag = args[1].ToUpper();

                #region Clan Dismantle

                ClanObj c;

                if (choice == "DISMANTLE")
                {
                    if (isAdmin(player.UserIDString))
                    {
                        c = allClans.getClanByTag(tag);

                        if (c != null)
                        {
                            if (allClans.clansList.Contains(c))
                            {
                                if (allClans.clansList.Remove(c))
                                {
                                    foreach (KeyValuePair<string, string> kv in allInvites.pendingInvites)
                                    {
                                        if (kv.Value == tag)
                                        {
                                            allInvites.pendingInvites.Remove(kv.Value);
                                        }
                                    }

                                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + "has been dismantled by " + player.displayName });
                                    Puts("Clan [" + c.tag + "] has been dismantled by " + player.displayName + ".");
                                    SaveClanData();
                                }
                                else
                                    PrintToChat(player, "Couldn't dismantle the clan for some reason.");
                            }
                            else
                                PrintToChat(player, "Couldn't find the " + tag + " clan to dismantle");
                        }
                        else
                        {
                            PrintToChat(player, "Couldn't find the " + tag + " clan to dismantle");
                        }

                    }
                    else
                        NoPerms(player, args[0]);
                }
                #endregion

                #region Clan Invite
                if (choice == "INVITE") // /clan invite Andy
                {
                    if (allClans.isOwner(player.UserIDString))
                    {
                        if (IsOnlineAndValid(player, args[1]))
                        {
                            var foundPlayer = rust.FindPlayer(args[1]);

                            if (foundPlayer != null)
                            {
                                if (allClans.isInClan(foundPlayer.UserIDString) == false)
                                {
                                    if (allInvites.pendingInvites.ContainsKey(foundPlayer.UserIDString))
                                    {
                                        PrintToChat(player, "Player already has an invite");
                                    }
                                    else
                                    {
                                        allInvites.pendingInvites.Add(foundPlayer.UserIDString, allClans.getClanByOwner(player.UserIDString).tag);
                                        PrintToChat(player, "Invited player " + foundPlayer.displayName + " to your clan.");
                                        PrintToChat(foundPlayer, "You have a clan invite from " + allClans.getClanByOwner(player.UserIDString).tag);

                                        SaveInviteData();
                                    }
                                }
                                else
                                {
                                    PrintToChat(player, "Player is already in a clan");
                                }
                            }
                        }
                        else
                            PrintToChat(player, "Player is not online to invite.");
                    }
                    else
                        PrintToChat(player, "You aren't the owner of a clan.");
                }
                #endregion

                #region Clan Kick
                if (choice == "KICK")
                {
                    if (allClans.isOwner(player.UserIDString))
                    {
                        var foundPlayer = rust.FindPlayer(args[1]);

                        if (foundPlayer != null)
                        {
                            if (foundPlayer != player)
                            {
                                allClans.leaveClan(foundPlayer.UserIDString);

                                if (list_UserToClanTags.ContainsKey(foundPlayer.UserIDString))
                                    list_UserToClanTags.Remove(foundPlayer.UserIDString);

                                SaveClanData();
                                PrintToChat(player, "Player " + foundPlayer.displayName + " has been kicked from the clan.");
                            }
                            else
                                PrintToChat(player, "You cannot kick yourself from the clan as owner. Please use /clan leave to dismantle the clan.");
                        }
                        else
                            PrintToChat(player, "Player " + args[1] + " has not been found.");
                    }
                    else
                    {
                        PrintToChat(player, "You aren't the owner of a clan");
                    }
                }
                #endregion

                #endregion
            }
            else if (argsCheck(args, 3))
            {
                #region args 3

                string choice = args[0].ToUpper();

                #region Clan Create
                if (choice == "CREATE")
                {
                    if (allClans != null)
                    {
                        if (allClans.isInClan(player.UserIDString) == false)
                        {
                            string tag = args[1].ToUpper();
                            string hexcolor = args[2];

                            ClanObj c = new ClanObj(tag);

                            if (allClans.getClanByTag(tag) != null)
                            {
                                PrintToChat(player, "Clan: " + tag + " already exits.");
                            }
                            else
                            {
                                if (tag.Length >= MINTAGSIZE && tag.Length <= MAXTAGSIZE && IsAlphaNumeric(Convert.ToString(tag)))
                                {
                                    if (ValidHex(hexcolor))
                                    {
                                        string userid = player.UserIDString;
                                        string t = tag.ToUpper();
                                        string tagColor = hexcolor;

                                        ClanObj clan = new ClanObj(userid, t, tagColor);

                                        allClans.clansList.Add(clan);

                                        list_UserToClanTags.Add(player.UserIDString, t);

                                        SaveClanData();

                                        PrintToChat(player, "Clan <color=" + tagColor + ">[" + t + "]</color> has been created by " + player.displayName + ".");
                                        Puts("Clan [" + t + "] has been created by " + player.displayName + ".");
                                    }
                                    else
                                        PrintToChat(player, CLANERRORMSG);
                                }
                                else
                                    PrintToChat(player, "Tag size can only be " + MAXTAGSIZE + " characters long and " + MINTAGSIZE + " characters short." + CLANERRORMSG);
                            }
                        }
                        else
                            PrintToChat(player, "You are already in a clan.");
                    }
                }
                #endregion Clan

                #endregion
            }
        }

        class InviteData
        {
            public Dictionary<string, string> pendingInvites { get; set; }

            public InviteData()
            {
                pendingInvites = new Dictionary<string, string>();
            }
        }

        class ClanData
        {
            public HashSet<ClanObj> clansList { get; set; }

            public ClanData()
            {
                clansList = new HashSet<ClanObj>();
            }

            public string getClanTag(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    foreach (string p in c.members)
                    {
                        if (p == userid)
                            return c.tag;
                    }
                }

                return "";
            }

            public string getClanTagColoured(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    foreach (string p in c.members)
                    {
                        if (p == userid)
                            return "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> ";
                    }
                }

                return "";
            }

            public bool isOwner(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    if (c.ownerid == userid)
                        return true;
                }

                return false;
            }

            public bool isInClan(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    foreach (string p in c.members)
                    {
                        if (p == userid)
                            return true;
                    }
                }

                return false;
            }

            public string getPlayerClan(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    foreach (string p in c.members)
                    {
                        if (p == userid)
                            return c.tag;
                    }
                }

                return "";
            }

            public ClanObj getClanByTag(string clantag)
            {
                foreach (ClanObj c in clansList)
                {
                    if (c.tag == clantag)
                        return c;
                }

                return null;
            }

            public ClanObj getClanByOwner(string userid)
            {
                foreach (ClanObj c in clansList)
                {
                    if (c.ownerid == userid)
                        return c;
                }

                return null;
            }

            public void leaveClan(string userid)
            {
                ClanObj tmp = new ClanObj();

                foreach (ClanObj c in clansList)
                {
                    bool flag = false;

                    foreach (string member in c.members)
                    {
                        if (userid == member)
                        {
                            flag = true;
                        }
                    }

                    if (flag)
                    {
                        c.members.Remove(userid);
                    }
                }
            }
        }

        class ClanObj
        {
            public string ownerid { get; set; }
            public string tag { get; set; }
            public string tagColor { get; set; }
            public HashSet<string> members { get; set; }

            public ClanObj() { }

            public ClanObj(string tag)
            {
                this.tag = tag;
            }

            public ClanObj(string userid, string tag, string tagColor)
            {
                members = new HashSet<string>();

                this.ownerid = userid;
                this.tag = tag;
                this.tagColor = tagColor;

                members.Add(ownerid);
            }

            public string getTagColoured()
            {
                return "<color=" + tagColor + ">" + tag + "</color>";
            }

            public override bool Equals(object obj)
            {
                var item = obj as ClanObj;

                if (tag != null && item != null)
                {
                    if (tag != "")
                    {
                        if (this.tag.ToUpper() == item.tag.ToUpper())
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
                else
                    return false;
            }

            public override int GetHashCode()
            {
                return tag.ToUpper().GetHashCode();
            }
        }
        #endregion

        #region Player Poke
        [ChatCommand("poke")]
        void cmd_CheckOnline(BasePlayer player, string cmd, string[] args)
        {
            if (args != null)
            {
                if (args[0] != null)
                {
                    if (IsOnlineAndValid(player, args[0]))
                    {
                        string online = "<color=green>" + IsOnlinePoke(player, args[0]) + " is online.</color>";

                        PrintToChat(player, online);
                    }
                    else
                    {
                        string offline = "<color=red>" + args[0] + " is offline.</color>";

                        PrintToChat(player, offline);
                    }
                }
                else
                {
                    PrintToChat(player, "Please enter proper arguments. Example /online Andy");
                }
            }

            metric_pokes++;
        }
        #endregion

        #region PM System
        [ChatCommand("pm")]
        void cmd_PrivateMessage(BasePlayer player, string cmd, string[] args)
        {
            if (allMutedPlayers.isMuted(player.UserIDString) == false)
            {
                if (args.Length > 1)
                {
                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        string msg = "";

                        for (int i = 1; i < args.Length; i++)
                        {
                            string tmp = "";

                            if (i > 0)
                                tmp = " ";

                            msg += tmp + args[i];
                        }

                        if (player.UserIDString != foundPlayer.UserIDString)
                        {
                            string fullMsg = "<color=" + Color_PrivateMessageTag + ">" + Tag_PrivateMessage + " </color><color=" + Color_PlayerName + ">(" + player.displayName + " --> " + foundPlayer.displayName + "):</color>" + msg;

                            Puts("[PM] " + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                            rust.SendChatMessage(player, fullMsg, null, player.UserIDString);
                            rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);

                            UpdateLastReplied(player, foundPlayer);

                            metric_privateMessages++;
                        }
                        else
                            PrintToChat(player, "You can't PM yourself.");
                    }
                    else
                        PrintToChat(player, "The player " + args[0] + " is not online.");
                }
                else
                    PrintToChat(player, "Message was too short.");
            }
            else
            {
                PrintToChat(player, YOUAREMUTED);

                if (args.Length > 1)
                {
                    string msg = "";

                    for (int i = 1; i < args.Length; i++)
                    {
                        string tmp = "";

                        if (i > 0)
                            tmp = " ";

                        msg += tmp + args[i];
                    }

                    TellMods(player.displayName, "<color=" + Color_PlayerName + ">" + player.displayName + ": </color>", msg, true);
                }
            }

        }// Private Message another player

        [ChatCommand("r")]
        void cmd_PrivateMessageReply(BasePlayer player, string cmd, string[] args)
        {
            if (anyArgsCheck(args))
            {
                if (list_LastRepliedTo.ContainsKey(player.UserIDString))
                {
                    if (IsOnlineAndValid(player, list_LastRepliedTo[player.UserIDString]))
                    {
                        var foundPlayer = rust.FindPlayer(list_LastRepliedTo[player.UserIDString]);

                        string msg = " ";

                        for (int i = 0; i < args.Length; i++)
                        {
                            string tmp = "";

                            if (i > 0)
                                tmp = " ";

                            msg += tmp + args[i];
                        }

                        string fullMsg = "<color=" + Color_PrivateMessageTag + ">" + Tag_PrivateMessage + " </color><color=" + Color_PlayerName + ">(" + player.displayName + " --> " + foundPlayer.displayName + "):</color>" + msg;

                        Puts("[PM] " + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                        rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                        rust.SendChatMessage(player, fullMsg, null, player.UserIDString);

                        UpdateLastReplied(player, foundPlayer);
                    }
                    else
                        PrintToChat(player, "You havn't pm'd anyone yet nor has anyone pm'd you.");
                }
                else
                    PrintToChat(player, "Message was too short.");
            }

        }// Private Message another player
        #endregion

        #region Helpers

        bool IsAlphaNumeric(string str)
        {
            string s = Convert.ToString(str);

            foreach (char c in s)
            {
                if (!Char.IsLetterOrDigit(c))
                    return false;
            }
            return true;
        }

        bool anyArgsCheck(string[] args)
        {
            if (args == null)
                return false;

            try
            {
                if (args[0] == "")
                    return false;
            }
            catch { return false; }

            return true;
        }

        bool argsCheck(string[] args, int count)
        {
            Puts("Args Len: " + args.Length + " count: " + count);

            if (args == null)
                return false;

            try
            {
                if (args[0] == null)
                    return false;
            }
            catch { return false; }

            try
            {
                if (args[0] == "")
                    return false;
            }
            catch { return false; }

            try
            {
                if (args.Length != count)
                    return false;
            }
            catch { return false; }

            return true;
        }

        public bool IsOnlineAndValid(BasePlayer player, string partialName)
        {
            var foundPlayer = rust.FindPlayer(partialName);
            if (foundPlayer == null)
            {
                //PrintToChat(player, $"We couldn't find a player named {partialName}");
                return false;
            }
            else
            {
                var Online = BasePlayer.activePlayerList as List<BasePlayer>;

                if (Online.Contains(foundPlayer))
                {
                    return true;
                }
                else
                {
                    //PrintToChat(player, $"{foundPlayer.displayName} is not online, try again later!");
                    return false;
                }
            }

        }// Checks if the user is online and the username is valid

        public string IsOnlinePoke(BasePlayer player, string partialName)
        {
            var foundPlayer = rust.FindPlayer(partialName);

            return foundPlayer.displayName;

        }// Checks if the user is online and the username is valid

        void SaveConfigurationChanges()
        {
            List<string> saveWords = new List<string>();

            foreach (String w in list_FilteredWords)
            {
                string tmp = w.ToUpper();

                saveWords.Add(tmp);
            }

            Config.Clear();

            Config["Badwords"] = saveWords;

            Config["MessageCensored"] = Message_ScoldText;
            Config["MessageWarning"] = Tag_Warning;
            Config["MessageMuted"] = Tag_Muted;

            Config["ColorAdminName"] = Color_AdminName;
            Config["ColorPlayerName"] = Color_PlayerName;
            Config["ColorAdminWarning"] = Color_WordFilterTag;
            Config["ColorMutedWarning"] = Color_MutedFilterTag;
            Config["ColorGlobalText"] = Color_GlobalText;
            Config["ColorTradeChat"] = Color_TradeText;
            Config["ColorPMTag"] = Color_PrivateMessageTag;
            Config["AutoMute"] = autoMute;

            Config["Metric_PrivateMessages"] = metric_privateMessages;
            Config["Metric_ClanMessages"] = metric_clanMessages;
            Config["Metric_TradeMessages"] = metric_tradeMessages;
            Config["Metric_AllMessages"] = metric_allMessages;
            Config["Metric_Pokes"] = metric_pokes;

            SaveConfig();

        }// Saves all changes made by commands to the configuration file.

        private void FindAddMods()
        {
            var Online = BasePlayer.activePlayerList as List<BasePlayer>;

            foreach (BasePlayer player in Online)
            {
                if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "mod"))
                {
                    list_ModeratorIDs.Add(player.UserIDString);
                }
            }

        }// Gets all current users and if they are a moderator or admin then add them to the moderatorid list.

        private void TellMods(string name, string namecolored, string originalMessage, bool flag)
        {
            string msg = "";

            if (!flag)
                msg = "<color=" + Color_WordFilterTag + ">" + Tag_Warning + "</color> " + namecolored + ": " + originalMessage;
            else
                msg = "<color=" + Color_MutedFilterTag + ">" + Tag_Muted + "</color> " + namecolored + ": " + originalMessage;

            foreach (string id in list_ModeratorIDs)
            {
                var Online = BasePlayer.activePlayerList as List<BasePlayer>;

                foreach (BasePlayer player in Online)
                {
                    if (player.UserIDString == id)
                    {
                        PrintToChat(player, msg);
                    }
                }
            }

            if (!flag)
                Puts("[Telling Mods] " + Tag_Warning + " " + name + ": " + originalMessage);
            else
                Puts("[Telling Mods] " + Tag_Muted + " " + name + ": " + originalMessage);

        }// Private message all moderators and admins

        void UpdateLastReplied(BasePlayer fromPlayer, BasePlayer toPlayer)
        {
            if (list_LastRepliedTo.ContainsKey(fromPlayer.UserIDString) == false)
            {
                list_LastRepliedTo.Add(fromPlayer.UserIDString, toPlayer.UserIDString);
            }
            else
            {
                list_LastRepliedTo[fromPlayer.UserIDString] = toPlayer.UserIDString;
            }

            if (list_LastRepliedTo.ContainsKey(toPlayer.UserIDString) == false)
            {
                list_LastRepliedTo.Add(toPlayer.UserIDString, fromPlayer.UserIDString);
            }
            else
            {
                list_LastRepliedTo[toPlayer.UserIDString] = fromPlayer.UserIDString;
            }

        }// Update last person replied too 

        private bool isAdmin(string playerID)
        {
            if (list_ModeratorIDs.Contains(playerID))
                return true;
            else
                return false;

        }// Check the moderatorIDS list with the userid, if they exist return true

        private string CleanMsg(string userid, string displayname, string msg)
        {
            string original = msg;
            string upper = msg.ToUpper();

            foreach (string word in list_FilteredWords)
            {
                if (upper.Contains(word))
                {
                    if (autoMute)
                    {
                        Puts("AUTOMUTED for saying: " + msg + " offending word: " + word);
                        allMutedPlayers.addMutedPlayer_Logged(userid, displayname, true, msg, word);
                        SaveMuteList();
                    }

                    return Message_ScoldText;
                }
            }

            return original;
        }

        void NoPerms(BasePlayer player, string arg)
        {
            string msg = UNKNOWN + arg;
            PrintToChat(player, msg);
        }

        bool ValidHex(string hexStr)
        {
            const int HEXSTRLEN = 7;

            if (hexStr.Length != HEXSTRLEN)
                return false;

            foreach (char c in hexStr.ToCharArray())
            {
                char tmp = Char.ToUpper(c);

                if ((Char.IsDigit(tmp) || (tmp >= 'A' && tmp <= 'F') || tmp == '#') == false)
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region MuteFun Methods

        string muteText(string msg)
        {
            string txt = "";

            const string OH = "O";
            string tmp = "";

            for (int i = 0; i < wordCount(msg); i++)
            {
                if (rnd.Next(0, 2) == 0)
                    tmp = "M";
                else
                    tmp = "m";

                for (int j = 0; j < randNum(); j++)
                {
                    if (rnd.Next(0, 2) == 0)
                        tmp += "o";
                    else
                        tmp += "O";
                }

                txt += tmp + " ";
            }

            return txt;
        }

        int wordCount(string msg)
        {
            char sp = ' ';
            int i, count;

            count = 1;

            for (i = 0; i < msg.Length; i++)
                if (msg[i].Equals(sp))
                    count++;

            return count;
        }

        int randNum()
        {
            return rnd.Next(2, 5);
        }

        #endregion
    }

}//namespace