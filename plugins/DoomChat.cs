using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("DoomChat", "SoftDevAndy & wski", "1.0.1")]
    [Description("Custom Chat Plugin for the DoomTown.io Rust Server")]
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
        private IgnoreData allIgnoreData = new IgnoreData();

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
        private int metric_diceRolls = 0;

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
            msg += "\n/rolldice <playername> <playername> etc - Rolls dice.";
            msg += "\n/automute <bool> - Switches automute on or off.";
            msg += "\n/metrics - Shows Chat Metrics.";
            msg += "\n/ignore <playername> - Ignore Player.";
            msg += "\n/unignore <playername> - Unignore Player.";
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
            msg += "\n\n - DiceRolls: " + metric_diceRolls;
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
            msg += "\n/clan online - Show's who's online in your clan.";
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
            msg += "\n\n<color=red>Other</color>";
            msg += "\n/rolldice <playername> <playername> etc - Rolls dice.";
            msg += "\n/ignore <playername> - Ignore Player.";
            msg += "\n/unignore <playername> - Unignore Player.";
            msg += "\n\n<color=grey>[DoomTown Chat Commands]</color>";
            return msg;
        }

        // Command Info Text Strings

        [ChatCommand("cmd")]
        void cmd_ShowCommands(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                PrintToChat(player, CommandsText_Admin());
            }
            else
                PrintToChat(player, CommandsText_Player());

            // Checks if the user is an admin or not and displays the appropriate command list
        }

        [ChatCommand("cmd_player")]
        void cmd_ShowCommands_Player(BasePlayer player, string cmd, string[] args)
        {
            PrintToChat(player, CommandsText_Player());

            // Used for admins to see the default player commands
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

            // Shows all the mute commands to the ADMINS ONLY
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

            // Shows all the filter commands to the ADMINS ONLY
        }

        [ChatCommand("cmd_clan")]
        void cmd_ShowCommands_Clans(BasePlayer player, string cmd, string[] args)
        {
            PrintToChat(player, CommandsText_Clans(isAdmin(player.UserIDString)));

            // Shows all the default clan commands to the regular player or the enhanced commands to the admin
        }
        #endregion

        #region System Hooks

        void Loaded()
        {
            LoadDefaultConfig();

            // Loading in the values from the configuration file

            allClans = Interface.Oxide.DataFileSystem.ReadObject<ClanData>("DoomChat_Clans_Data");
            allInvites = Interface.Oxide.DataFileSystem.ReadObject<InviteData>("DoomChat_Clans_Invites");
            allMutedPlayers = Interface.Oxide.DataFileSystem.ReadObject<MutedData>("DoomChat_MutedPlayers");
            allTradeChatIDs = Interface.Oxide.DataFileSystem.ReadObject<TradeChatData>("DoomChat_TradeChat");
            allIgnoreData = Interface.Oxide.DataFileSystem.ReadObject<IgnoreData>("DoomChat_IgnoreData");

            // Loading in the values from the data files

            var Online = BasePlayer.activePlayerList as List<BasePlayer>;

            foreach (BasePlayer player in Online)
            {
                if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "moderator"))
                {
                    if(list_ModeratorIDs.Contains(player.UserIDString) == false)
                        list_ModeratorIDs.Add(player.UserIDString);
                }

                // Adds moderator/admin to moderator list
                
                if (allClans.isInClan(player.UserIDString))
                {
                    list_UserToClanTags.Add(player.UserIDString, allClans.getClanTag(player.UserIDString));
                }    

                // Checks if player has a clan tag and tracks it
            }

        }

        void OnServerSave()
        {
            SaveInviteData();
            SaveClanData();
            SaveMuteList();
            SaveTradeChat();
            SaveIgnoreData();

            // Save Data to files
            
            SaveConfigurationChanges();

            // Update Configuration changes
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
            metric_diceRolls = Config.Get<int>("Metric_DiceRolls");

            foreach (string w in bwords)
            {
                list_FilteredWords.Add(w.ToUpper());
            }

            // Loading in all of the data from the /config/DoomChat.json configuration file
        }

        void OnPlayerInit(BasePlayer player)
        {
            // When a player connects to the server

            if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "moderator"))
            {
                list_ModeratorIDs.Add(player.UserIDString);
            }

            // If they are a moderator, track it

            if (allClans.isInClan(player.UserIDString))
            {
                list_UserToClanTags.Add(player.UserIDString, allClans.getClanTag(player.UserIDString));
            }

            // If they are in a clan, associate their clan id

            string clanName = allClans.getPlayerClan(player.UserIDString);

            // Get the players clan name

            if (clanName != "")
            {
                // If they are in a clan

                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    // For each member in the clan

                    if (IsOnlineAndValid(player, member))
                    {
                        // If the member is online

                        var foundPlayer = rust.FindPlayer(member);

                        if (foundPlayer != null && foundPlayer != player)
                            PrintToChat(foundPlayer, "<color=green>" + player.displayName + "</color> came online.");

                        // Let them know the user came online
                    }
                }
            }
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (list_ModeratorIDs.Contains(player.UserIDString))
                list_ModeratorIDs.Remove(player.UserIDString);

            if (list_UserToClanTags.ContainsKey(player.UserIDString))
                list_UserToClanTags.Remove(player.UserIDString);

            // Removes the player from the moderator tracking list and the playername to tag list

            string clanName = allClans.getPlayerClan(player.UserIDString);

            // Get the players clan name

            if (clanName != "")
            {
                // If the player is in a clan

                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    // For each member in the clan

                    if (IsOnlineAndValid(player, member))
                    {
                        // And if that memeber is online

                        var foundPlayer = rust.FindPlayer(member);

                        if (foundPlayer != null && foundPlayer != player)
                            PrintToChat(foundPlayer, "<color=red>" + player.displayName + "</color> went offline.");

                        // Let them know the user went offline
                    }
                }
            }

            // When a player disconnects or loses connection
        }

        object OnUserChat(IPlayer player, string message)
        {
            string styled = "";
            string colouredClanTag = allClans.getClanTagColoured(player.Id);

            if (allMutedPlayers.isMuted(player.Id))
            {
                // If the player IS muted

                if (allMutedPlayers.mutedStatus(player.Id) == false)
                {
                    // If the player has the mutefun status they will Mooo...

                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + muteText(message) + "</color>";

                    // Styles the message with coloured clan tag etc

                    ChatWithIgnore(player, styled);

                    // Broadcasts the message to anyone who doesn't have them on their ignore list
                }

                TellMods(player.Name, colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, true);

                // Tell the moderators (privately) what the original, offending message said
            }
            else
            {
                // If the player is NOT muted

                string msg = CleanMsg(player.Id, player.Name, message);

                // If the player tries to say something bad.. replace the text

                if(msg == Message_ScoldText && autoMute)
                {
                    var foundPlayer = rust.FindPlayer(player.Name);

                    if(foundPlayer != null)
                        rust.SendChatMessage(foundPlayer, "You have been muted, please contact an admin or moderator on steam/discord.", null, player.Id);
                }

                if (Message_ScoldText == msg)
                {
                    // If the message has been altered and replaced with the scold message e.g I have been muted for saying something bad...

                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + Message_ScoldText + "</color>";

                    // Styles the message with coloured clan tag etc

                    ChatWithIgnore(player, styled);

                    // Broadcasts the message to anyone who doesn't have them on their ignore list

                    TellMods(player.Name, colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, false);

                    // Tell the moderators (privately) what the original, offending message said
                }
                else
                {
                    if (isAdmin(player.Id))
                        styled = colouredClanTag + "<color=" + Color_AdminName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + message + "</color>";
                    else
                        styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + message + "</color>";

                    ChatWithIgnore(player, styled);

                    // Broadcasts the message to anyone who doesn't have them on their ignore list
                }

                Puts(player.Name + ": " + message);

                // Enter the original message into the console
            }
            
            metric_allMessages++;

            // CORE FUNCTION, This function intercepts all messages sent by the user, from admins to regular users

            return true;
        }

        #endregion

        #region Mute Player System

        [ChatCommand("automute")]
        void cmd_AutoMuteToggle(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                // Makes sure an admin/moderator is calling this command

                if (argsCheck(args, 1))
                {
                    // Make sure an argument is passed /automute on

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

            // Pretty straight forward
        }

        [ChatCommand("mute")]
        void cmd_MuteAdd(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                // Makes sure an admin/moderator is calling this command

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

                        // Prints the list of (online) muted players

                        PrintToChat(player, mlist);
                    }
                    else if (IsOnlineAndValid(player, args[0]))
                    {
                        // E.g /mute Andy
                        // Takes in args[0] as a name instead of a choice

                        var foundPlayer = rust.FindPlayer(args[0]);

                        // If the player is online to mute

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString) == false)
                        {
                            // If the player isn't muted yet

                            allMutedPlayers.addMutedPlayer_Manual(foundPlayer.UserIDString, foundPlayer.displayName, true);

                            // Add the player to the mute list without inputing a reason (e.g the filter word stuff)

                            PrintToChat(player, "Added " + foundPlayer.displayName + " to mute list.");

                            Puts("[MUTED] Player " + foundPlayer.displayName + " .");

                            SaveMuteList();

                            // Update the mute list data file
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
                // Makes sure an admin/moderator is calling this command

                if (args != null && args.Length > 0)
                {
                    int count = allMutedPlayers.getMutedCount();
                    string name = "";

                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        // If the muted player is online currently

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString))
                        {
                            // Check if they were muted

                            name = foundPlayer.displayName;
                            allMutedPlayers.removeMutedPlayer(foundPlayer.UserIDString);

                            // Remove player from the mute list and save to file

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
            // Mutes the player but anything they say in chat comes out in Moo's

            if (isAdmin(player.UserIDString))
            {
                // Makes sure an admin/moderator is calling this command

                if (args != null && args.Length > 0)
                {
                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        // If the muted player is online currently

                        if (allMutedPlayers.isMuted(foundPlayer.UserIDString) == false)
                        {
                            // If the player isn't muted already

                            allMutedPlayers.addMutedPlayer_Manual(foundPlayer.UserIDString, foundPlayer.displayName, false);

                            PrintToChat(player, "Added " + foundPlayer.displayName + " to mute list.");

                            // Add the name to the mute list and save

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

            // Save the mutelist to file
        }

        #endregion

        #region Filter System

        [ChatCommand("filter")]
        void cmd_WordFilters(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                // If the command was called by a moderator or admin

                #region args 1
                if (argsCheck(args, 1))
                {
                    string choice = args[0].ToUpper();
                    const int WORDSPERLINE = 7;

                    if (choice == "LIST")
                    {
                        string clist = "Filter List";
                        int count = 0;

                        foreach (string w in list_FilteredWords)
                        {
                            clist = clist + " | " + w;

                            if (count != 0 && count % WORDSPERLINE == 0)
                            {
                                clist += "\n";
                            }

                            ++count;
                        }

                        PrintToChat(player, clist);

                        // List all the filtered words... Privately to the admin that entered in the /filter list command
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

                            // Added the filter word to the DoomChat config file.

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

                            // Removed the filter word from the DoomChat config file.

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

            // Unsubscribes the player from the tradechat if they havn't been unsubscribed already
        }

        [ChatCommand("t")]
        void cmd_PostIntoTradeChat(BasePlayer player, string cmd, string[] args)
        {
            if (allMutedPlayers.isMuted(player.UserIDString) == false)
            {
                // If the player isn't muted

                if (anyArgsCheck(args))
                {
                    // Make sure they've entered text

                    if (allTradeChatIDs.doesExist(player.UserIDString))
                    {
                        allTradeChatIDs.removePlayer(player.UserIDString);
                        PrintToChat(player, "Subscribed to trade chat\nTo unsubscribe type /unsub");
                    }

                    // Subscribe the user to the chat if they aren't already

                    string msg = "";

                    for (int i = 0; i < args.Length; i++)
                    {
                        string tmp = "";

                        if (i > 0)
                            tmp = " ";

                        msg += tmp + args[i];
                    }

                    // Build message string from arguments

                    string fullMsg = " <color=" + Color_TradeText + ">[Trade] " + "</color><color=" + Color_PlayerName + ">" + player.displayName + ": </color>" + msg;

                    Puts("[Trade Chat] " + player.displayName + ": " + msg);

                    foreach (var p in BasePlayer.activePlayerList)
                    {
                        if (allTradeChatIDs.doesExist(p.UserIDString) == false)
                        {
                            // If person isn't in the unsub list

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

                    // Build message string from arguments

                    TellMods(player.displayName, "<color=" + Color_PlayerName + ">" + player.displayName + ": </color>", msg, true);
                }
            }
        }

        void SaveTradeChat()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_TradeChat", allTradeChatIDs);

            // Save the TradeChat ID's to file
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

        }// TradeChatData

        #endregion

        #region Clan System

        void SaveInviteData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_Clans_Invites", allInvites);

            // Save all the invites data to file
        }

        void SaveClanData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_Clans_Data", allClans);

            // Save all the clans data to file
        }

        bool CheckForInvite(string userID)
        {
            if (allInvites.pendingInvites.ContainsKey(userID))
                return true;
            else
                return false;

            // Checks if the player has a current pending invite
        }

        private void TellClan(BasePlayer player, ClanObj clan, string fullMsg)
        {
            foreach (string member in clan.members)
            {
                // Foreach member in the clan

                if (IsOnlineAndValid(player, member))
                {
                    var foundPlayer = rust.FindPlayer(member);

                    // If the player is online currently, send them a private clan message

                    if (foundPlayer != null)
                        rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                }
            }
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

                // Builds up the message from the arguments

                string clanName = allClans.getClanTag(player.UserIDString);
                string colouredTag = allClans.getClanByTag(clanName).tagColor;
                string fullMsg = "<color=" + colouredTag + ">" + "[CLAN CHAT] </color>" + "<color=" + Color_PlayerName + ">" + player.displayName + ":</color> " + msg;

                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    // Foreach member in the clan

                    if (IsOnlineAndValid(player, member))
                    {
                        var foundPlayer = rust.FindPlayer(member);

                        // If they are clan member online, privately pass along the users message

                        if (foundPlayer != null)
                            rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                    }
                }

                metric_clanMessages++;

                // Log the message to console

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
                // Admin command that displays the clan list privately

                const int PAGESIZE = 10;
                bool valid = true;

                int page = 0;

                if (anyArgsCheck(args))
                {
                    valid = int.TryParse(args[0], out page);

                    if (valid)
                        page = Convert.ToInt32(args[0]);
                }

                // Parse the page number e.g /clans 2

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
                    // Foreach clan, get the clan count

                    if (count >= startCount && count <= (startCount + PAGESIZE) && count < allClans.clansList.Count)
                        clist = clist + "\n<color=" + clan.tagColor + ">" + clan.tag + "</color> ---- Members: " + clan.members.Count;

                    ++count;
                }                             

                PrintToChat(player, clist);

                // Build up the clan list string and print it to chat
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
                    // If the player types in /clan it will display if they have an invite or not

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

                    // If the player has an invite

                    if (choice == "ACCEPT")
                    {
                        // If the argument passed was accept

                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " accepted!");

                        // Tell the player they have been accepted into the clan

                        TellClan(player, allClans.getClanByTag(allInvites.pendingInvites[player.UserIDString]), "Player " + player.displayName + " has joined the clan.");

                        // Tell the clan the player joined

                        allClans.getClanByTag(allInvites.pendingInvites[player.UserIDString]).members.Add(player.UserIDString);

                        // Update the clan information

                        list_UserToClanTags.Add(player.UserIDString, allInvites.pendingInvites[player.UserIDString]);

                        // Update the players clan tag on the fly

                        allInvites.pendingInvites.Remove(player.UserIDString);

                        // Remove the invitation

                        action = true;
                        SaveInviteData();
                        SaveClanData();

                        // Save changes to file
                    }

                    if (choice == "DECLINE")
                    {
                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " declined!");

                        // Tell the player they have declined the invite

                        TellClan(player, allClans.getClanByTag(allInvites.pendingInvites[player.UserIDString]), "Player " + player.displayName + " has declined the clan invite.");

                        // Tell the clan the player has declined the invite

                        allInvites.pendingInvites.Remove(player.UserIDString);

                        // Remove the pending invite

                        action = true;
                        SaveInviteData();
                        SaveClanData();

                        // Save the changes to file
                    }
                    #endregion
                }

                #region Clan Online List
                if (choice == "ONLINE")
                {
                    // Displays who's currently online in the clan

                    string clanTag = allClans.getPlayerClan(player.UserIDString);
                    string message = "";

                    if (clanTag != "")
                    {
                        // Make sure the player is in a clan

                        int count = 0;

                        foreach (string s in allClans.getClanByTag(clanTag).members)
                        {
                            // For each member in the clan

                            var foundPlayer = rust.FindPlayer(s);

                            if (foundPlayer != null)
                            {
                                if (IsOnlineAndValid(player, s))
                                {
                                    // Make sure they are online

                                    message += foundPlayer.displayName + " , ";

                                    // Add them to the string

                                    if (count != 0 && count % 5 == 0)
                                        message += "\n";

                                    // For every 5 names add a new line

                                    count++;

                                    // Track the player
                                }
                            }
                        }
                        
                        string premessage = "<color=orange>Clan Members Online for [" + clanTag + "]</color> - <color=yellow>[ " + count + " ] Online</color>\n";
                        
                        PrintToChat(player, premessage + message);

                        // Print the online list to the player
                    }

                    action = true;
                }
                #endregion

                #region Clan Leave
                if (choice == "LEAVE")
                {
                    string clanname = list_UserToClanTags[player.UserIDString];

                    if (isOwner)
                    {
                        ClanObj c = new ClanObj(clanname);

                        // Checks if the player leaving is the clan owner

                        if (allClans.clansList.Contains(c))
                        {
                            // If the clan exists

                            c = allClans.getClanByTag(clanname);

                            foreach (KeyValuePair<string, string> kv in allInvites.pendingInvites)
                            {
                                if (kv.Value == args[0])
                                {
                                    allInvites.pendingInvites.Remove(kv.Value);
                                }
                            }

                            // Remove all pending invites

                            foreach (string member in allClans.getClanByTag(clanname).members)
                            {
                                if (list_UserToClanTags.ContainsKey(member))
                                    list_UserToClanTags.Remove(member);
                            }

                            // Remove all the user to clantags records on the fly

                            ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + "has been dismantled by " + player.displayName + "." });

                            // Tell everyone ther clan has been dismantled

                            Puts("Clan " + c.tag + " has been dismantled by " + player.displayName + ".");

                            // Log it into the console

                            allClans.clansList.Remove(c);

                            // Remove the clan

                            action = true;
                            SaveClanData();
                            SaveInviteData();

                            // Save changes to file
                        }
                    }
                    else
                    {
                        PrintToChat(player, "You have left the clan " + clanname + " .");

                        // Tell the player they left the clan

                        TellClan(player, allClans.getClanByTag(clanname), "Player " + player.displayName + " has left the clan.");

                        // Tell the clan they left

                        allClans.leaveClan(player.UserIDString);

                        // Remove the player from the clan

                        if (list_UserToClanTags.ContainsKey(player.UserIDString))
                            list_UserToClanTags.Remove(player.UserIDString);

                        // Remove the player from the user to clan tags list on the fly

                        action = true;
                        SaveClanData();

                        // Save changes to file
                    }
                }

                if (!action)
                {
                    PrintToChat(player, "Bad or missing argument : /clan " + args[0]);
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
                        // Admin command Dismantle, removes trace of a clan

                        c = allClans.getClanByTag(tag);

                        if (c != null)
                        {
                            if (allClans.clansList.Contains(c))
                            {
                                // Checks that the clan exists

                                if (allClans.clansList.Remove(c))
                                {
                                    // Removes the clan

                                    foreach (KeyValuePair<string, string> kv in allInvites.pendingInvites)
                                    {
                                        if (kv.Value == tag)
                                        {
                                            allInvites.pendingInvites.Remove(kv.Value);
                                        }
                                    }

                                    // Removes all pending invites by that clan

                                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + "has been dismantled."});
                                    Puts("Clan [" + c.tag + "] has been dismantled by " + player.displayName + ".");

                                    // Alert the server and the console that the clan has been dismantled

                                    SaveClanData();
                                    SaveInviteData();

                                    // Save the clan and invite data
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
                if (choice == "INVITE")
                {
                    if (allClans.isOwner(player.UserIDString))
                    {
                        //If you are an owner of a clan you can send invites to other online players, who aren't in a clan already

                        if (IsOnlineAndValid(player, args[1]))
                        {
                            var foundPlayer = rust.FindPlayer(args[1]);

                            if (foundPlayer != null)
                            {
                                //If the player is online

                                if (allClans.isInClan(foundPlayer.UserIDString) == false)
                                {
                                    //If the player isn't in a clan already

                                    if (allInvites.pendingInvites.ContainsKey(foundPlayer.UserIDString))
                                    {
                                        //If they have a fight already don't invite them and tell the clan owner

                                        PrintToChat(player, "Player already has an invite");
                                    }
                                    else
                                    {
                                        // Track the pending invite and alert the player and the clan owner about the invite

                                        allInvites.pendingInvites.Add(foundPlayer.UserIDString, allClans.getClanByOwner(player.UserIDString).tag);
                                        PrintToChat(player, "Invited player " + foundPlayer.displayName + " to your clan.");
                                        PrintToChat(foundPlayer, "You have a clan invite from " + allClans.getClanByOwner(player.UserIDString).tag);

                                        // Update the invite data to file

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
                        // As a clan owner enables you to kick the player from the clan

                        var foundPlayer = rust.FindPlayer(args[1]);

                        if (foundPlayer != null)
                        {
                            if (foundPlayer != player)
                            {
                                // Make sure you aren't trying to kick yourself and that the player is online

                                TellClan(player, allClans.getClanByTag(allClans.getPlayerClan(foundPlayer.UserIDString)), "Player " + player.displayName + " has left the clan.");

                                // Tell everyone the player has been kicked from the clan

                                allClans.leaveClan(foundPlayer.UserIDString);

                                if (list_UserToClanTags.ContainsKey(foundPlayer.UserIDString))
                                    list_UserToClanTags.Remove(foundPlayer.UserIDString);

                                SaveClanData();

                                // Update clan data

                                PrintToChat(player, "Player " + foundPlayer.displayName + " has been kicked from the clan.");

                                // Tell the clan owner he has kicked the player from the clan
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
                            // If the player isn't in a clan

                            string tag = args[1].ToUpper();
                            string hexcolor = args[2];

                            // Parse the clan tag
                            // Parse the hex color value e.g #aabbcc

                            ClanObj c = new ClanObj(tag);

                            if (allClans.getClanByTag(tag) != null)
                            {
                                // If the clan exists already stop

                                PrintToChat(player, "Clan: " + tag + " already exits.");
                            }
                            else
                            {
                                if (tag.Length >= MINTAGSIZE && tag.Length <= MAXTAGSIZE && IsAlphaNumeric(Convert.ToString(tag)))
                                {
                                    // Make sure the clan tag is within a certain size and is alphanumeric

                                    if (ValidHex(hexcolor))
                                    {
                                        // Check that the hex value passed, is valid

                                        string userid = player.UserIDString;
                                        string t = tag.ToUpper();
                                        string tagColor = hexcolor;

                                        ClanObj clan = new ClanObj(userid, t, tagColor);

                                        // Create a new clan object

                                        allClans.clansList.Add(clan);

                                        // Add the clan object 

                                        list_UserToClanTags.Add(player.UserIDString, t);

                                        // Add the owner to the user to clan tags list on the fly

                                        SaveClanData();

                                        // Save the changes to file

                                        ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "The Clan " + "<color=" + tagColor + ">" + "[" + tag + "]" + "</color> " + "has been created by " + player.displayName });

                                        // Tell the player they have created the clan

                                        Puts("Clan [" + t + "] has been created by " + player.displayName + ".");

                                        // Track the made clan in the console
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

        }// InviteData

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

        }// ClanData

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

        }// ClanObj
        #endregion

        #region PM System
        [ChatCommand("pm")]
        void cmd_PrivateMessage(BasePlayer player, string cmd, string[] args)
        {
            if (allMutedPlayers.isMuted(player.UserIDString) == false)
            {
                // If the player isn't muted

                if (args.Length > 1)
                {
                    // If the player has provided a username

                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        // If the player they are trying to message online

                        string msg = "";

                        for (int i = 1; i < args.Length; i++)
                        {
                            string tmp = "";

                            if (i > 0)
                                tmp = " ";

                            msg += tmp + args[i];
                        }

                        // Build their message from the rest of the arguments

                        if (player.UserIDString != foundPlayer.UserIDString)
                        {
                            // Makes sure the player isn't trying to message themselves

                            string fullMsg = "<color=" + Color_PrivateMessageTag + ">" + Tag_PrivateMessage + " </color><color=" + Color_PlayerName + ">(" + player.displayName + " --> " + foundPlayer.displayName + "):</color>" + msg;

                            Puts("[PM] " + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                            // Log the message in the console

                            rust.SendChatMessage(player, fullMsg, null, player.UserIDString); 

                            // Displays the players own message to themselves

                            if (allIgnoreData.isIgnoringPlayer(foundPlayer.UserIDString, player.UserIDString) == false)
                            {
                                rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString); 
                                
                                // If the player isn't pm'ing somebody who has them actively ignored, send the message privately
                            }

                            UpdateLastReplied(player, foundPlayer);

                            // Update the last replied list 

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

                // Tell the player they are muted

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

                    // Build the message from the rest of the arguments and tell the mods what they tried to say

                    string colouredClanTag = allClans.getClanTagColoured(player.Id);

                    TellMods(player.displayName, colouredClanTag + "<color=" + Color_PlayerName + ">" + player.displayName + ": </color>", msg, true);
                }
            }

        }// Private Message another player

        [ChatCommand("r")]
        void cmd_PrivateMessageReply(BasePlayer player, string cmd, string[] args)
        {
            if (anyArgsCheck(args))
            {
                // Make sure the user is passing along a message and not anything blank

                if (allMutedPlayers.isMuted(player.UserIDString) == false)
                {

                    if (list_LastRepliedTo.ContainsKey(player.UserIDString))
                    {
                        // Check if the person has been messaged otherwise they can't reply to nobody

                        if (IsOnlineAndValid(player, list_LastRepliedTo[player.UserIDString]))
                        {
                            var foundPlayer = rust.FindPlayer(list_LastRepliedTo[player.UserIDString]);

                            // Make sure the user is online

                            string msg = " ";

                            for (int i = 0; i < args.Length; i++)
                            {
                                string tmp = "";

                                if (i > 0)
                                    tmp = " ";

                                msg += tmp + args[i];
                            }

                            // Build up the message

                            string fullMsg = "<color=" + Color_PrivateMessageTag + ">" + Tag_PrivateMessage + " </color><color=" + Color_PlayerName + ">(" + player.displayName + " --> " + foundPlayer.displayName + "):</color>" + msg;

                            Puts("[PM] " + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                            // Log the PM to file

                            rust.SendChatMessage(foundPlayer, fullMsg, null, player.UserIDString);
                            rust.SendChatMessage(player, fullMsg, null, player.UserIDString);

                            // Send the message to the original player and the player they are replying to privately

                            UpdateLastReplied(player, foundPlayer);

                            // Update that the player has replied to them
                        }
                        else
                            PrintToChat(player, "You havn't pm'd anyone yet nor has anyone pm'd you.");
                    }
                    else
                        PrintToChat(player, "Message was too short.");

                }// If not Muted
                else
                {
                    PrintToChat(player, YOUAREMUTED);

                    // Tell the player they are muted

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

                        // Build the message from the rest of the arguments and tell the mods what they tried to say

                        TellMods(player.displayName, "<color=" + Color_PlayerName + ">" + player.displayName + ": </color>", msg, true);
                    }
                }
            }

        }// Private Message another player

        #endregion

        #region Ignore System

        [ChatCommand("ignore")]
        void cmd_IgnoreUser(BasePlayer player, string cmd, string[] args)
        {
            if (argsCheck(args, 1))
            {
                // Makes sure the arguments include at least one parameter e.g /ignore <username>

                var foundPlayer = rust.FindPlayer(args[0]);

                if (foundPlayer != null)
                {
                    // If the player to be muted is online

                    if (allIgnoreData.isIgnoringPlayer(player.UserIDString, foundPlayer.UserIDString) == false)
                    {
                        // If the player isn't ignoring the player to be muted, yet..

                        if (foundPlayer.UserIDString != player.UserIDString)
                        {
                            // Check is the user is trying to mute themselves... edge case

                            allIgnoreData.ignorePlayer(player.UserIDString, foundPlayer.UserIDString);

                            // Track that the user is ignored and let the user know

                            PrintToChat(player, "You have ignored player " + foundPlayer.displayName + ".");
                        }
                        else
                        {
                            // Tell the user they can't mute themselves

                            PrintToChat(player, "You can't ignore yourself...*sad music*");
                        }
                    }
                    else
                    {
                        // Tell the user they are already ignoring the person

                        PrintToChat(player, "You are already ignoring that person.");
                    }
                }
                else
                {
                    PrintToChat(player, "Couldn't find a player by that name to ignore.");
                }
            }
            else
                PrintToChat(player, "Incorrect arguments e.g /ignore playername");
        }

        [ChatCommand("unignore")]
        void cmd_UnignoreUser(BasePlayer player, string cmd, string[] args)
        {
            if (argsCheck(args, 1))
            {
                // Makes sure the arguments include at least one parameter e.g /unignore <username>

                var foundPlayer = rust.FindPlayer(args[0]);

                if (foundPlayer != null)
                {
                    // Make sure the player exists

                    if (allIgnoreData.isIgnoringPlayer(player.UserIDString, foundPlayer.UserIDString))
                    {
                        // Check is the player is ignoring the username provided

                        allIgnoreData.unIgnorePlayer(player.UserIDString, foundPlayer.UserIDString);
                        PrintToChat(player, "You have unignored player " + foundPlayer.displayName + ".");
                    }
                    else
                    {
                        // Let the player know they aren't ignoring the user

                        PrintToChat(player, "You don't have this player ignored.");
                    }
                }
                else
                {
                    // Let the player know we couldn't find a user by that name to unignore

                    PrintToChat(player, "Couldn't find a player by that name to unignore.");
                }
            }
            else
                PrintToChat(player, "Incorrect arguments e.g /ignore playername");
        }

        void ChatWithIgnore(IPlayer playerTalking, string theirMessage)
        {
            // Most messages pass through this method, this checks if a player, has another player ignored
            // If the player does have them ignored, the message won't be broadcasted to them

            foreach (var playerIgnoring in BasePlayer.activePlayerList)
            {
                // For everyone on the server

                if (allIgnoreData.isIgnoringPeople(playerIgnoring.UserIDString))
                {
                    // If the player is ignoring ANYONE online (some people don't bother with the ignore list) then..

                    if (allIgnoreData.isIgnoringPlayer(playerIgnoring.UserIDString, playerTalking.Id) == false)
                    {
                        // If the player isn't being ignored by the person on the server, send them a message

                        rust.SendChatMessage(playerIgnoring, theirMessage, null, playerTalking.Id);
                    }
                }
                else
                {
                    // If the player on the server isn't ignoring anyone, send them the message

                    rust.SendChatMessage(playerIgnoring, theirMessage, null, playerTalking.Id);
                }
            }
        }

        void SaveIgnoreData()
        {
            // Save all the players ignore list to file

            Interface.Oxide.DataFileSystem.WriteObject("DoomChat_IgnoreData", allIgnoreData);
        }

        class IgnoreData
        {
            public List<IgnoreOb> allIgnoreData { get; set; }

            public IgnoreData()
            {
                allIgnoreData = new List<IgnoreOb>();
            }

            public bool isIgnoringPeople(string userID)
            {
                if (allIgnoreData.Contains(new IgnoreOb(userID)))
                    return true;
                else
                    return false;
            }

            public bool isIgnoringPlayer(string player, string ignoredPlayer)
            {
                foreach (IgnoreOb o in allIgnoreData)
                {
                    if (player == o.userID)
                    {
                        if (o.isIgnoringPlayer(ignoredPlayer))
                            return true;
                    }
                }

                return false;
            }

            public IgnoreOb getPlayer(string p)
            {
                foreach (IgnoreOb o in allIgnoreData)
                {
                    if (o.userID == p)
                        return o;
                }

                return null;
            }

            public void ignorePlayer(string player, string ignoredPlayer)
            {
                IgnoreOb p = new IgnoreOb(player);

                if (allIgnoreData.Contains(p) == false)
                {
                    p.ignoreList.Add(ignoredPlayer);

                    allIgnoreData.Add(p);
                }
                else
                {
                    foreach (IgnoreOb o in allIgnoreData)
                    {
                        if (o.userID == player)
                        {
                            if (o.ignoreList.Contains(ignoredPlayer) == false)
                                o.ignoreList.Add(ignoredPlayer);
                        }
                    }
                }
            }

            public void unIgnorePlayer(string player, string ignoredPlayer)
            {
                foreach (IgnoreOb p in allIgnoreData)
                {
                    if (p.userID == player)
                    {
                        if (p.ignoreList.Contains(ignoredPlayer))
                            p.ignoreList.Remove(ignoredPlayer);
                    }
                }
            }

        }// IgnoreData

        class IgnoreOb
        {
            public string userID { get; set; }
            public HashSet<string> ignoreList { get; set; }

            public IgnoreOb(string userID)
            {
                ignoreList = new HashSet<string>();
                this.userID = userID;
            }

            public void ignorePlayer(string id)
            {
                if (ignoreList.Contains(id))
                    ignoreList.Remove(id);
            }

            public void unignorePlayer(string id)
            {
                if (ignoreList.Contains(id) == false)
                    ignoreList.Add(id);
            }

            public bool isIgnoringPlayer(string id)
            {
                if (ignoreList == null)
                    return false;

                if (ignoreList.Contains(id))
                    return true;

                return false;
            }

            public override bool Equals(object obj)
            {
                var item = obj as IgnoreOb;

                if (this.userID != null && item != null)
                {
                    if (this.userID != "")
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

        }// IgnoreOb

        #endregion

        #region Poke
        [ChatCommand("poke")]
        void cmd_CheckOnline(BasePlayer player, string cmd, string[] args)
        {
            if (argsCheck(args, 1))
            {
                if (IsOnlineAndValid(player, args[0]))
                {
                    // Checks that the player is online and if they are, tell the player who's poking

                    string online = "<color=#99ff66>[ The player " + IsOnlinePoke(player, args[0]) + " is ONLINE ]</color>";

                    PrintToChat(player, online);
                }
                else
                {
                    // If the player is offline tell the player who's poking

                    string offline = "<color=#a9a9a9>[ The player " + args[0] + " is OFFLINE ]</color>";

                    PrintToChat(player, offline);
                }
            }
            else
                PrintToChat(player, "Please enter proper arguments. Example /online Andy");

            metric_pokes++;
        }
        #endregion

        #region Metrics

        [ChatCommand("metrics")]
        void cmd_Metrics(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
                PrintToChat(player, Metrics_Text());
            else
                NoPerms(player, args[0]);

            // If the user is an admin/moderator, prints all of the metrics (privately) to chat.
        }

        #endregion

        #region Roll Dice

        [ChatCommand("rolldice")]
        void cmd_DiceRoll(BasePlayer player, string cmd, string[] args)
        {
            if (anyArgsCheck(args))
            {
                // Makes at least one username is passed to this arguments /rolldice 8bit

                System.Random r = new System.Random();
                Dictionary<string, int> nameNumber = new Dictionary<string, int>();

                string message = "<color=orange>Dice Rolled</color> - <color=yellow>Winner is closest to 100</color>\n";
                string WinnerName = player.displayName;
                int num = r.Next(100);

                nameNumber.Add(player.displayName, num);
                message += player.displayName + " --  [ " + num + " ]";

                // Adds the person who calls the command to the Dictionary initially

                foreach (string name in args)
                {
                    // For each name

                    if (IsOnlineAndValid(player, name))
                    {
                        var foundPlayer = rust.FindPlayer(name);

                        if (foundPlayer != null)
                        {
                            // Make sure they are online

                            if (nameNumber.ContainsKey(foundPlayer.displayName) == false)
                            {
                                // Makes sure the person isn't the person who called the command

                                num = r.Next(100);
                                nameNumber.Add(foundPlayer.displayName, num);
                                message += "\n" + foundPlayer.displayName + "--  [ " + num + " ]";

                                // Adds the user and dice roll to the message
                            }
                        }
                    }
                }

                foreach (KeyValuePair<string, int> kv in nameNumber)
                {
                    var foundPlayer = rust.FindPlayer(kv.Key);

                    if (foundPlayer != null)
                        rust.SendChatMessage(foundPlayer, message, null, player.UserIDString);

                    // Sends the results to everyone who was added to the pool
                }

                metric_diceRolls++;
            }
            else
            {
                PrintToChat(player, "To roll dice please follow the format.\nE.g /rolldice Andy 8Bit hogan");
            }
        }

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

            // Checks whether a string is alphanumeric or not

            return true;
        }

        bool anyArgsCheck(string[] args)
        {
            // Makes sure the argument array isn't null

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
            // Checks if the argument array countains the right amount of arguments
            // Also makes sure the argument array isn't null

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
                    return false;
                }
            }

            // Checks if the user is online and the username is valid
        }

        public string IsOnlinePoke(BasePlayer player, string partialName)
        {
            var foundPlayer = rust.FindPlayer(partialName);

            return foundPlayer.displayName;

            // Checks if the user is online and returns their proper username
        }

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
            Config["Metric_DiceRolls"] = metric_diceRolls;

            SaveConfig();

            // Saves all changes made by in game admin commands to the configuration file.
        }

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

            // Private message all moderators and admins
        }

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

            // Update last person replied too list
        }

        private bool isAdmin(string playerID)
        {
            if (list_ModeratorIDs.Contains(playerID))
                return true;
            else
                return false;

            // Check if the player is an admin/moderator
        }

        private string CleanMsg(string userid, string displayname, string msg)
        {
            string original = msg;
            string upper = msg.ToUpper();

            foreach (string word in list_FilteredWords)
            {
                // For each word on the offending words list

                if (upper.Contains(word))
                {
                    // If the message contains a bad word...

                    if (autoMute)
                    {
                        // If automute is on

                        Puts("AUTOMUTED for saying: " + msg + " offending word: " + word);

                        // Log in the console

                        allMutedPlayers.addMutedPlayer_Logged(userid, displayname, true, msg, word);

                        // Add the users id,displayname,original message and offending filtered word to the data file

                        SaveMuteList();

                        // Update the file immediately
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

            // If the player has no permission to use that command, give them no permission text
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

            // Checks if the string is a valid hex color e.g #00ff11

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

            // Inputs a word e.g "Hello Buddy" will return something like "MoOoo mooo" or something similar

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

            // Checks how many words are in a sentence

            return count;
        }

        int randNum()
        {
            int MIN = 2;
            int MAX = 5;

            return rnd.Next(2, 5);

            // Returns a random number between MIN & MAX
        }

        #endregion
    }

}//namespace