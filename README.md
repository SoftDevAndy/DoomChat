# rustoxidechatmoderator

Custom plugins written to work with Oxide &amp; Rust. This plugin was launched on http://doomtown.io/ which is a popular modded Rust server. 

This plugin was created out of a necessity to moderate the chat in game.

### Features

- Mute System
- Filter System
- Trade Chat System
- Clan System
- Private Message System 
- Ignore System
- Poke
- Metrics
- Roll Dice

### Mute System (Admin/Moderator Command)

```
/automute <true>/<false>/<on>/<off>
```

***Switches automute on or off.***

If automute is switched on, instead of the offending message being just censored the user is muted automatically and the offending message and information about the automute is recorded to the file
```
data/DoomChat_MutedPlayers.json
```

in the following form

```
{
  "playerList": [
    {
      "userID": "XXXXXXXXXXXXXX",
      "displayName": "Player Display Name",
      "muteStatus": true,
      "offendingMessage": "I am testing the filter system using the word heck.",
      "offendingWord": "HECK"
    }
  ]
}
```

### Filter System (Admin/Moderator Command)

```
/filter list
```

Privately displays the list of filtered words to the admin/moderator that calls the command.

```
/filter add <word>
```

Allows the administrator add a word to the chat filter.

```
/filter remove <word>
```

Allows the administrator remove a word from the chat filter.

### Trade Chat

By default, everyone is included in the trade chat unless they opt out. This is to encourage the use of tradechat.

***Unsubscribes the player from trade chat***
```
/unsub
```

***Lets the user post a message in trade chat***
```
/t <message>
```

### Clan System

***Display list of clans (Admin/Moderator command)***

```
/clans
```
```
/clans <pagenumber>
```

***Clan Dismantle (Admin/Moderator command)***
Dismantles a clan using the clan name
```
/clan dismantle <clanname>
```

***Create a clan***
Create a colourized clan tag. Being a member of the clan lets you use a private clan chat.

```
/clan create <tag> <hexvalue>
```
***Lets you talk in clan chat***
```
/c <message>
```
***Shows whether the user has a pending invite to a clan or not***
```
/clan
```
***Accept or Decline a clan invitation***
A user can only have one invitation at a time and must decline their existing invitation before accepting another.
```
/clan <accept>/<decline>
```

***Show who's online in the clan currently***
```
/clan online
```

***Leave/Disband current clan***
If the leader/creator of the clan leaves the clan it disbands automatically. If they are just a member, the clan keeps existing and the player leaves.

```
/clan leave
```

***Clan Kick***
If the player is the leader of the clan, they can kick other users from the clan by name.
```
/clan kick <playername>
```

***Clan Invite***
Invites the player to the clan. This command can only be used by the creator of the clan.
```
/clan invite <playername>
```
### Private Message System

```
/pm <playername> <message>
```

Allow the user to ***private message*** another player (if they are online). If the player is ignored by who they are trying to message, they get silently ignored.

```
/r <message>
```

If the player has messaged another player or recieved a message from another player, they can use the ***reply*** command to message that player quickly without having to type in their full name.

### Ignore System

***Lets the user ignore or unignore players***
Once the player has ignored another player they won't see any of the that players global chat or private messages. They will see the players trade chat messages and clan chat messages though.
```
/ignore <playername>
```

```
/unignore <playername>
```

### Poke
```
/poke <playername>
```

Returns (privately), whether the user is online or not.

### Metrics (Admin/Moderator Command)
```
/metrics
```
Displays the metrics, privately to the moderator/admin who calls the command. The following metrics are returned.

- Private Message Count
- Clan Message Count
- Trade Message Count
- Global Message Count
- Pokes made
- Dice rolls


### Roll Dice
```
/rolldice <playername> <playername> etc..
```
When you call this command and supply online player names. A dice is rolled for each of the player and the closest to 100 is deemed the winner. The player who calls this command and each of the players online, who were included in the roll, are messaged with the results.
