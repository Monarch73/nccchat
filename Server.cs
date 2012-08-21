using System.Collections.Generic;
using System.Threading;
using System.Text;
namespace chat
{
	public class MessageItem
	{
		public string message;
		public TelnetClient client;
	}
	
	public class ServerFullException : System.Exception
	{
		
	}
	
	public class Server
	{
		
		public static Dictionary<int,TelnetClient> telnets;
		private Queue<chat.MessageItem> messageQue = new Queue<chat.MessageItem>();

		public void Add (chat.MessageItem item)
		{
			lock (this)
			{
				messageQue.Enqueue(item);
			}
		}
		
		public Server (Dictionary<int,TelnetClient> telnets)
		{
			Server.telnets = telnets;
			Thread thr = new Thread(new ThreadStart(this.Run));
			thr.Start();
		}
		
		private void Run()
		{
			while(true)
			{
				if (messageQue.Count > 0)
				{
					chat.MessageItem messageItem = (chat.MessageItem) messageQue.Dequeue();
					ProcessItem(messageItem);
				}
				else
				{
					Thread.Sleep(1000);
				}
			}
		}
		
		private void ProcessItem(chat.MessageItem item)
		{
			if (item.message.Length > 1 && item.message[0] == '.')
			{
				// see class ChatCommands
				if (! item.client.chatCommands.ProcessCommand(item))
				{
					item.client.SendToUser("Unknown command - try .h for help.\r\n");
				}
			}
			else if (item.message.Length > 3 && item.message[0] == '!')
			{
				if (item.message[1] >= '0' && item.message[1] <= '9' )
				{
					int number;
					int numberLen = 1;
					while(numberLen<item.message.Length && item.message[numberLen]>='0' && item.message[numberLen] <= '9') numberLen ++;
					if (int.TryParse(item.message.Substring(1,numberLen - 1), out number))
					{
						if ( number == item.client.slotNumber)
						{
							item.client.SendToUser("Cannot whisper in your own ear.\r\n");
							return ;
						}
						TelnetClient target = Server.GetTelById(number);
						if (target != null && target.clientState == chat.TelnetClient.ClientStates.TEXT)
						{
							string messageOriginator = string.Format("Whisper to ({0}) {1}: {2}", target.slotNumber, target.username, item.message);
							string messageTarget = string.Format("({0}) {1} {2}: {3}", item.client.slotNumber, item.client.username, item.client.userPrefs.GetWhisperText(), item.message);
							
							// Send message to target
							target.BackSpace();
							if (target.userPrefs.IsWhisperHighlighting()) 
								target.SendLineToUserHighlight(messageTarget);
							else
								target.SendLineToUser(messageTarget);
							target.LineBufferOut();
							
							// send message to originator (myself)
							if (item.client.userPrefs.IsWhisperHighlighting())
								item.client.SendLineToUserHighlight(messageOriginator);
							else
								item.client.SendLineToUser(messageOriginator);
						}
						else
						{
							item.client.SendToUser("No such user.\r\n");
						}
					}
				}
			}
			else
			{
				sendToChannel(string.Format("({0}) {1} {2}: {3}\r\n", item.client.slotNumber, item.client.username, item.client.userPrefs.GetSayText(), item.message), item.client.userPrefs.GetChannelNumber());
			}
		}
		
		// exits user
		public static bool RemoveMe(TelnetClient telnetclientToRemove)
		{
			if (telnetclientToRemove != null && telnetclientToRemove.slotNumber != 0 && telnets.ContainsKey(telnetclientToRemove.slotNumber))
			{
				telnets.Remove(telnetclientToRemove.slotNumber);
				return true;
			}
			return false;
		}
		
		public static void sendToChannel(string message, int currentChannelNumber)
		{
			Dictionary<int, TelnetClient> cpyTelnets = new Dictionary<int, TelnetClient>(telnets);
			foreach(KeyValuePair<int,TelnetClient> telnet in cpyTelnets)
			{
				if (telnet.Value.clientState == chat.TelnetClient.ClientStates.TEXT &&
				    telnet.Value.userPrefs.GetChannelNumber() == currentChannelNumber
				    )
				{
					lock(telnet.Value)
					{
						telnet.Value.BackSpace();
						telnet.Value.SendToUser(telnet.Value.getTimestampString() + message);
						telnet.Value.LineBufferOut();
					}
				}
			}
		}
		
		// writes to all / broadcast
		public static void SendAll(string message)
		{
			Dictionary<int, TelnetClient> cpyTelnets = new Dictionary<int, TelnetClient>(telnets);
			foreach(KeyValuePair<int,TelnetClient> telnet in cpyTelnets)
			{
				if (telnet.Value.clientState == chat.TelnetClient.ClientStates.TEXT)
				{
					lock(telnet.Value)
					{
						telnet.Value.BackSpace();
						telnet.Value.SendToUser(telnet.Value.getTimestampString() + message);
						telnet.Value.LineBufferOut();
					}
				}
			}
		}

		public static int FindFreeSlot()
		{
			int slotNumber = 1;
			while(telnets.ContainsKey(slotNumber) && slotNumber < 1000) slotNumber++;
			if (slotNumber >= 1000) throw new ServerFullException();
			return slotNumber;
		}
		
		public static TelnetClient GetTelById(int id)
		{ 
			TelnetClient telnetClient = null;
			if (telnets.TryGetValue(id, out telnetClient))
			{
				return telnetClient;
			}
			return null;
		}
	}
}
