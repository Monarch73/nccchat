using System;
using System.Net.Sockets;
using System.Timers;
using System.Text;
using System.Net;
using System.Collections.Generic;

namespace chat
{
	enum IAC
	{
		IAC = (byte)255,
		DO = (byte)253, //fd
		DONT = (byte)254, //fe
		WILL = (byte)251, //fb
		WONT = (byte)252, //fc
		SUPPRESS_GO_AHEAD = (byte)3,
		ECHO = (byte)1,
		BINARY = (byte)0,
		LOCAL_ECHO = (byte)45, //2d
		LINE_MODE = (byte)34 //22
	} ;
	
	public class TelnetClient
	{
			public enum ClientStates
			{
				SENDGREETING,
				USERNAME,
				TEXT
			};

			public enum UserStates 
			{
				GUEST,
				USER,
				ADMIN
			}


		private const int BUFFERLEN = 4198;
		private const int LINEBUFFERLEN = 255;
		private byte[] inputBuffer = new byte[BUFFERLEN];
		private TcpClient  tcpClient;
		private NetworkStream ns;
		private chat.Server sv;
		private StringBuilder lineBuffer = new StringBuilder();
		public string username;
		public bool userShowTimestamp = false;
		public UserPrefs userPrefs;
		public ClientStates clientState;
		public ChatCommands chatCommands;
		public int slotNumber;
		public UserStates userState = UserStates.GUEST;

		public string FormatedUser 
		{
			get 
			{
				return string.Format("({0}) {1}", this.slotNumber, this.username);
			}
		}

		public bool HasIp (string ip)
		{
			return (ip == ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString());
		}

		public void Disconnect ()
		{
			if (this.ns != null)	this.ns.Close();
			if (this.tcpClient != null && this.tcpClient.Connected) this.tcpClient.Close();
		}
		
		public TelnetClient (TcpClient tcpClient, chat.Server sv, int slotNumber)
		{
			this.tcpClient = tcpClient;
			this.sv = sv;
			this.clientState = ClientStates.SENDGREETING;
			this.slotNumber = slotNumber;
			this.chatCommands = new ChatCommands();
			this.userPrefs = new UserPrefs(); // after class creation
		}
		
		public void Run()
		{
			
			// force telnet to canonical mode.
			byte[] init = { 
				//(byte)IAC.IAC, (byte)IAC.DONT, (byte)IAC.ECHO, 
				(byte)IAC.IAC, (byte)IAC.DO, (byte)IAC.BINARY, 
				(byte)IAC.IAC, (byte)IAC.WILL, (byte)IAC.ECHO,
				(byte)IAC.IAC, (byte)IAC.DO,	(byte)IAC.LINE_MODE};
			
			this.ns = this.tcpClient.GetStream();
			this.ns.BeginRead(inputBuffer,0,BUFFERLEN,new AsyncCallback(this.EndRead), null);
			this.ns.Write(init,0,init.Length);
			Timer ti = new Timer();
			ti.Elapsed += delegate { OnTimerEvent(this); };
			ti.Interval = 2000;
			ti.Enabled = true;
			ti.AutoReset = false;
			ti.Start();
		}
		
		private static void OnTimerEvent(object source)
		{
			Console.WriteLine("Timer expired");
			TelnetClient me = source as TelnetClient;
			if (me == null)
			{
				Console.WriteLine("Unable to cast to source.");
				return;
			}
			if (me.clientState == ClientStates.SENDGREETING)
			{
				me.SendToUser("Warning: Your telnet did not respond to my configuration requests and may not behave properly.\r\n");
				me.clientState = ClientStates.USERNAME;
				me.WelcomeMessage();
			}
		}
		
