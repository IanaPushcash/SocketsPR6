using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PR_lab6_Server
{
	public static class Server
	{
		public static List<Client> Clients = new List<Client>();
		public static void NewClient(Socket handle)
		{
			try
			{
				Client newClient = new Client(handle);
				Clients.Add(newClient);
				Console.WriteLine("New client connected: {0}", handle.RemoteEndPoint);
			}
			catch (Exception exp) { Console.WriteLine("Error with addNewClient: {0}.", exp.Message); }
		}
		public static void EndClient(Client client)
		{
			try
			{
				client.End();
				Clients.Remove(client);
				Console.WriteLine("User {0} has been disconnected.", client.UserName);
			}
			catch (Exception exp) { Console.WriteLine("Error with endClient: {0}.", exp.Message); }
		}
		//public static void UpdateAllChats()
		//{
		//	try
		//	{
		//		int countUsers = Clients.Count;
		//		for (int i = 0; i < countUsers; i++)
		//		{
		//			Clients[i].UpdateChat();
		//		}
		//	}
		//	catch (Exception exp) { Console.WriteLine("Error with updateAlLChats: {0}.", exp.Message); }
		//}

		public static void SendMessages(ChatController.message msg)
		{
			try
			{
				if (msg.destination.Length > 0)
				{
					var dests = Clients.Where(c => c.UserName == msg.destination).ToList();
					if (dests.Count > 0)
						Clients.Where(c => c.UserName == msg.destination || c.UserName == msg.userName).ToList().ForEach(c=>c.UpdateChat(msg));
				}
				else
				{
					Clients.ForEach(c=>c.UpdateChat(msg));
				}
			}
			catch (Exception exp) { Console.WriteLine("Error with updateAlLChats: {0}.", exp.Message); }
		}
	}
}
