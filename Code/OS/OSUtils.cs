﻿using DiskDetector;
using DiskDetector.Models;
using Flowframes.IO;
using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.Text;

namespace Flowframes.OS
{
    class OSUtils
    {
        public static bool IsUserAdministrator()
        {
            //bool value to hold our return value
            bool isAdmin;
            WindowsIdentity user = null;
            try
            {
                //get the currently logged in user
                user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            finally
            {
                if (user != null)
                    user.Dispose();
            }
            return isAdmin;
        }

        public static Process SetStartInfo(Process proc, bool hidden, string filename = "cmd.exe")
        {
            proc.StartInfo.UseShellExecute = !hidden;
            proc.StartInfo.RedirectStandardOutput = hidden;
            proc.StartInfo.RedirectStandardError = hidden;
            proc.StartInfo.CreateNoWindow = hidden;
            proc.StartInfo.FileName = filename;
            return proc;
        }

        public static Process NewProcess(bool hidden, string filename = "cmd.exe")
        {
            Process proc = new Process();
            return SetStartInfo(proc, hidden, filename);
        }

        public static void KillProcessTree(int pid)
        {
            ManagementObjectSearcher processSearcher = new ManagementObjectSearcher
              ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection processCollection = processSearcher.Get();

            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited) proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }

            if (processCollection != null)
            {
                foreach (ManagementObject mo in processCollection)
                {
                    KillProcessTree(Convert.ToInt32(mo["ProcessID"])); //kill child processes(also kills childrens of childrens etc.)
                }
            }
        }

        public static string GetCmdArg()
        {
            bool stayOpen = Config.GetInt("cmdDebugMode") == 2;
            if (stayOpen)
                return "/K";
            else
                return "/C";
        }

        public static bool ShowHiddenCmd()
        {
            return Config.GetInt("cmdDebugMode") > 0;
        }

        public static bool DriveIsSSD(string path)
        {
            try
            {
                var detectedDrives = Detector.DetectFixedDrives(QueryType.SeekPenalty);
                if (detectedDrives.Count != 0)
                {
                    char pathDriveLetter = (path[0].ToString().ToUpper())[0];
                    foreach (var detectedDrive in detectedDrives)
                    {
                        if (detectedDrive.DriveLetter == pathDriveLetter && detectedDrive.HardwareType.ToString().ToLower().Trim() == "ssd")
                            return true;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed to detect drive type: " + e.Message);
                return true;    // Default to SSD on fail
            }
            return false;
        }

        public static bool HasNonAsciiChars(string str)
        {
            return (Encoding.UTF8.GetByteCount(str) != str.Length);
        }

        public static int GetFreeRamMb()
        {
            try
            {
                return (int)(new ComputerInfo().AvailablePhysicalMemory / 1048576);
            }
            catch
            {
                return 1000;
            }
        }
    }
}