using System;
using System.Text;
using System.Collections.Generic;

namespace chat
{
	public class ChatCommands
	{
		private chat.MessageItem item;

		public ChatCommands()
		{
			
		}
		
		public bool ProcessCommand(chat.MessageItem item)
		{
			this.item = item;
			
			if (this.ProcessSingleCharCommand())
				return true;
			
			
			if (this.ProcessDoubleCharCommand())
				return true;
			
			return false;
		}

		
		private bool ProcessSingleCharCommand()
		{
			bool commandFound = true;
			
			if (item.message.Length > 2) // command not found
				return false;
			
			char singleCharCommand = item.message[1];
			
			switch (singleCharCommand)
			{
				case 'h': // help
					this.SendChatHelp();
					break;
				
				case 't': // show/hide timestamp
					this.SwitchOnOffTimestampPrefix();
					break;
					
				case 's': // user list
					this.SendUserListInChat();
					break;
				
				case 'x':
					this.ExitChat();
				break;
				
				default:
					commandFound = false;
					break;
			}

			return commandFound;
		}

		private bool ProcessDoubleCharCommand()
		{
			bool commandFound = true;
			if (item.message.Length < 3)
					return commandFound;
			
			string dualCharCommand = item.message.Substring(1,2);
			
			switch(dualCharCommand)
			{
				case "ns": // change say text
					this.SetSayText();
					break;
					
				case "nw": // change whisper 
					this.SetWhisperText();
					break;
					
				case "cw": // change whishper color
					this.SetWhisperHighlightColor();
					break;
					
				case "ac": // show ansi colos
					this.SendAnsiColors();
					break;

				case "li": // login
					this.Login();
					break;

				case "re": // register
					this.Register();
					break;

				case "hc": // create channel
					this.SendChannelHelp();
					break;
				
				case "cr": // create channel
					this.CreateChannel();
					break;

				case "co": // whisper text highlighted
					this.SwitchOnOfColorWhispertext();
					break;

				case "ad": //function make user admin
					this.GrantAdmin();
					break;

				case "du":
					this.DropUser();
					break;
								
				case "c ":
					this.SwitchToChannel();
					break;				
				
				case "in":
					this.InviteUserToOwnChannel();
					break;
				
				case "p ":
					this.SetChannelPassword();
					break;

				case "sx":
					this.SendUserListInChat();
					break;
				
				case "dc":
					this.DropUserFromChannel();
					break;
				
				case "cm":
					this.ChangeChannelOp();
					break;
				
				case "un":
					this.RemoveChannelPassword();
					break;

				default:
					commandFound = false;
					break;
			}
		
			return commandFound;			
		}

		private void DropUser ()
		{
			if (item.client.userState != TelnetClient.UserStates.ADMIN) 
			{
				return;
			}
			int number = 0;
			if (item.message.Length < 5 || int.TryParse (item.message.Substring (4, item.message.Length - 4), out number) == false || number < 1) 
			{
				item.client.SendLineToUser ("ERROR: invalid usernumber");
				return;
			}

			lock (Server.telnets) 
			{
				if (!Server.telnets.ContainsKey (number)) 
				{
					item.client.SendLineToUser("ERROR: no such user.");
					return;
				}
				TelnetClient tcl = Server.telnets[number];
				string message = string.Format("--- {0} --- (dropped by {1})\r\n", tcl.FormatedUser, item.client.FormatedUser);
				tcl.SendLineToUser(message);
				Server.RemoveMe(tcl);
				tcl.Disconnect();
				Server.SendAll(message);
			}
		}

