﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace OSDiagTool.Utils {
    class WinUtils {

        [Flags]
        public enum ThreadAccess : int {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);
        [DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);


        public static void SuspendProcess(int pid) {
            var process = Process.GetProcessById(pid); // throws exception if process does not exist

            foreach (ProcessThread pT in process.Threads) {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero) {
                    continue;
                }

                SuspendThread(pOpenThread);

                CloseHandle(pOpenThread);
            }
        }

        public static void ResumeProcess(int pid) {
            var process = Process.GetProcessById(pid);

            if (process.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in process.Threads) {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero) {
                    continue;
                }

                var suspendCount = 0;
                do {
                    suspendCount = ResumeThread(pOpenThread);
                } while (suspendCount > 0);

                CloseHandle(pOpenThread);
            }
        }

        public static void WriteToFile(string filePath, string message) {

            if (!File.Exists(filePath)) { 
                using (File.Create(filePath));
            }

            File.AppendAllText(filePath, message + Environment.NewLine);

        }        

        // Retrieves the current status of a windows service
        public static string ServiceStatus(string serviceName) {
            ServiceController sc;
            try
            {
                sc = new ServiceController(serviceName);
            }
            catch (ArgumentException)
            {
                return "Invalid service name."; // Note that just because a name is valid does not mean the service exists.
            }

            using (sc)
            {
                ServiceControllerStatus status;
                try
                {
                    sc.Refresh(); // calling sc.Refresh() is unnecessary on the first use of `Status` but if you keep the ServiceController in-memory then be sure to call this if you're using it periodically.
                    status = sc.Status;
                }
                catch (Win32Exception ex)
                {
                    // A Win32Exception will be raised if the service-name does not exist or the running process has insufficient permissions to query service status.
                    return "Error: " + ex.Message;
                }

                switch (status)
                {
                    case ServiceControllerStatus.Running:
                        return "Running";
                    case ServiceControllerStatus.Stopped:
                        return "Stopped";
                    case ServiceControllerStatus.Paused:
                        return "Paused";
                    case ServiceControllerStatus.StopPending:
                        return "Stopping";
                    case ServiceControllerStatus.StartPending:
                        return "Starting";
                    default:
                        return "Changing status";
                }
            }
        }
    }
}
