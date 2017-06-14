using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PR_lab6_Server
{
	public static class ChatController
	{
		//private const int _maxMessage = 100;
		//public static List<message> Chat = new List<message>();
		public struct message
		{
			public string userName;
			public string data;
			public string destination;
			public message(string name, string msg, string dest)
			{
				userName = name;
				data = msg;
				destination = dest;
			}
		}
		public static void AddMessage(string userName, string msg, string dest)
		{
			try
			{
				if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(msg)) return;
				message newMessage = new message(userName, msg, dest);
				Console.WriteLine("New message from {0}.", userName);
				Server.SendMessages(newMessage);
			}
			catch (Exception exp) { Console.WriteLine("Error with addMessage: {0}.", exp.Message); }
		}
		
		public static string GetChat(message msg)
		{
			try
			{
				string data = msg.destination.Length > 0 ? "#privatemsg&":"#publicmsg&";
				data += String.Format("{0}~{1}~{2}", msg.userName, msg.destination, msg.data);
				return data;
			}
			catch (Exception exp) { Console.WriteLine("Error with getChat: {0}", exp.Message); return string.Empty; }
		}
	}
}
