using System;
using System.Text;

namespace chat
{
	public class UserPrefs
	{
		private string alias;
		private string sayText = "says";
		private string whisperText = "wishpers";
		private bool ShowTimestamp = false;
		private int channelNumber = 0; // lobby
		private string channelName = "Lobby";
		private bool channelOp = false;
		private string channelPassword = "";
		private bool whisperHighlighting = true;
		private string whisperHighlightingAnsi = "\x1b[0;42;34m"; // dark green on blue
		private string defaultAnsi = "\x1b[0;0;37m"; // dark white on black
		
		
		public UserPrefs ()
		{
		}
		
		
		public string GetUserFlags()
		{
			StringBuilder sb = new StringBuilder("");
			
			// channel owner flag
			sb.Append(string.Format("{0}", (this.IsChannelOP() ? "O" : " ")));
 			sb.Append(string.Format("{0}", (this.GetChannelPassword() != "" ? "P" : " ")));
			
			return sb.ToString();
		}
		
		// channel password
		public string GetChannelPassword()
		{
			return this.channelPassword;
		}
		
		public void SetChannelPassword(string channelPassword)
		{
			this.channelPassword = channelPassword;
		}
		
		// user alias
		public void SetAlias(string alias)
		{
			this.alias = alias;
		}
		
		public string GetAlias()
		{
			return this.alias;
		}

		
		// default text - encapsulated - maybe user wants to change this color as well later
		public void SetDefaultAnsi (string ansiSequence)
		{
			this.defaultAnsi = ansiSequence;
		}
		
		public string GetDefaultAnsi()
		{
			return defaultAnsi;
		}
		
		
		// whishper highlighting ansi sequence
		public void SetWhisperHighlightingAnsi(string ansiSequence)
		{
			this.whisperHighlightingAnsi = ansiSequence;
		}
		
		public string GetWhisperHighlightingAnsi()
		{
			return whisperHighlightingAnsi;
		}
		
		
		// whisher highlighting
		public bool IsWhisperHighlighting()
		{
			return this.whisperHighlighting;
		}
		
		public void setWhisperHighlighting(bool enableHighlighting)
		{
			this.whisperHighlighting = enableHighlighting;
		}
		
		
		// channel op
		public bool IsChannelOP()
		{
			return this.channelOp;
		}
		
		public void SetChannelOp(bool enableOp)
		{
			this.channelOp = enableOp;
		}
		
		
		// channel ids
		public int GetChannelNumber()
		{
			return this.channelNumber;
		}
		
		public void SetChannelnumber(int channelNumber)
		{
			this.channelNumber = channelNumber;
		}

		
		// channel name
		public string GetChannelName()
		{
			return this.channelName;
		}
		
		public void SetChannelName(string channelName)
		{
			this.channelName = channelName;
		}

	
		// whisper verb
		public string GetWhisperText()
		{
			return this.whisperText;
		}
		
		public void SetWhisperText(string whisperText)
		{
			this.whisperText = whisperText;
		}		
		
		
		// say verb
		public string GetSayText()
		{
			return this.sayText;
		}
		
		public void SetSayText(string sayText)
		{
			this.sayText = sayText;
		}
	}
}

