using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace hosts
{
    internal class Program
    {
        // Leave old name for backward compatibility
        private const string COMMENT_AFLINK = "# AFLINK";

        static void Main(string[] args)
        {
            // Get the hosts file path from the registry
            // Windows 7+ does not use this key, but if modified, they probably knew what they're doing
            RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\");
            string hostsFile = null;
            if (regKey != null)
            {
                hostsFile = (string)regKey.GetValue("DataBasePath");
                regKey.Close();
            }
            if (hostsFile == null || hostsFile.Trim() == string.Empty)
            {
                hostsFile = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\drivers\etc");
            }
            hostsFile += @"\hosts";

            bool modified = args.Length > 0; // whether new entries exist
            List<string> result = new List<string>();
            if (File.Exists(hostsFile))
            {
                // Make the hosts file writeable
                FileAttributes attributes = File.GetAttributes(hostsFile);
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    File.SetAttributes(hostsFile, attributes & ~FileAttributes.ReadOnly);
                }

                foreach (var line in File.ReadLines(hostsFile))
                {
                    // Remove previous entries
                    if (line.Contains(COMMENT_AFLINK))
                    {
                        modified = true;
                        continue;
                    }
                    result.Add(line);
                }
            }
            if (!modified)
            {
                return;
            }

            // Add new entries
            for (int i = 0; i < args.Length; i++)
            {
                foreach (var host in args[i].Split(','))
                {
                    if (Uri.CheckHostName(host) != UriHostNameType.Dns)
                    {
                        throw new Exception("Invalid host!");
                    }
                    result.Add($"127.0.0.{i + 1} {host} {COMMENT_AFLINK}");
                }
            }
            File.WriteAllLines(hostsFile, result.ToArray());
        }
    }
}