		private void GrantAdmin ()
		{
			if (item.client.userState != TelnetClient.UserStates.ADMIN) 
			{
				return;
			}

			if (item.message.Length < 5) 
			{
				item.client.SendLineToUser ("ERROR: username (too short)");
				return;
			}

			string username = item.message.Substring (4, item.message.Length - 4).Trim ();
			chat.UserDB.DataStructure dbuser = UserDB.GetInstance ().FindByName (username);
			if (dbuser == null)
			{
				item.client.SendLineToUser("ERROR: No such user");
				return;
			}

			chat.UserDB.GetInstance().SetAdmin(dbuser.id);
			item.client.SendLineToUser(string.Format("Admin permission granted to user {0}", dbuser.id));
		}
		
		
		// login user
		private void Login ()
		{
			if (item.message.Length < 7) 
			{
				item.client.SendLineToUser ("ERROR: Invalid password (too short)");
				return;
			}

			if (item.client.userState != TelnetClient.UserStates.GUEST) 
			{
				item.client.SendLineToUser ("ERROR: Already logged in");
				return;
			}

			string password = item.message.Substring (4, item.message.Length - 4).Trim();
			chat.UserDB.DataStructure dbuser = UserDB.GetInstance ().FindByName (item.client.username.ToLower()); // case insensitive
			if (dbuser == null) {
				item.client.SendLineToUser ("ERROR: Unknown user. Please register first.");
				return;
			}

			if (password != dbuser.password) {
				item.client.SendLineToUser ("ERROR: Wrong password.");
				return;
			}

			item.client.userState = dbuser.admin || dbuser.id == 1 ? TelnetClient.UserStates.ADMIN : TelnetClient.UserStates.USER;
			if (item.client.userState == TelnetClient.UserStates.ADMIN)
			{
				Server.SendAll(string.Format("({0}) {1} >>> {2}\r\n",item.client.slotNumber,item.client.username, item.client.userState.ToString()));
			}
		    else
		    {
				item.client.SendLineToUser("Logged in as " + item.client.userState.ToString());
			}
		}

		private void Register ()
		{
			if (item.client.userState != TelnetClient.UserStates.GUEST) {
				item.client.SendLineToUser ("ERROR: You are already logged in.");
				return;		
			}

			if (item.message.Length < 7) {
				item.client.SendLineToUser ("ERROR: Invalid password (too short)");
				return;
			}

			chat.UserDB.DataStructure dbuser = UserDB.GetInstance ().FindByName (item.client.username.ToLower());
			if (dbuser != null) {
				item.client.SendLineToUser ("ERROR: User already registered. Please Login.");
				return;
			}
			string password = item.message.Substring (4, item.message.Length - 4).Trim();
			dbuser = new UserDB.DataStructure () 
			{
				username = item.client.username.ToLower(), // case insensitive user names
				password = password
			};

			if (!UserDB.GetInstance ().StoreUser(dbuser)) 
			{
				item.client.SendLineToUser ("User created. Grating user permissions.");
				item.client.userState = TelnetClient.UserStates.USER;
			} 
			else 
			{
				item.client.SendLineToUser("ERROR creating user sorry.");
			}
			return ; 
		}

