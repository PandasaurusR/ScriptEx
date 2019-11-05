﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace ScriptExDee
{
    class QCMode
    {
        /// <summary>
        /// Code from Magnus Hjorth
        /// </summary>
        public static void FormatDrives()
        {
            string HARD_DRIVE = "3";
            int GPT = 2;

            // Create partitions
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"Root/Microsoft/Windows/Storage", "select * from MSFT_DISK");
            foreach (ManagementObject m in searcher.Get())
            {
                // Initialize disk first
                var res = m.InvokeMethod("Initialize", new object[] { GPT });   // Will have no effect on intialized disks

                Console.Write("[*] " + m["Model"].ToString().Trim());

                if (m["NumberOfPartitions"].ToString().Equals("0"))
                {
                    // https://docs.microsoft.com/en-au/previous-versions/windows/desktop/stormgmt/createpartition-msft-disk
                    // Use 1MB alignment = 1048576 bytes
                    var parRes = m.InvokeMethod("CreatePartition", new object[] { null, true, null, 1048576, null, true, null, null, false, false });
                    // Thread.Sleep(1000);
                    Console.WriteLine(" | INITIALISED");
                }
                else
                {
                    Console.WriteLine(" | IGNORED");
                }
            }
            Terminal.WriteLine("ALL DRIVES INITIALISED");
            Terminal.hRule();
            Thread.Sleep(2000);  // Formatting is unstable if drive letters are not given time to be created

            // Format 
            searcher = new ManagementObjectSearcher("select * from Win32_Volume");
            foreach (ManagementObject m in searcher.Get())
            {
                
                if ((m["DriveType"].ToString() == HARD_DRIVE) && (m["FileSystem"] == null))
                {
                    // https://docs.microsoft.com/en-au/previous-versions/windows/desktop/stormgmt/format-msft-volume
                    var res = m.InvokeMethod("Format", new object[] { "NTFS", true, 8192, "New Volume", false });
                    Console.WriteLine($"[#] {m["DriveLetter"].ToString()} CREATED");
                }

            }

            Terminal.WriteLine("ALL DRIVES FORMATTED");
            Terminal.hRule();

            // In case any drives are offline...
            /*
            searcher = new ManagementObjectSearcher(@"Root/Microsoft/Windows/Storage", "select * from MSFT_Partition");
            foreach (ManagementObject m in searcher.Get())
            {
                if (m["IsOffline"].ToString().ToLower().Equals("true"))
                {
                    var res = m.InvokeMethod("Online", new object[] { });
                }
            }
            */
        }

        /// <summary>
        /// Run Windows Activation
        /// </summary>
        public static void WinActivation()
        {
            // If program is not x64, system32 path gets redirected and this fails to launch
            string slui = Environment.GetFolderPath(Environment.SpecialFolder.System);
            ProcessStartInfo sInfo = new ProcessStartInfo(Path.Combine(slui, "slui.exe"));
            Process p = new Process();
            p.StartInfo = sInfo;
            p.Start();
        }

    }
}