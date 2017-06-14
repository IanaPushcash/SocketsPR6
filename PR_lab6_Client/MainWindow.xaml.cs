using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PR_lab6_Client
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private delegate void printer(string data);
		private delegate void cleaner();
		printer Printer;
		cleaner Cleaner;
		private Socket _serverSocket;
		private Thread _clientThread;
		private const string _serverHost = "localhost";
		private const int _serverPort = 9933;
		public MainWindow()
		{
			InitializeComponent();
			Printer = new printer(print);
			Cleaner = new cleaner(clearChat);
			connect();
			_clientThread = new Thread(listner);
			_clientThread.IsBackground = true;
			_clientThread.Start();
		}
		private void listner()
		{
			while (_serverSocket.Connected)
			{
				byte[] buffer = new byte[8196];
				int bytesRec = _serverSocket.Receive(buffer);
				string data = Encoding.UTF8.GetString(buffer, 0, bytesRec);
				if (data.Contains("#publicmsg"))
				{
					ShowPublicMes(data);
					continue;
				}
				if (data.Contains("#privatemsg"))
				{
					ShowPrivateMes(data);
					continue;
				}
				if (data.StartsWith("#successauth"))
				{
					Dispatcher.Invoke((Action) (() =>
					{
						TbChat.IsEnabled = true;
						TbMessage.IsEnabled = true;
						BtnSend.IsEnabled = true;
						TbLogin.IsEnabled = false;
						PbPass.IsEnabled = false;
						BtnLogin.IsEnabled = false;
						BtnReg.IsEnabled = false;
						}));
					continue;
				}
				else if (data.StartsWith("#errorauth"))
					MessageBox.Show("Wrong login or password", "Error");
				if (data.StartsWith("#countusages"))
				{
					Dispatcher.Invoke((Action) (() =>
					{
						lbResult.Content = $"Was found {data.Split('&')[1]}";
					}));
				}
			}
		}
		private void connect()
		{
			try
			{
				IPHostEntry ipHost = Dns.GetHostEntry(_serverHost);
				IPAddress ipAddress = ipHost.AddressList[0];
				IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, _serverPort);
				_serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
				_serverSocket.Connect(ipEndPoint);
			}
			catch { print("Сервер недоступен!"); }
		}
		private void clearChat()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(Cleaner);
				return;
			}
			TbChat.Clear();
		}

		private void ShowPublicMes(string data)
		{
			//#publicmsg&userName~destUser~data
			string[] mesData = data.Split('&')[1].Split('~');
			if (mesData[2].Length == 0) return;
			print(string.Format("[{0}]:{1}.", mesData[0], mesData[2]));
		}

		private void ShowPrivateMes(string data)
		{
			//#privatemsg&username~destUser~data
			string[] mesData = data.Split('&')[1].Split('~');
			if (mesData[1].Length == 0) return;
			print(string.Format("private[{0}-{1}]:{2}.", mesData[0], mesData[1], mesData[2]));
		}

		private void send(string data)
		{
			try
			{
				byte[] buffer = Encoding.UTF8.GetBytes(data);
				int bytesSent = _serverSocket.Send(buffer);
			}
			catch { print("Связь с сервером прервалась..."); }
		}
		private void print(string msg)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.Invoke(Printer, msg);
				return;
			}
			if (TbChat.Text.Length == 0)
				TbChat.AppendText(msg);
			else
				TbChat.AppendText(Environment.NewLine + msg);
		}
		
		private void sendMessage()
		{
			try
			{
				string data = TbMessage.Text;
				string destination = tbTo.Text;
				if (string.IsNullOrEmpty(data)) return;
				send($"#newmsg&{tbTo.Text}&{data}");
				TbMessage.Text = string.Empty;
			}
			catch { MessageBox.Show("Ошибка при отправке сообщения!"); }
		}
		private void BtnLogin_Click(object sender, RoutedEventArgs e)
		{
			string name = TbLogin.Text;
			string password = PbPass.Password;
			if (string.IsNullOrEmpty(name)) return;
			send($"#login&{name}&{password}");
		}

		private void TbChat_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				sendMessage();
		}


		private void TbMessage_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
				sendMessage();
		}

		private void button_Click(object sender, RoutedEventArgs e)
		{
			sendMessage();
		}

		private void BtnReg_Click(object sender, RoutedEventArgs e)
		{
			string name = TbLogin.Text;
			string password = PbPass.Password;
			if (string.IsNullOrEmpty(name)) return;
			send($"#reglogin&{name}&{password}");
		}
		
		private void MainWindow_OnClosing(object sender, CancelEventArgs e)
		{
			_clientThread.Abort();
			_serverSocket.Disconnect(false);
		}
		
		private void cbIsPrivate_Checked(object sender, RoutedEventArgs e)
		{
			tbTo.IsEnabled = true;
		}

		private void TbTo_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			tbTo.Text = "";
		}

		private void CbIsPrivate_OnUnchecked(object sender, RoutedEventArgs e)
		{
			tbTo.IsEnabled = false;
		}

		private void BtnFind_OnClick(object sender, RoutedEventArgs e)
		{
			string page = tbPage.Text;
			string word = tbWord.Text;
			if (page.Length > 0 && word.Length > 0)
			{
				send($"#findusages&{page}~{word}");
			}
		}
	}
}
