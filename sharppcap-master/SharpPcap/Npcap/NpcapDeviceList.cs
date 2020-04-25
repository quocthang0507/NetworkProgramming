/*
This file is part of SharpPcap.

SharpPcap is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

SharpPcap is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with SharpPcap.  If not, see <http://www.gnu.org/licenses/>.
*/
/* 
 * Copyright 2011 Chris Morgan <chmorgan@gmail.com>
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpPcap.Npcap
{
	/// <summary>
	/// Remote adapter list
	/// </summary>
	public class NpcapDeviceList : ReadOnlyCollection<NpcapDevice>
	{
		/// <summary>
		/// Port used by rpcapd by default
		/// </summary>
		public static int RpcapdDefaultPort = 2002;

		private static NpcapDeviceList instance;

		/// <summary>
		/// Method to retrieve this classes singleton instance
		/// </summary>
		public static NpcapDeviceList Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new NpcapDeviceList();
				}

				return instance;
			}
		}

		/// <summary>
		/// Caution: Use the singlton instance unless you know why you need to call this.
		/// One use is for multiple filters on the same physical device. To apply multiple
		/// filters open the same physical device multiple times, one for each
		/// filter by calling this routine and picking the same device out of each list.
		/// </summary>
		/// <returns>
		/// A <see cref="CaptureDeviceList"/>
		/// </returns>
		public static NpcapDeviceList New()
		{
			return new NpcapDeviceList();
		}

		/// <summary>
		/// Represents a strongly typed, read-only list of PcapDevices.
		/// </summary>
		private NpcapDeviceList() : base(new List<NpcapDevice>())
		{
			Refresh();
		}

		/// <summary>
		/// Returns a list of devices
		/// </summary>
		/// <param name="address">
		/// A <see cref="IPAddress"/>
		/// </param>
		/// <param name="port">
		/// A <see cref="int"/>
		/// </param>
		/// <param name="remoteAuthentication">
		/// A <see cref="RemoteAuthentication"/>
		/// </param>
		/// <returns>
		/// A <see cref="List<NpcapDevice>"/>
		/// </returns>
		public static List<NpcapDevice> Devices(IPAddress address,
												  int port,
												  RemoteAuthentication remoteAuthentication)
		{
			// build the remote string
			var rmStr = string.Format("rpcap://{0}:{1}",
									  address,
									  port);
			return Devices(rmStr,
						   remoteAuthentication);
		}

		public static List<NpcapDevice> Devices()
		{
			var devicePtr = IntPtr.Zero;
			var errorBuffer = new StringBuilder(Pcap.PCAP_ERRBUF_SIZE); //will hold errors

			var result = SafeNativeMethods.pcap_findalldevs(ref devicePtr, errorBuffer);

			if (result < 0)
				throw new PcapException(errorBuffer.ToString());

			var retval = BuildDeviceList(devicePtr);

			LibPcap.LibPcapSafeNativeMethods.pcap_freealldevs(devicePtr);  // Free unmanaged memory allocation.

			return retval;
		}

		private static List<NpcapDevice> Devices(string rpcapString,
												   RemoteAuthentication remoteAuthentication)
		{
			var devicePtr = IntPtr.Zero;
			var errorBuffer = new StringBuilder(Pcap.PCAP_ERRBUF_SIZE); //will hold errors

			// convert the remote authentication structure to unmanaged memory if
			// one was specified
			int result;
			IntPtr rmAuthPointer;
			if (remoteAuthentication == null)
				rmAuthPointer = IntPtr.Zero;
			else
				rmAuthPointer = remoteAuthentication.GetUnmanaged();

			try
			{
				result = SafeNativeMethods.pcap_findalldevs_ex(rpcapString, rmAuthPointer, ref devicePtr, errorBuffer);
			}
			finally
			{
				// free the memory if any was allocated
				if (rmAuthPointer != IntPtr.Zero)
					Marshal.FreeHGlobal(rmAuthPointer);
			}

			if (result < 0)
				throw new PcapException(errorBuffer.ToString());

			IntPtr nextDevPtr = devicePtr;

			var retval = BuildDeviceList(devicePtr);

			LibPcap.LibPcapSafeNativeMethods.pcap_freealldevs(devicePtr);  // Free unmanaged memory allocation.

			return retval;
		}

		private static List<NpcapDevice> BuildDeviceList(IntPtr devicePtr)
		{
			var retval = new List<NpcapDevice>();
			foreach (var pcap_if in LibPcap.PcapInterface.GetAllPcapInterfaces(devicePtr))
			{
				retval.Add(new NpcapDevice(pcap_if));
			}
			return retval;
		}

		/// <summary>
		/// Refresh the device list
		/// </summary>
		public void Refresh()
		{
			lock (this)
			{
				// retrieve the current device list
				var newDeviceList = Devices();

				// update existing devices with values in the new list
				foreach (var newItem in newDeviceList)
				{
					foreach (var existingItem in base.Items)
					{
						if (newItem.Name == existingItem.Name)
						{
							// copy the flags and addresses over
							existingItem.Interface.Flags = newItem.Interface.Flags;
							existingItem.Interface.Addresses = newItem.Interface.Addresses;

							break; // break out of the foreach(existingItem)
						}
					}
				}

				// find items the current list is missing
				foreach (var newItem in newDeviceList)
				{
					bool found = false;
					foreach (var existingItem in base.Items)
					{
						if (existingItem.Name == newItem.Name)
						{
							found = true;
							break;
						}
					}

					// add items that we were missing
					if (!found)
					{
						base.Items.Add(newItem);
					}
				}

				// find items that we have that the current list is missing
				var itemsToRemove = new List<NpcapDevice>();
				foreach (var existingItem in base.Items)
				{
					bool found = false;

					foreach (var newItem in newDeviceList)
					{
						if (existingItem.Name == newItem.Name)
						{
							found = true;
							break;
						}
					}

					// add the PcapDevice we didn't see in the new list
					if (!found)
					{
						itemsToRemove.Add(existingItem);
					}
				}

				// remove the items outside of the foreach() to avoid
				// enumeration errors
				foreach (var itemToRemove in itemsToRemove)
				{
					base.Items.Remove(itemToRemove);
				}
			}
		}

		#region Device Indexers
		/// <param name="Name">The name or description of the pcap interface to get.</param>
		public NpcapDevice this[string Name]
		{
			get
			{
				// lock to prevent issues with multi-threaded access
				// with other methods
				lock (this)
				{
					var devices = (List<NpcapDevice>)base.Items;
					var dev = devices.Find(delegate (NpcapDevice i) { return i.Name == Name; });
					var result = dev ?? devices.Find(delegate (NpcapDevice i) { return i.Description == Name; });

					if (result == null)
						throw new IndexOutOfRangeException();
					return result;
				}
			}
		}
		#endregion
	}
}