		private void WelcomeMessage()
		{
				this.SendToUser("          Conversation - utility\r\n");
				this.SendToUser("            ******************\r\n");
				this.SendToUser("            * C * H * A * T *\r\n");
				this.SendToUser("            ******************\r\n");
				this.SendToUser(
				  string.Format("              V1.5 build:{0}\r\n",  System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
				this.SendToUser("    Programmed by Niels Huesken in C# using MonoDevelop\r\n");
				this.SendToUser("    extended by Markus Feilen\r\n");
				this.SendToUser("    (C) 2012\r\n");
				this.SendToUser("    Thanx mono-guys\r\n\r\n");
				this.SendToUser("Please enter your username: ");
		}
		
		private void EndRead(IAsyncResult iar)
		{
			try
			{
				int byteRead = this.ns.EndRead(iar);
				if (byteRead <1)
				{
					this.UnexpectedLogout();
				}
				if (byteRead > 0 && inputBuffer[0] == 255 && this.clientState == ClientStates.SENDGREETING)
			{
				this.clientState = ClientStates.USERNAME;
				WelcomeMessage();
			}
			
			if (byteRead > 0 && inputBuffer[0] != 255 && ( this.clientState == ClientStates.TEXT || this.clientState == ClientStates.USERNAME))
			{
				string input = System.Text.Encoding.ASCII.GetString(inputBuffer,0,byteRead);
				foreach (char chr in input)
				{
						ProcessChar(chr);
				}
			}
							
			this.ns.BeginRead(inputBuffer,0,BUFFERLEN, this.EndRead,null);
			}
			catch(System.ObjectDisposedException e)
			{
				Console.WriteLine("User exited");
			}
			catch(System.IO.IOException e)
			{
				Console.WriteLine("User exited2");
			}
		}
		
		// process every key stroke
		void ProcessChar(char chr)
		{
			if (this.clientState == ClientStates.TEXT && chr == 9 && lineBuffer.Length == 1 && lineBuffer[0] >= '0' && lineBuffer[0] <= '9')
			{
				int number = int.Parse(lineBuffer.ToString());
				if (number != this.slotNumber) 
				{
					TelnetClient telnetClient = Server.GetTelById(number);
					if (telnetClient != null && telnetClient.clientState == ClientStates.TEXT)
					{
						this.BackSpace();
						lineBuffer.Clear();					
						lineBuffer.Append(telnetClient.username + ": ");
						this.LineBufferOut();
					}
				}
			}
			
			if (chr == 10 || chr == 13) // line feed || carriage return
			{
				if (lineBuffer.Length != 0)
				{
					if (this.clientState == ClientStates.USERNAME)
					{
						SetUserName();								
					}
					else
					{
						BackSpace();
						chat.MessageItem messageItem = new chat.MessageItem();
						messageItem.message = lineBuffer.ToString();
						messageItem.client = this;
						sv.Add(messageItem);
					}
					lineBuffer.Clear();
				}
			}
			
			if (chr == 8 || chr == 127 ) // backspace || DEL
			{
				if (lineBuffer.Length >0 )
				{
					lineBuffer.Remove(lineBuffer.Length -1,1);
					SendToUser("\x08 \x08");
					return;
				}
			}
			
			if (chr > 31  && chr < 127) // normal chars (letters, digits etc)
			{
				if (this.clientState == ClientStates.USERNAME) // login
				{
					// username no spaces + max length 20
					if (chr == 32 || lineBuffer.Length > 20)
					{
						SendToUser('\b');
						return;
					}
					else
					{
						lineBuffer.Append(chr);
						SendToUser(chr);
						return;
					}
				}
				else if (lineBuffer.Length < LINEBUFFERLEN)
				{
					lineBuffer.Append(chr);
					string tmp = lineBuffer.ToString();
					if (tmp.Length > 4 && (tmp.Substring(0,3) == ".li" || tmp.Substring(0,3) == ".re")) 
					{
						SendToUser("*"); 
						return;
					}
					SendToUser(chr);
					return;
				}
			}
		}
		
		public void BackSpace()
		{
			lock(this)
			{
				for(int cou=0; cou<lineBuffer.Length; cou++)
				{
					this.SendToUser("\x08 \x08");
				}
			}
		}
		
		public void LineBufferOut() // writes buffer to client
		{
			lock(this)
			{
				string tmp = lineBuffer.ToString();
				if (tmp.Length>4 && (tmp.Substring(0,3) == ".li" || tmp.Substring(0,3) == ".re"))
				{
					int i=0;
					foreach(char chr in tmp)
					{
						if (i++<4)
						{
							SendToUser(chr);
						}
						else
						{
							SendToUser('*');
						}
					}
					return;
				}
				this.SendToUser(tmp);
			}
		}
		
		private void SetUserName()
		{
			if (lineBuffer.Length < 3 && lineBuffer.Length > 20)
			{
				this.SendToUser("\r\nUsername must be at least 3- and maximum 20 letters\r\n");
				this.SendToUser("Try again:");
				lineBuffer.Clear();
				return;
			}
			
			foreach(KeyValuePair<int,TelnetClient> telnet in Server.telnets)
			{
				if (telnet.Value.clientState == ClientStates.TEXT && telnet.Value.username == lineBuffer.ToString())
				{
					this.SendToUser("\r\nUsername already exists.\r\n");
					this.SendToUser("Try again:");
					lineBuffer.Clear();
					return;
				}
			}
			
			this.username = lineBuffer.ToString();
			lineBuffer.Clear();
			this.clientState = ClientStates.TEXT;
			this.userPrefs.SetAlias(this.username);
			this.SendToUser("\r\nYou're all set. Enjoy your conversation.\r\n");
			chat.Server.SendAll(string.Format("+++ ({0}) {1} +++\r\n", this.slotNumber, this.username));
		}
		
		private void UnexpectedLogout()
		{
			lock(this)
			{
				if (Server.RemoveMe(this))
				{
					Console.WriteLine("Socket unexpectedly closed");
					if (this.clientState == ClientStates.TEXT)
					{
						Server.SendAll(string.Format("--- ({0}) {1} --- (hangup)\r\n", this.slotNumber, this.username));
					}
				}
				this.Disconnect();
			}
		}
		
		
		private void SendToUser(char chr)
		{
			lock(this)
			{
				try
				{
					byte[] bytesToSend = System.Text.ASCIIEncoding.ASCII.GetBytes(chr.ToString());
					this.ns.WriteByte(bytesToSend[0]);
				}
				catch(System.IO.IOException e)
				{
					this.UnexpectedLogout();
				}
				catch (System.ObjectDisposedException e)
				{
					this.UnexpectedLogout();
				}
			}
		}
		

		
		public void SendToUser(string message)
		{
			lock(this)
			{
				try
				{
					byte[] bytesToSend = System.Text.ASCIIEncoding.ASCII.GetBytes(message);
					this.ns.Write(bytesToSend,0,bytesToSend.Length);
				}
				catch (System.IO.IOException e)
				{
					this.UnexpectedLogout();
				}
				catch (System.ObjectDisposedException e)
				{
					this.UnexpectedLogout();
				}
			}
		}

		
		public void SendLineToUser(string message)
		{
			this.SendToUser(message + "\r\n");
		}
		
		
		// sends whispertext to client / remote user with ansi sequences
		public void SendLineToUserHighlight(string message) 
		{
			this.SendToUser(this.userPrefs.GetWhisperHighlightingAnsi() + this.getTimestampString() + message + "\x1b[0;40;37m\r\n");
		}
		
		
		// returns time stamp prefix if necessary
		public string getTimestampString()
		{
			string msgTimestamp = "";
			if (this.userShowTimestamp)
			{
				string formattedDate = String.Format("{0:dd.MM.yyyy HH:mm:ss}", DateTime.Now);
				msgTimestamp = "[" + formattedDate + "] ";
			}
			
			return msgTimestamp;
		}
	}
}

