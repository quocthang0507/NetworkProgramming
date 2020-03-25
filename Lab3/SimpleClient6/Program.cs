﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleClient
{
	class Program
	{
		static void Main(string[] args)
		{
			Cau6_2();
		}

		static void Cau6_1()
		{
			Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			EndPoint remote = new IPEndPoint(IPAddress.Loopback, 5000); //Different from previous project: EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
			serverSocket.Connect(remote);
			serverSocket.SendTo(Encoding.UTF8.GetBytes("Hello server"), remote);
			string message;
			byte[] buff;
			int bytes;
			while (true)
			{
				Console.Write("Input: ");
				message = Console.ReadLine();
				buff = Encoding.ASCII.GetBytes(message);
				serverSocket.SendTo(buff, 0, buff.Length, SocketFlags.None, remote);

				buff = new byte[10]; // Error when the message is larger than 10 characters
				bytes = serverSocket.ReceiveFrom(buff, 0, buff.Length, SocketFlags.None, ref remote);
				message = Encoding.ASCII.GetString(buff, 0, bytes);
				Console.WriteLine("Server: " + message);
			}
		}

		static void Cau6_2()
		{
			// IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
			Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			EndPoint remote = new IPEndPoint(IPAddress.Loopback, 5000); //Different from previous project: EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
			serverSocket.Connect(remote);
			serverSocket.SendTo(Encoding.UTF8.GetBytes("Hello server"), remote);

			string message;
			byte[] buff;
			int i = 10;
			int bytes;
			while (true)
			{
				Console.Write("Input: ");
				message = Console.ReadLine();
				if (message.Equals("exit", StringComparison.InvariantCultureIgnoreCase))
					break;
				buff = Encoding.UTF8.GetBytes(message);
				serverSocket.SendTo(buff, remote);
				buff = new byte[i];
				try
				{
					bytes = serverSocket.ReceiveFrom(buff, ref remote);
					message = Encoding.UTF8.GetString(buff, 0, bytes);
					Console.WriteLine("Server: " + message);
				}
				catch (SocketException)
				{
					Console.WriteLine("Canh bao: du lieu bi mat, hay thu lai");
					i += 10;
				}
			}
		}
	}
}