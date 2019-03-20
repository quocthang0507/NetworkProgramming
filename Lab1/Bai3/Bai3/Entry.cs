﻿using System.Net;

namespace Bai3
{
	class Entry
	{
		private IPAddress[] listIP;
		private string hostName;

		public IPAddress[] ListIP { get => listIP; set => listIP = value; }
		public string HostName { get => hostName; set => hostName = value; }

		public Entry(IPAddress[] ip, string hostName)
		{
			ListIP = ip;
			HostName = hostName;
		}

		public override string ToString()
		{
			string ip_str = "";
			foreach (var i in ListIP)
			{
				ip_str += "\t" + i.ToString() + "\n";
			}
			return string.Format("Host name:\t{0}\nIP address(es): {1}", HostName, ip_str);
		}
	}
}
