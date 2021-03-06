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
			/* Bởi vì Client không cần chờ trên một port UDP định sắn nên nó cũng chẳng cần dùng phương thức Bind(), 
			 * thay vì vậy nó sẽ lấy một port ngẫu nhien trên hệ thống khi dữ liệu được gởi và nó giữa port này để 
			 * nhận dữ liệu trả về. */
			IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
			Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

			byte[] buff = Encoding.UTF8.GetBytes("Hello server!");
			Console.WriteLine("Sending a message to server...");
			server.SendTo(buff, buff.Length, SocketFlags.None, serverEndPoint);
			Console.WriteLine("Sended message successfully");
			Console.ReadKey();
		}
	}
}
