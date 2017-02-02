using System;
using System.Collections.Generic;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("DoomChat", "AndyDev2013 & wski", "2.0.0")]
    [Description("Custom Chat Plugin for DoomTown Rust Server")]
    class DoomChat : RustPlugin
    {
        private HashSet<string> list_FilteredWords = new HashSet<string>();
        private HashSet<string> list_ModeratorIDs = new HashSet<string>();
        private HashSet<string> list_TradeChatIDs = new HashSet<string>();

        private Dictionary<string, bool> list_MutedPlayers = new Dictionary<string, bool>();
        private Dictionary<string, string> list_LastRepliedTo = new Dictionary<string, string>();

        private Dictionary<string, string> list_UserToClanTags = new Dictionary<string, string>();

        private ClanData allClans = new ClanData();
        private InviteData allInvites = new InviteData();

        System.Random rnd = new System.Random();

        const int MINTAGSIZE = 3;
        const int MAXTAGSIZE = 5;

        const string CLANERRORMSG = "Please follow the format. Example /clan_create BACCA #663300";
        const string ERROR = "You couldn't update the configuration file for some reason...";
        const string UNKNOWN = "Unknown Command: ";

        string Tag_Warning = "[CENSORED]";
        string Tag_Muted = "[MUTED]";
        string Tag_PrivateMessage = "[PM]";
        string Message_ScoldText = "I said a naughty word... I'm looking to get banned..";


        string Color_WordFilterTag = "#ff0707";
        string Color_MutedFilterTag = "#fcfc71";
        string Color_PrivateMessageTag = "#660066";
        string Color_AdminName = "#bef25e";
        string Color_PlayerName = "#797a79";
        string Color_GlobalText = "#e5e5e5";
        string Color_TradeText = "#ff99dd";

        /* End of Global Variables */

        #region Command List

        string CommandsText_Admin()
        {
            string msg = "";
            msg += "[DoomTown Admin Chat Commands]\n";
            msg += "\n/cmd - Lists admin commands.";
            msg += "\n/cmd_player - Lists player commands.";
            msg += "\n/cmd_mute - Lists all mute commands";
            msg += "\n/cmd_filters - Lists all word filter commands";
            msg += "\n/cmd_clan - Lists all word filter commands";
            msg += "\n";
            msg += "\n/t <text> - Post in trade chat";
            msg += "\n/unsub - Unsub from trade chat so you no longer see trades.";
            msg += "\n/poke <playername> - Check if a user if online or offline.";
            msg += "\n/pm <playername> - Private message a player if they are online.";
            msg += "\n/r - Reply to the last person who messaged you.";
            msg += "\n\n[DoomTown Admin Chat Commands]";
            return msg;
        }

        string CommandsText_Mute()
        {
            string msg = "";
            msg += "[Admin Mute Commands]\n";
            msg += "\n/cmd_mute - Lists all mute commands";
            msg += "\n/mute_list - Lists muted users.";
            msg += "\n/mute <username> - Mutes user.";
            msg += "\n/unmute <username> - Unmutes the user.";
            msg += "\n/mutefun <username> - Mutes user and makes them moo.";
            msg += "\n\n[Admin Mute Commands]";
            return msg;
        }

        string CommandsText_Filters()
        {
            string msg = "";
            msg += "[Admin Filter Commands]\n";
            msg += "\n/filter_list - Lists filtered words.";
            msg += "\n/filter_add <text> - Adds word to filter list.";
            msg += "\n/filter_remove <text> - Removes word from filter list.";
            msg += "\n\n[Admin Filter Commands]";
            return msg;
        }

        string CommandsText_Clans(bool admin)
        {
            string msg = "";
            string adminline = "\n/clan_remove <text> - Removes clan. \n/clan_list <text> - Lists clans.";

            msg += "[Clan Commands]\n";
            msg += "\n/c <text> - Talk in clan chat.";
            if (admin)
                msg += adminline;
            msg += "\n/clan_create <tag letters> <hex colour> - Create a clan.";
            msg += "\n/clan_invite <playername> - Invites player to clan.";
            msg += "\n/clan - Shows current invite if any.";
            msg += "\n/clan accept - Join clan if you have an invite.";
            msg += "\n/clan decline - Join clan if you have an invite.";
            msg += "\n/clan leave - Leave a clan if you are in one.";
            msg += "\n\n[Clan Commands]";
            return msg;
        }

        string CommandsText_Player()
        {
            string msg = "";
            msg += "[DoomTown Chat Commands]";
            msg += "\n\n/cmd - Lists commands.";
            msg += "\n/cmd_clan - Lists all the clan commands";
            msg += "\n/pm <playername> - Private message a player if they are online.";
            msg += "\n/r - Reply to the last person who messaged you.";
            msg += "\n/poke <playername> - Check if a user if online or offline.";
            msg += "\n/t <text> - Post in trade chat";
            msg += "\n/unsub - Unsub from trade chat so you no longer see trades.";
            msg += "\n\n[DoomTown Chat Commands]";
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

            allClans = Interface.Oxide.DataFileSystem.ReadObject<ClanData>("Clans_Data");
            allInvites = Interface.Oxide.DataFileSystem.ReadObject<InviteData>("Clans_Invites");

            var Online = BasePlayer.activePlayerList as List<BasePlayer>;

            foreach (BasePlayer player in Online)
            {
                if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "mod"))
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

            Puts("Server just saved DoomChat stuffs");
        }

        protected override void LoadDefaultConfig()
        {
            List<string> bwords = new List<string>();

            bwords = Config.Get<List<string>>("Badwords");
            list_MutedPlayers = Config.Get<Dictionary<string, bool>>("MuteList");

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

            foreach (string w in bwords)
            {
                list_FilteredWords.Add(w.ToUpper());
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (permission.UserHasGroup(player.UserIDString, "admin") || permission.UserHasGroup(player.UserIDString, "mod"))
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

        }// Checking if the player is a moderator or admin and adding them to the moderator list.

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (list_ModeratorIDs.Contains(player.UserIDString))
            {
                list_ModeratorIDs.Remove(player.UserIDString);
            }

            if (list_TradeChatIDs.Contains(player.UserIDString))
            {
                list_TradeChatIDs.Remove(player.UserIDString);
            }

            if (list_UserToClanTags.ContainsKey(player.UserIDString))
            {
                list_UserToClanTags.Remove(player.UserIDString);
            }

        }// If the player was a moderator or admin and was registed on the moderator list, they are removed on disconnect.

        object OnUserChat(IPlayer player, string message)
        {
            string styled = "";
            string colouredClanTag = allClans.getClanTagColoured(player.Id);

            if (isMuted(player) == false)
            {
                string msg = CleanMsg(message);

                if (Message_ScoldText == msg)
                {
                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + Message_ScoldText + "</color>";

                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player.Id, styled });

                    TellMods(colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, false);
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
                if (!mutedType(player))
                {
                    styled = colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + ": </color><color=" + Color_GlobalText + ">" + muteText(message) + "</color>";
                    ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player.Id, styled });
                }

                TellMods(colouredClanTag + "<color=" + Color_PlayerName + ">" + player.Name + "</color>", message, true);
            }

            return true;
        }

        #endregion            

        #region Word Filter System

        [ChatCommand("filter_list")]
        void cmd_ShowCurses(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                string clist = "Curse List";

                foreach (string w in list_FilteredWords)
                {
                    clist = clist + " | " + w;
                }

                PrintToChat(player, clist);
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("filter_add")]
        void cmd_AddCurse(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null)
                {
                    string tmp = args[0].ToUpper();
                    string msg = tmp + " added to the badwords config.";

                    list_FilteredWords.Add(tmp);

                    //SaveConfigurationChanges(); // Moved to server save function

                    PrintToChat(player, msg);
                }
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("filter_remove")]
        void cmd_RemoveCurse(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null)
                {
                    string tmp = args[0].ToUpper();
                    string msg = tmp + " removed from the badwords config.";

                    if (list_FilteredWords.Contains(tmp))
                    {
                        list_FilteredWords.Remove(tmp);
                    }

                    //SaveConfigurationChanges(); //Done on server save

                    PrintToChat(player, msg);
                }
            }
            else
                NoPerms(player, args[0]);
        }

        #endregion System System

        #region Mute Player System
        [ChatCommand("mute_list")]
        void cmd_MutePlayerList(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                string mlist = "Mute List";

                foreach (KeyValuePair<string, bool> p in list_MutedPlayers)
                {
                    if (IsOnlineAndValid(player, player.displayName))
                    {
                        mlist = mlist + "\n- " + player.displayName;
                    }
                }

                PrintToChat(player, mlist);
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
                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        if (list_MutedPlayers.ContainsKey(foundPlayer.UserIDString) == false)
                        {
                            list_MutedPlayers.Add(foundPlayer.UserIDString, true);
                            PrintToChat(player, "Added " + foundPlayer.displayName + " to mute list.");
                            //SaveConfigurationChanges(); //Done on server save
                        }
                        else
                            PrintToChat(player, args[0] + " already added to mute list");
                    }
                    else
                    {
                        PrintToChat(player, args[0] + " not found please type the full name");
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

                    int count = list_MutedPlayers.Count;
                    string name = "";

                    if (IsOnlineAndValid(player, args[0]))
                    {
                        var foundPlayer = rust.FindPlayer(args[0]);

                        if (list_MutedPlayers.ContainsKey(foundPlayer.UserIDString))
                        {
                            name = foundPlayer.displayName;
                            list_MutedPlayers.Remove(foundPlayer.UserIDString);
                            //SaveConfigurationChanges(); // Done on server save
                        }
                    }

                    if (count != list_MutedPlayers.Count)
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

                        if (list_MutedPlayers.ContainsKey(foundPlayer.UserIDString) == false)
                        {
                            list_MutedPlayers.Add(foundPlayer.UserIDString, false);
                            PrintToChat(player, "Added: " + foundPlayer.displayName + " to mute list.");
                            //SaveConfigurationChanges(); //Done on server save
                        }
                        else
                            PrintToChat(player, args[0] + " already added to mute list");
                    }
                    else
                    {
                        PrintToChat(player, args[0] + " not found please type the full name");
                    }
                }
            }
            else
                NoPerms(player, cmd);
        }
        #endregion

        #region Trade System
        [ChatCommand("t")]
        void cmd_PostIntoTradeChat(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length > 1)
            {
                if (list_TradeChatIDs.Contains(player.UserIDString) == false)
                {
                    list_TradeChatIDs.Add(player.UserIDString);
                    PrintToChat(player, "Subbed to trade chat.");
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

                Puts(fullMsg);

                foreach (string playerID in list_TradeChatIDs)
                {
                    var foundPlayer = rust.FindPlayer(playerID);

                    if (foundPlayer != null)
                        PrintToChat(foundPlayer, fullMsg);
                }
            }
            else
                PrintToChat(player, "Message was too short.");
        }

        [ChatCommand("unsub")]
        void cmd_UnsubTradeChat(BasePlayer player, string cmd, string[] args)
        {
            list_TradeChatIDs.Remove(player.UserIDString);
            PrintToChat(player, "Unsubbed from trade chat.");
        }
        #endregion

        #region Clan System

        void SaveInviteData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Clans_Invites", allInvites);
        }

        void SaveClanData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("Clans_Data", allClans);
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

                foreach (string member in allClans.getClanByTag(clanName).members)
                {
                    if (IsOnlineAndValid(player, member))
                    {
                        var foundPlayer = rust.FindPlayer(member);

                        PrintToChat(foundPlayer, "<color=" + colouredTag + ">" + "[CLAN CHAT] </color>" + "<color=" + Color_PlayerName + ">" + player.displayName + ":</color> " + msg);
                    }
                }

                Puts("[CLAN CHAT]" + player.displayName + ": " + msg);
            }
            else
                PrintToChat(player, "You are not in a clan.");
        }

        [ChatCommand("clan_list")]
        void cmd_ClanList(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                string clist = "Clan List";

                foreach (ClanObj clan in allClans.clansList)
                {
                    clist = clist + "\n<color=" + clan.tagColor + ">" + clan.tag + "</color> ---- Members: " + clan.members.Count;
                }

                PrintToChat(player, clist);
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("clan_remove")]
        void cmd_DismantleClan(BasePlayer player, string cmd, string[] args)
        {
            if (isAdmin(player.UserIDString))
            {
                if (args != null)
                {
                    ClanObj c = new ClanObj(args[0]);

                    if (allClans.clansList.Contains(c))
                    {
                        allClans.clansList.Remove(c);

                        foreach (KeyValuePair<string, string> kv in allInvites.pendingInvites)
                        {
                            if (kv.Value == args[0])
                            {
                                allInvites.pendingInvites.Remove(kv.Value);
                            }
                        }

                        ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + " has been dismantled by " + player.displayName });

                        //SaveClanData(); // Done on server save
                    }
                    else
                        PrintToChat(player, "Couldn't find the " + c.tag + " clan to dismantle");
                }
            }
            else
                NoPerms(player, args[0]);
        }

        [ChatCommand("clan_create")]
        void cmd_CreateClan(BasePlayer player, string cmd, string[] args)
        {
            if (allClans != null && args != null)
            {
                if (allClans.isInClan(player.UserIDString) == false)
                {
                    if (args.Length == 2)
                    {
                        string tag = args[0];

                        ClanObj c = new ClanObj(tag);

                        if (allClans.clansList.Contains(c))
                        {
                            PrintToChat(player, "Clan: " + args[0] + " already exits");
                        }
                        else
                        {
                            if (args[0].Length >= MINTAGSIZE && args[0].Length <= MAXTAGSIZE && IsAlphaNumeric(Convert.ToString(args[0])))
                            {
                                if (ValidHex(args[1]))
                                {
                                    string userid = player.UserIDString;
                                    string t = args[0].ToUpper();
                                    string tagColor = args[1];

                                    ClanObj clan = new ClanObj(userid, t, tagColor);

                                    allClans.clansList.Add(clan);

                                    list_UserToClanTags.Add(player.UserIDString, t);

                                    //SaveClanData(); // Done on server save

                                    PrintToChat(player, "Clan <color=" + tagColor + ">" + t + "</color> Created");
                                }
                                else
                                    PrintToChat(player, CLANERRORMSG);
                            }
                            else
                                PrintToChat(player, "Tag size can only be " + MAXTAGSIZE + " characters long and " + MINTAGSIZE + " characters short." + CLANERRORMSG);
                        }
                    }
                    else
                        PrintToChat(player, CLANERRORMSG);
                }
                else
                    PrintToChat(player, "You are already in a clan.");
            }
            else
                PrintToChat(player, "Clans are unavailable");
        }

        [ChatCommand("clan_invite")]
        void cmd_InviteClan(BasePlayer player, string cmd, string[] args)
        {
            if (allClans.isOwner(player.UserIDString))
            {
                if (IsOnlineAndValid(player, args[0]))
                {
                    var foundPlayer = rust.FindPlayer(args[0]);

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

                                //SaveInviteData(); // Done on server save
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

        [ChatCommand("clan")]
        void cmd_ClanDecision(BasePlayer player, string cmd, string[] args)
        {
            bool InClan = allClans.isInClan(player.UserIDString);
            bool left = false;

            if (argsCheck(args))
            {
                string choice = args[0].ToUpper();

                if (InClan)
                {
                    if (choice == "LEAVE")
                    {
                        left = true;

                        bool isOwner = allClans.isOwner(player.UserIDString);

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

                                // Scrapping Invites

                                foreach (string member in allClans.getClanByTag(clanname).members)
                                {
                                    if (list_UserToClanTags.ContainsKey(member))
                                        list_UserToClanTags.Remove(member);
                                }

                                // Removing Tags on list

                                ConsoleNetwork.BroadcastToAllClients("chat.add", new object[] { player, "Clan " + "<color=" + c.tagColor + ">" + "[" + c.tag + "]" + "</color> " + "has been dismantled by " + player.displayName });

                                // Removing Clan

                                Puts("Clan count: " + allClans.clansList.Count);

                                allClans.clansList.Remove(c);

                                Puts("Clan count: " + allClans.clansList.Count);

                                //SaveClanData(); // Done on server save
                                //SaveInviteData(); // Done on server save
                            }
                            else
                            {
                                allClans.leaveClan(player.UserIDString);

                                //SaveClanData(); // Done on server save
                            }
                        }
                        else
                        {
                            allClans.leaveClan(player.UserIDString);

                            //SaveClanData(); // Done on server save
                        }
                    }
                }
                else if (allInvites.pendingInvites.ContainsKey(player.UserIDString))
                {
                    PrintToChat(player, "You have a pending invite from clan " + allInvites.pendingInvites[player.UserIDString]);

                    if (choice == "ACCEPT")
                    {
                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " accepted!");

                        allClans.getClanByTag(allInvites.pendingInvites[player.UserIDString]).members.Add(player.UserIDString);

                        list_UserToClanTags.Add(player.UserIDString, allInvites.pendingInvites[player.UserIDString]);

                        allInvites.pendingInvites.Remove(player.UserIDString);

                        //SaveInviteData();// Done on server save
                        //SaveClanData(); // Done on server save
                    }

                    if (choice == "DECLINE")
                    {
                        PrintToChat(player, "Invite to " + allInvites.pendingInvites[player.UserIDString] + " declined!");
                        allInvites.pendingInvites.Remove(player.UserIDString);

                        //SaveInviteData();// Done on server save
                        //SaveClanData(); // Done on server save
                    }
                }
            }

            if (InClan && left == false)
            {
                PrintToChat(player, "You are already in a clan. Type /cmd_clan for all commands.");
            }
            else
            {
                if (allInvites.pendingInvites.ContainsKey(player.UserIDString))
                {
                    PrintToChat(player, "You have a pending invite for clan " + allInvites.pendingInvites[player.UserIDString]);
                }
                else
                    PrintToChat(player, "You have no pending invites.");
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
                        string online = "<color=green>" + args[0] + " is online.</color>";

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
        }
        #endregion

        #region PM System
        [ChatCommand("pm")]
        void cmd_PrivateMessage(BasePlayer player, string cmd, string[] args)
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

                        Puts("[PM]" + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                        PrintToChat(foundPlayer, fullMsg);
                        PrintToChat(player, fullMsg);

                        UpdateLastReplied(player, foundPlayer);
                    }
                    else
                        PrintToChat(player, "You can't PM yourself.");
                }
            }
            else
                PrintToChat(player, "Message was too short.");

        }// Private Message another player

        [ChatCommand("r")]
        void cmd_PrivateMessageReply(BasePlayer player, string cmd, string[] args)
        {
            if (args.Length > 0)
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

                        Puts("[PM]" + player.displayName + " to " + foundPlayer.displayName + " : " + msg);

                        PrintToChat(foundPlayer, fullMsg);
                        PrintToChat(player, fullMsg);

                        UpdateLastReplied(player, foundPlayer); // DOUBLE CHECK THIS
                    }
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

        bool argsCheck(string[] args)
        {
            if (args == null)
                return false;

            if (args.Length == 0)
                return false;

            if (args[0] == null)
                return false;

            if (args[0] == "")
                return false;

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
                    PrintToChat(player, $"{foundPlayer.displayName} is not online, try again later!");
                    return false;
                }
            }

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
            Config["MuteList"] = list_MutedPlayers;

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

        private void TellMods(string name, string originalMessage, bool flag)
        {
            string msg = "";

            if (!flag)
                msg = "<color=" + Color_WordFilterTag + ">" + Tag_Warning + "</color> " + name + ": " + originalMessage;
            else
                msg = "<color=" + Color_MutedFilterTag + ">" + Tag_Muted + "</color> " + name + ": " + originalMessage;

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

        private bool isMuted(IPlayer player)
        {
            if (list_MutedPlayers.ContainsKey(player.Id))
                return true;
            else
                return false;

        }// Check is player is on the mutedplayer list

        private bool mutedType(IPlayer player)
        {
            if (list_MutedPlayers.ContainsKey(player.Id))
            {
                return list_MutedPlayers[player.Id];
            }

            return false;

        }// Check is player is on the mutedplayer list

        private string CleanMsg(string msg)
        {
            string original = msg;
            string upper = msg.ToUpper();

            foreach (string word in list_FilteredWords)
            {
                if (upper.Contains(word))
                {
                    return Message_ScoldText;
                }
            }

            return original;
        }

        void NoPerms(BasePlayer player, string arg)
        {
            string msg = UNKNOWN + arg + " no permission";
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
            {
                if (msg[i].Equals(sp))
                {
                    count++;
                }
            }

            return count;
        }

        int randNum()
        {
            return rnd.Next(2, 5);
        }

        #endregion
    }

}//namespace