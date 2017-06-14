using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;

namespace PR_lab6_Server
{
	public class Client
	{
		private string _userName;
		private string _userPassword;
		private Socket _handler;
		private Thread _userThread;
		public Client(Socket socket)
		{
			_handler = socket;
			_userThread = new Thread(listner);
			_userThread.IsBackground = true;
			_userThread.Start();
		}
		public string UserName
		{
			get { return _userName; }
		}
		private void listner()
		{
			bool auth = false;
			while (!auth)
			{
				try
				{
					byte[] buffer = new byte[1024];
					int bytesRec = _handler.Receive(buffer);
					string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);
					auth =  Authorization(data);
					if (!auth)
						SendErrorAuth();
					else Send("#successauth");
				}
				catch { Server.EndClient(this); return; }
			}
			while (true)
			{
				try
				{
					byte[] buffer = new byte[1024];
					int bytesRec = _handler.Receive(buffer);
					string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);
					handleCommand(data);
				}
				catch { Server.EndClient(this); return; }
			}
		}

		private bool Authorization(string data)
		{
			if (data.Contains("#login"))
			{
				var d = data.Split('&');
				_userName = d[1];
				_userPassword = d[2];
				if (!Authorization())
					return false;
				return true;
			}
			if (data.Contains("#reglogin"))
			{
				var d = data.Split('&');
				_userName = d[1];
				_userPassword = d[2];
				AddUser();
				return true;
			}
			return false;
		}

		public void End()
		{
			try
			{
				_handler.Close();
				try
				{
					_userThread.Abort();
				}
				catch { } // г
			}
			catch (Exception exp) { Console.WriteLine("Error with end: {0}.", exp.Message); }
		}
		private void handleCommand(string data)
		{
			if (data.Contains("#newmsg"))
			{
				var d = data.Split('&');
				ChatController.AddMessage(_userName, d[2], d[1]);
				return;
			}
			if (data.StartsWith("#findusages"))
			{
				data = data.Replace("#findusages&", "");
				CountUsages(data);
			}
		}

		private void CountUsages(string data)
		{
			var d = data.Split('~');
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(d[0]);
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				var sr = new StreamReader(response.GetResponseStream());
				var parser = new HtmlParser();
				var page = sr.ReadToEnd();
				var document = parser.Parse(page);
				sr.Close();
				Send($"#countusages&{new Regex(d[1]).Matches(page).Count}");
				Console.WriteLine($"For user {_userName} found {d[1]} on {d[0]}");
			}
		}

		private void AddUser()
		{
			string connectionString =
				@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Iana\documents\visual studio 2015\Projects\PR_lab6_Client\PR_lab6_Server\Database1.mdf;Integrated Security=True";
			SqlConnection con = new SqlConnection(connectionString);
			con.Open();
			SqlCommand command = new SqlCommand($"INSERT INTO [Table] VALUES ('{_userName}', '{_userPassword}')", con);
			command.ExecuteNonQuery();
			con.Close();
			Console.WriteLine($"Registration successful user {_userName}");
		}

		private bool Authorization()
		{
			string connectionString =
				@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=C:\Users\Iana\documents\visual studio 2015\Projects\PR_lab6_Client\PR_lab6_Server\Database1.mdf;Integrated Security=True";
			SqlConnection con = new SqlConnection(connectionString);
			con.Open();
			SqlCommand command = new SqlCommand($"SELECT COUNT(ID) FROM [Table] WHERE UserName = '{_userName}' AND UserPass = '{_userPassword}'", con);
			int count = (int)command.ExecuteScalar();
			con.Close();
			if (count > 0)
			{
				Console.WriteLine($"Authorization successful user {_userName}");
				return true;
			}
			Console.WriteLine($"Authorization unsuccessful user {_userName}");
			return false;
		}

		public void UpdateChat(ChatController.message msg)
		{
			Send(ChatController.GetChat(msg));
		}
		public void Send(string command)
		{
			try
			{
				int bytesSent = _handler.Send(Encoding.UTF8.GetBytes(command));
				if (bytesSent > 0) Console.WriteLine("Success");
			}
			catch (Exception exp) { Console.WriteLine("Error with send command: {0}.", exp.Message); Server.EndClient(this); }
		}
		public void SendErrorAuth()
		{
			try
			{
				int bytesSent = _handler.Send(Encoding.UTF8.GetBytes("#errorauth"));
				if (bytesSent > 0) Console.WriteLine("Error authorization "+_handler.LocalEndPoint);
			}
			catch (Exception exp) { Console.WriteLine("Error with send command: {0}.", exp.Message); Server.EndClient(this); }
		}

	}
}
