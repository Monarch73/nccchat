using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace chat
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			int port= 9000;
			if (args.Length > 0 )
			{
				Int32.TryParse(args[0], out port);
			}
			Console.WriteLine("Port used:" + port);

			byte[] rejectMessage = System.Text.ASCIIEncoding.ASCII.GetBytes("You are not allowed to connect multiple times, sorry.\r\n");
			byte[] serverFullMessage = System.Text.ASCIIEncoding.ASCII.GetBytes("Server is full. Please check back later.\r\n");

			Dictionary<int,TelnetClient> telnets = new Dictionary<int,TelnetClient>();
			var sv = new chat.Server(telnets);
			var tcpServer = new TcpListener(IPAddress.Any, port);
			tcpServer.Start();
			
			Console.WriteLine("Chat Server started on {0}.", tcpServer.LocalEndpoint.ToString());

			while(true)
			{
				bool ipFound = false;
				var tcpClient = tcpServer.AcceptTcpClient();
				string  ip = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address.ToString();
				foreach (KeyValuePair<int,TelnetClient> telnet in telnets)
				{
					if (telnet.Value != null && telnet.Value is TelnetClient && telnet.Value.HasIp(ip))
					{
						ipFound = true;
						break;
					}
				}
				if (ipFound)
				{
					tcpClient.GetStream().Write(rejectMessage,0,rejectMessage.Length);
					tcpClient.Close();
				}
				else
				{
					try
					{
						int slotNumber = Server.FindFreeSlot();
						var telnetClient = new TelnetClient(tcpClient,sv, slotNumber);
						Thread thread = new Thread(new ThreadStart(telnetClient.Run));
						telnets.Add(slotNumber, telnetClient);
						Console.WriteLine("Connected clients: " + telnets.Count);
						thread.Start();
					}
					catch (chat.ServerFullException e)
					{
						tcpClient.GetStream().Write(serverFullMessage,0,serverFullMessage.Length);
						tcpClient.Close();
					}
				}
			}
		}
	}
}