		// sends help to user
		private void SendChatHelp ()
		{                         
			                          //"12345678901234567890123456789012345678901234567890123456789012345678901234567890" // reference :)
			item.client.SendLineToUser ("Help:");
			item.client.SendLineToUser (".s              show list of users online.");
			item.client.SendLineToUser (".h              show this help");
			item.client.SendLineToUser (".co             turn whisper highlighting on/off (default on)");
			item.client.SendLineToUser (".li <passwd>    login as user");
			item.client.SendLineToUser (".re <passwd>    register as user with <password>");
			item.client.SendLineToUser (".ns [text]      set verb to [text] (default 'says')");
			item.client.SendLineToUser (".nw [text]      set whisper verb to [text] (default 'whispers')");
			item.client.SendLineToUser (".t              shows current time before each line");
			item.client.SendLineToUser (".ac             shows color table (ansi)");
			item.client.SendLineToUser (".cw [color]     changes whisper color according to '.ac' color table");
			item.client.SendLineToUser ("!<no>           whisper to user");
			item.client.SendLineToUser ("<no>[TAB]       replace <no> with username");
			item.client.SendLineToUser (".c <no>,[pw]    change to channel <no> with password, if protected");
			item.client.SendLineToUser (".cr [text]      create channel with name [text] (max. length 30)");
			item.client.SendLineToUser (".in [userid]    sends invitation to user incl. password (channel OP only)");
			item.client.SendLineToUser (".dc [userid]    drops user back to lobby (channel OP only)");
			item.client.SendLineToUser (".p  [text]      sets password to channel");
			item.client.SendLineToUser (".un             unprotects channel. All users can enter now without password");
			item.client.SendLineToUser (".cm [userid]    give channel OP rights to another user");
		
			if (item.client.userState == TelnetClient.UserStates.ADMIN)
			{
				item.client.SendLineToUser (" ");
				item.client.SendLineToUser ("Admin functions:");
				item.client.SendLineToUser (".ad <username>  grant admin permissions to username");
				item.client.SendLineToUser (".du <number>    drop user from chat");
			}
		}
		
		
		private void SendChannelHelp()
		{
		}
		
		
		private void CreateChannel()
		{
			// get next free channelid
			int channelNumber = this.GetNextFreeChannelId();
			if (channelNumber == 0)
			{
				item.client.SendToUser("We're sorry. No free channel available. Please join another channel or try again later.\r\n");
				return;
			}

			if (item.client.userState == TelnetClient.UserStates.GUEST) 
			{
				item.client.SendLineToUser("Please login or register first.");
				return;
			}

			
			string channelName = item.message.Substring(3, (item.message.Length-3)); // exctract channel name
			channelName = channelName.Trim();
			
			// truncate if necessary
			int maxLength = 30; // maxmimum channel name length
			string maxLengthReached = "";
			if (channelName.Length > maxLength)
			{
				channelName = channelName.Substring(0, 30);
				maxLengthReached = string.Format(" (truncated to {0} chars)", maxLength);
			}
			
			// channel name in use?
			if (IsChannelNameUsed(channelName))
			{
				item.client.SendToUser("---- Channel name already existant. Please try another name. ----\r\n");
				return;
			}
			
			// set channel details
			item.client.userPrefs.SetChannelnumber(channelNumber);
			item.client.userPrefs.SetChannelName(channelName);
			item.client.userPrefs.SetChannelOp(true);
	
			Server.SendAll(string.Format("User {0} creates channel `({1}) {2}`\r\n", item.client.FormatedUser, channelNumber.ToString(), channelName));
		}
		
		
		private int GetNextFreeChannelId()
		{
			int availableChannelNumber = 0;
			lock (Server.telnets) 
			{
				for (int i = 1; i <= 255; i++) // 0 = lobby
				{
					if (!this.isChannelNumberUsed(i))
					{
						availableChannelNumber = i;
						break;
						
					}
				}
			}
			
			return availableChannelNumber;
		}
		
		
		// check, if channel nubmer is already used
		private bool isChannelNumberUsed (int channelNumber)
		{
			bool channelNumberUsed = false;
			lock (Server.telnets) 
			{
				foreach (KeyValuePair<int,TelnetClient> telnet in Server.telnets) 
				{
					if (telnet.Value.userPrefs.GetChannelNumber () == channelNumber) 
					{
						channelNumberUsed = true;
						break;
					}
				}

				return channelNumberUsed;
			}
		}
		
		
		// check, if channel name is used - case insensitive
		private bool IsChannelNameUsed(string channelName)
		{
			bool channelNameUsed = false;
			lock (Server.telnets)
			{
				foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
				{
					if(telnet.Value.userPrefs.GetChannelName().ToLower() == channelName.ToLower())
					{
						channelNameUsed = true;
						break;
					}
				}
			}
			return channelNameUsed;
		}
		
		
		// jumps into channel
		private void SwitchToChannel ()
	    {
			string channelNumberAsString = item.message.Substring (2, (item.message.Length - 2));
			string[] pieces = channelNumberAsString.Split (',');
			string channelPassword = "";
			int channelNumber;
			
			// logged in?
			if (item.client.userState == TelnetClient.UserStates.GUEST) 
			{
				item.client.SendLineToUser("Please login or register first.");
				return;
			}
			
			if (pieces.Length < 1 || pieces[0].Length == 0) // if parameters given or nothing before the ',' separator
			{
				item.client.SendToUser("No channel number given. Please use '.c <channelNo>,[password if required]'\r\n");
				return;
			}
			
			int.TryParse(pieces[0], out channelNumber);
			
			if (channelNumber == item.client.userPrefs.GetChannelNumber())
			{
				item.client.SendToUser(string.Format("--- You are already on channel '({0}) {1}'! ---\r\n", channelNumber.ToString(), GetChannelName(channelNumber)));
				return;
			}
			
			if (channelNumber == 0) // if lobby
			{
				item.client.userPrefs.SetChannelName(this.GetChannelName(channelNumber));
				item.client.userPrefs.SetChannelOp(false);
				item.client.userPrefs.SetChannelnumber(0);
				item.client.userPrefs.SetChannelPassword("");
				Server.SendAll(string.Format(">>>> User '{0}' moved to channel '({1}) {2}'>>>>\r\n", item.client.username.ToUpper(), channelNumber, this.GetChannelName(channelNumber).ToString()));
				return;
			}
			
			// already on channel
			if (channelNumber == item.client.userPrefs.GetChannelNumber())
			{
				item.client.SendToUser("--- Believe it or not: You are already on this channel! ---\r\n");
				return;
			}
			
			// channel exists?
			if (! this.IsChannelAlive(channelNumber))
			{
				item.client.SendToUser(string.Format("--- Sorry, I cannot find a channel number '{0}' ---\r\n", channelNumber.ToString()));
				return;
			}
			
			if (pieces.Length == 2 && pieces[1].ToString().Length != 0) // password given?
			{
				channelPassword = pieces[1];
				channelPassword = channelPassword.Trim();
			}
			
			// channel password ok?
			if (channelPassword != this.GetChannelPassword(channelNumber)) // password ok?
			{
				item.client.SendToUser("--- Sorry, wrong Password / Password expected ---\r\n");
				return;
			}
			
			// no lock here. that might create a channel w/o OP rights - but anyway.
			item.client.userPrefs.SetChannelName(this.GetChannelName(channelNumber));
			item.client.userPrefs.SetChannelnumber(channelNumber);
			Server.SendAll(string.Format(">>>> User '{0}' moved to channel '({1}) {2}'>>>>\r\n", item.client.username.ToUpper(), channelNumber, this.GetChannelName(channelNumber).ToString()));
		}
		

