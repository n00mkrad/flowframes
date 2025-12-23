using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace Flowframes.Data
{
    class Servers
    {
        public static Server hetznerEu = new Server { name = "Germany (Nürnberg)", host = "nmkd-hz.de", pattern = "https://dl.*" };
        public static Server contaboUs = new Server { name = "USA (St. Louis)", host = "nmkd-cb.de", pattern = "https://dl.*" };

        public static List<Server> serverList = new List<Server> { hetznerEu, contaboUs };

        private static Server closestServer = serverList[0];

        public class Server
        {
            public string name = "";
            public string host = "";
            public string pattern = "*";

            public string GetUrl()
            {
                return pattern.Replace("*", host);
            }
        }

        public static async Task InitAsync ()
        {
            await Task.Run(() => Init());
        }

        private const int _defaultPing = 10000;

        public static void Init(ComboBox comboBox = null)
        {
            Dictionary<string[], long> serversPings = new Dictionary<string[], long>();

            foreach (Server server in serverList)
            {
                try
                {
                    long ping = _defaultPing;
                    int attempts = 0;

                    while(ping == _defaultPing && attempts < 3)
                    {
                        Ping p = new Ping();
                        PingReply pReply = p.Send(server.host, 3000);

                        if(pReply.RoundtripTime > 0)
                        {
                            ping = pReply.RoundtripTime;
                        }

                        attempts++;
                    }

                    serversPings[new string[] { server.name, server.host, server.pattern }] = ping;
                    Logger.Log($"[Servers] Ping to {server.host}: {ping} ms", true);
                }
                catch (Exception e)
                {
                    Logger.Log($"[Servers] Failed to ping {server.host}: {e.Message}", true);
                    serversPings[new string[] { server.name, server.host, server.pattern }] = _defaultPing;
                }
            }

            var closest = serversPings.Aggregate((l, r) => l.Value < r.Value ? l : r);
            Logger.Log($"[Servers] Closest Server: {closest.Key[0]} ({closest.Value} ms)", true);
            closestServer = new Server { name = closest.Key[0], host = closest.Key[1], pattern = closest.Key[2] };

            if (comboBox != null)
            {
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    if (comboBox.Items[i].ToString() == closestServer.name)
                        comboBox.SelectedIndex = i;
                }
            }
        }

        public static Server GetServer ()
        {
            int server = Config.GetInt("serverCombox");

            if (server == 0)
                return closestServer;
            else
                return serverList[server - 1];
        }
    }
}