		// channel exists
		private bool IsChannelAlive(int channelNumber)
		{
			foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
			{
				if (telnet.Value.userPrefs.GetChannelNumber() == channelNumber)
				{
					return true;
				}
			}
			
			return false;
		}
		
		
		// returns channel name of a channelNumber
		private string GetChannelName(int channelNumber)
		{
			string channelName = "";
			
			if (channelNumber == 0)
			{
				channelName = "Lobby";
				return channelName;
			}
			
			foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
			{
				if (telnet.Value.userPrefs.GetChannelNumber() == channelNumber)
				{
					channelName = telnet.Value.userPrefs.GetChannelName();
					break;
				}
			}
			return channelName;			
		}
		
		
		// returns channel password of a channelOwner
		private string GetChannelPassword(int channelNumber)
		{
			string channelPassword = "";
			foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
			{
				if (telnet.Value.userPrefs.GetChannelNumber() == channelNumber) // if channel found
				{
					if (telnet.Value.userPrefs.IsChannelOP()) // if thread contains channel op
					{
						channelPassword =  telnet.Value.userPrefs.GetChannelPassword();
					}
					break;
				}
			}
			return channelPassword;
		}
		
		
		// set channel password
		private void SetChannelPassword()
		{
			string channelPassword = item.message.Substring(2,(item.message.Length -2));
			channelPassword = channelPassword.Trim();
			
			// channel owner?
			if (! item.client.userPrefs.IsChannelOP())
			{
				item.client.SendToUser("--- You do not own this channel ---\r\n");
				return;
			}
			
			
			// password empty?
			if (channelPassword.Length == 0)
			{
				item.client.SendToUser("Cannot set empty passwort to channel. Please try '.p [yourpassword]'.\r\n");
				return;
			}
			
			// set password!
			item.client.userPrefs.SetChannelPassword(channelPassword);
			item.client.SendToUser(string.Format("--- Password set to '{0}' ---\r\n", channelPassword.ToString()));
			return;
		}
		
		
		// remove channel password
		private void RemoveChannelPassword()
		{
			if (!item.client.userPrefs.IsChannelOP())
			{
				item.client.SendToUser("--- You do not own this channel ---\r\n");
				return;
			}
			
			item.client.userPrefs.SetChannelPassword("");
			
			item.client.SendToUser("--- Channel protection removed. Password is no longer required ---\r\n");
		}
		
		
		// invite user
		private void InviteUserToOwnChannel()
		{
			if (!item.client.userPrefs.IsChannelOP())
			{
				item.client.SendToUser("--- You do not own this channel ---\r\n");
				return;
			}
			
			string slotNumberAsString = item.message.Substring(3, (item.message.Length-3));
			int slotNumber = 0;
			int.TryParse(slotNumberAsString, out slotNumber);
			
			lock(Server.telnets)
			{
				if (slotNumber != 0 && userIdActive(slotNumber))
				{
					if (slotNumber == item.client.slotNumber)
					{
						item.client.SendToUser("--- So loneley that you try to invite yourself? ---\r\n");
						return;
					}
					
					if (Server.telnets[slotNumber].userPrefs.GetChannelNumber() == item.client.userPrefs.GetChannelNumber())
					{
						item.client.SendToUser(string.Format("--- User ({0}) {1} is already on your channel! ---\r\n", slotNumber.ToString(), Server.telnets[slotNumber].username.ToUpper()));
						return;
					}
					
					TelnetClient target = Server.GetTelById(slotNumber);
					if (target != null && target.clientState == chat.TelnetClient.ClientStates.TEXT)
					{
						target.SendLineToUser(
								string.Format(
								"User ({0}) {1} invites you to channel `({2}) {3}`", 
								item.client.slotNumber.ToString(),
								item.client.username.ToUpper(), 
								item.client.userPrefs.GetChannelNumber(), 
								item.client.userPrefs.GetChannelName()));
						
						if (item.client.userPrefs.GetChannelPassword() != "")
						{
							target.SendLineToUser(string.Format("Channel is protected with password '{0}'\r\n", item.client.userPrefs.GetChannelPassword()));
						}
						item.client.SendToUser("--- User invited ---\r\n");
					}
					
				} else {
					
					item.client.SendToUser("--- Sorry, user cannot be found ---\r\n");
				}
			}
		}
		
		
		// Drops a user back to the lobby. ChannelOP only
		private void DropUserFromChannel()
		{
			if (!item.client.userPrefs.IsChannelOP())
			{
				item.client.SendToUser("--- You do not own this channel ---\r\n");
				return;
			}
			
			string slotNumberAsString = item.message.Substring(3, (item.message.Length-3));
			int slotNumber = 0;
			int.TryParse(slotNumberAsString, out slotNumber);
			
			// prevent channel op drop user from annother channel he does not own ;-)
			if (Server.telnets[slotNumber].userPrefs.GetChannelNumber() != item.client.userPrefs.GetChannelNumber())
			{
				item.client.SendToUser(string.Format("--- User ({0}) {1} is not on your channel ---\r\n", slotNumber.ToString(), Server.telnets[slotNumber].username));
				return;
			}
			
			lock(Server.telnets)
			{
				if (slotNumber != 0 && userIdActive(slotNumber))
				{
					Server.SendAll(string.Format("--- User ({0}) {1} dropped down to channel LOBBY ---\r\n", slotNumber.ToString(), Server.telnets[slotNumber].username.ToUpper()));
		
					Server.telnets[slotNumber].userPrefs.SetChannelName("Lobby");
					Server.telnets[slotNumber].userPrefs.SetChannelnumber(0);

					// in case channel OP drops himself
					Server.telnets[slotNumber].userPrefs.SetChannelOp(false);
					Server.telnets[slotNumber].userPrefs.SetChannelPassword("");
					
					Server.telnets[slotNumber].SendToUser("--- You were kindfully asked to return to LOBBY ---\r\n");
				}
			}
		}
		
		
		// change channel operator to slotNumber
		private void ChangeChannelOp()
		{
			if (!item.client.userPrefs.IsChannelOP())
			{
				item.client.SendToUser("--- You do not own this channel ---\r\n");
				return;
			}
			
			string slotNumberAsString = item.message.Substring(3, (item.message.Length-3));
			int slotNumber = 0;
			int.TryParse(slotNumberAsString, out slotNumber);
			
			if (slotNumber == item.client.slotNumber)
			{
				item.client.SendToUser("--- You already own this channel ---\r\n");
				return;
			}
			
			lock(Server.telnets)
			{
				if (slotNumber != 0 && userIdActive(slotNumber))
				{
					item.client.userPrefs.SetChannelOp(false);
					Server.telnets[slotNumber].userPrefs.SetChannelOp(true);
					Server.telnets[slotNumber].userPrefs.SetChannelPassword(item.client.userPrefs.GetChannelPassword());
					item.client.userPrefs.SetChannelPassword("");
					
					Server.telnets[slotNumber].SendToUser("--- You are the channel operator now! ---\r\n");
					Server.telnets[slotNumber].SendLineToUser(string.Format("Channel is protected with password '{0}'\r\n", Server.telnets[slotNumber].userPrefs.GetChannelPassword()));
					item.client.SendToUser(string.Format("--- Channel operator status moved to User {0} {1} ---\r\n", slotNumber.ToString(), Server.telnets[slotNumber].username));
				}
			}
		}
		
		// user online?
		private bool userIdActive(int slotNumber)
		{
			foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
			{
				if (telnet.Value.slotNumber == slotNumber)
				{
					return true;
				}
			}
			return false;
		}
		
		
		// show users in chat
		private void SendUserListInChat ()
		{
			lock (Server.telnets) 
			{
				SortedList<int,TelnetClient> sortedTelnetCopy = new SortedList<int, TelnetClient> (Server.telnets);
				StringBuilder sb = new StringBuilder ("");
				sb.Append ("\r\n");
				sb.Append (" No Name                 | CNo Channelname      | Nickname             | Flags\r\n");
// 			sb.Append(" No Name                 | CNo  Channelname       | Nickname             | Flgs\r\n"); // future version
				sb.Append ("-------------------------+----------------------+----------------------+-------\r\n");

				foreach (KeyValuePair<int,TelnetClient> telnet in sortedTelnetCopy) {
					if (telnet.Value.clientState == chat.TelnetClient.ClientStates.TEXT) {
						sb.Append (this.GetFormattedSlotNumber (telnet.Value.slotNumber));
						sb.Append (this.GetFormattedUserName (telnet.Value.username));
						sb.Append ("| ");
						sb.Append (this.GetFormattedChannelNumber (telnet.Value.slotNumber));
						sb.Append (this.GetFormattedChannelName(telnet.Value.slotNumber));
						sb.Append ("| ");
						sb.Append (this.GetFormattedUserAlias (telnet.Value.userPrefs.GetAlias ()));
						sb.Append ("| ");
						sb.Append (string.Format("{0}",telnet.Value.userState.ToString().Substring(0,1))); // dirty, i know :)
						sb.Append (string.Format("{0}\r\n",telnet.Value.userPrefs.GetUserFlags()));
					}
				}
			
				sb.Append ("-------------------------+----------------------+----------------------+-------\r\n");
				sb.Append ("A = Admin | U = User | G = Guest | O = ChannelOP | P Channel protected\r\n");

				item.client.SendToUser (sb.ToString ());
			}
		}
		
		
		// exit from chat
		private void ExitChat()
		{
			item.client.SendToUser("Good Bye\r\n");
			Server.RemoveMe(item.client);
			item.client.Disconnect();
			Server.SendAll(string.Format("--- ({0}) {1} --- (normal exit)\r\n",item.client.slotNumber,item.client.username));
		}
		
		
		// switch on/off colored whispertext
		private void SwitchOnOfColorWhispertext()
		{
			item.client.SendToUser("Whisper highlighting ");

			if (item.client.userPrefs.IsWhisperHighlighting())
			{
				item.client.userPrefs.setWhisperHighlighting(false);
				item.client.SendToUser("off\r\n");
			}
			else
			{
				item.client.userPrefs.setWhisperHighlighting(true);
				item.client.SendToUser("on\r\n");
				item.client.SendLineToUserHighlight("Whisper messages are now highlighted.");
			}			
		}
		
		
		// switch on/off timestamp prefix
		private void SwitchOnOffTimestampPrefix()
		{
			item.client.userShowTimestamp = !item.client.userShowTimestamp;
			item.client.SendToUser(string.Format("time stamp {0}\r\n", (item.client.userShowTimestamp ? "activated" : "disabled")));
		}
		
		
		// change 'says' text
		private void SetSayText()
		{
			int maxLength = 20;
			int cutOfLength = 20;
			string maxLengthReached = "";
			string newSayText = item.message.Substring(3, (item.message.Length-3));

			newSayText = newSayText.Trim();
			
			if (maxLength < newSayText.Length)
			{
				cutOfLength = maxLength;
				maxLengthReached = string.Format(" (truncated to {0} chars)", maxLength);
				
			} else {
			
				cutOfLength = newSayText.Length;
			}
			
			newSayText = newSayText.Substring(0,cutOfLength);
			
			item.client.userPrefs.SetSayText(newSayText);
			item.client.SendToUser(string.Format("Normal verb set to '{0}'{1}.\r\n", newSayText, maxLengthReached));
		}		
		
		
		// change 'whisper' text
		private void SetWhisperText()
		{
			int maxLength = 20;
			int cutOfLength = 20;
			string maxLengthReached = "";
			string newWisperText = item.message.Substring(3, (item.message.Length-3));

			newWisperText = newWisperText.Trim();
			
			if (maxLength < newWisperText.Length)
			{
				cutOfLength = maxLength;
				maxLengthReached = string.Format(" (truncated to {0} chars)", maxLength);
				
			} else {
			
				cutOfLength = newWisperText.Length;
			}
			
			newWisperText = newWisperText.Substring(0,cutOfLength);

			item.client.userPrefs.SetWhisperText(newWisperText);
			item.client.SendToUser(string.Format("Whisper verb set to '{0}'{1}.\r\n", newWisperText, maxLengthReached));
		}	
		
		
		// set whisper ansi colors
		private void SendAnsiColors()
		{
			string ansiEscapeChar = "\x1b[";
			StringBuilder sb = new StringBuilder();
			
			for (int darkBright = 0; darkBright <= 1; darkBright++) // dark bright
			{
				for (int fgColor = 30; fgColor <= 37; fgColor++)
				{
					for (int bgColor = 40; bgColor <= 47; bgColor++) // background
					{
						sb.Append(ansiEscapeChar);
						sb.Append(string.Format("{0};", darkBright.ToString())); // dark bright (foreground only)
						sb.Append(string.Format("{0};", bgColor.ToString())); // reset; clears all colors and styles (to white on black)
						sb.Append(string.Format("{0}m", fgColor.ToString())); // reset; clears all colors and styles (to white on black)
						sb.Append(string.Format(" {0},{1},{2} ", darkBright.ToString(), bgColor.ToString(), fgColor.ToString()));
						sb.Append(string.Format("{0}{1} ", ansiEscapeChar,"0;40;37m"));
					}
					
					sb.Append("\r\n");
				}
			}

			item.client.SendToUser(sb.ToString());
		}
		
		
		// set whisper text/background color
		private void SetWhisperHighlightColor()
		{
			string newColors = item.message.Substring(3, (item.message.Length-3));
			string[] pieces = newColors.Split(',');
			
			if (pieces.Length < 3)
			{
				item.client.SendToUser("Invalid parameters. Example: '.cw 1,44,37'\r\n");
				return;
			}
			
			
			// vanity check valid colors
			int darkBright;
			if (int.TryParse(pieces[0], out darkBright) && darkBright != 0 && darkBright != 1)
			{
				item.client.SendToUser(string.Format("Could not set whisper color - invalid brightness. Your tried: {0}\r\n", newColors.ToString()));
				return;
			}
			
			int bgColor;
			int.TryParse(pieces[1], out bgColor);
			if (bgColor < 40 || bgColor > 47)
			{
				item.client.SendToUser(string.Format("Could not set whisper color - invalid background color. You tried: {0}\r\n", newColors.ToString()));
				return;
			}
			
			int fgColor;
			int.TryParse(pieces[2], out fgColor);
			if (fgColor < 30 || fgColor > 37)
			{
				item.client.SendToUser(string.Format("Could not set whisper color - invalid foreground color. Your tried: {0}\r\n", newColors.ToString()));
				return;
			}
			
			// set prefs
 			string ansiSequence = string.Format("\x1b[{0};{1};{2}m", darkBright.ToString(), bgColor.ToString(), fgColor.ToString());
			item.client.userPrefs.SetWhisperHighlightingAnsi(ansiSequence);
			item.client.SendToUser(string.Format("{0}{1}{2}\r\n", item.client.userPrefs.GetWhisperHighlightingAnsi(), "Your whishper color has been set.", item.client.userPrefs.GetDefaultAnsi()));
		}
		
		
		// slot number als string formatiert
		private string GetFormattedSlotNumber(int slotNumber)
		{
			int maxLength = 3;
			StringBuilder sb = new StringBuilder("");

			sb.Append(this.RepeatChar(" ", (maxLength - slotNumber.ToString().Length)));
			sb.Append(slotNumber.ToString());
			sb.Append(" ");
			
			return sb.ToString();
		}
		
		
		// returns username truncated to max 20 chars
		private string GetFormattedUserName(string username)
		{
			return this.GetFormattedUserlistElement(username, 20);
		}
		
		
		// currently same amount chars as username
		private string GetFormattedUserAlias(string userAlias)
		{
			return this.GetFormattedUserlistElement(userAlias, 20);
		}
		
		
		// returns channel name truncated to max 20 chars
		private string GetFormattedChannelName(int slotNumber)
		{
			return this.GetFormattedUserlistElement(Server.telnets[slotNumber].userPrefs.GetChannelName(), 16);
		}
		
		
		// returns channell number 4 chars
		private string GetFormattedChannelNumber(int slotNumber)
		{
			return this.GetFormattedUserlistElement(Server.telnets[slotNumber].userPrefs.GetChannelNumber().ToString(), 3);
		}
		
		
		// sends a formatted user list element (name, alias, channel name etc)
		private string GetFormattedUserlistElement(string element, int maxLength)
		{
			StringBuilder sb = new StringBuilder(""); //userName.ToString());
			
			if (element.Length <= maxLength)
			{
				sb.Append(element.ToString());
				sb.Append(this.RepeatChar(" ", (maxLength - element.Length)));
				
			} else {
				
				sb.Append(element.Substring(0,(maxLength - 2)));
				sb.Append("..");
			}
			
			sb.Append(" ");
			
			return sb.ToString();			
		}
		
		
		// repeates a char by int multiplier
		private string RepeatChar(string source, int multiplier)
		{
			if (multiplier <= 0)
				return "";
			
		    StringBuilder sb = new StringBuilder(multiplier * source.Length);
		    for (int i = 0; i < multiplier; i++)
		    {
		       sb.Append(source);
		    }
		    return sb.ToString();
		}
		


		public void SendUserPrefs(chat.MessageItem item)
		{

			
		}
	}
}

