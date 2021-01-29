using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class GetWebInfo
    {
        public static async Task LoadNews(Label newsLabel)
        {
            string url = $"http://dl.nmkd.de/flowframes/changelog.txt";
            var client = new WebClient();
            var str = await client.DownloadStringTaskAsync(new Uri(url));
            newsLabel.Text = str;
        }

        public static async Task LoadPatronList(Label patronsLabel)
        {
            string url = $"http://dl.nmkd.de/flowframes/patreon.txt";
            var client = new WebClient();
            var str = await client.DownloadStringTaskAsync(new Uri(url));
            patronsLabel.Text = str;
        }

        public static async Task LoadPatronListCsv(Label patronsLabel)
        {
            string url = $"http://dl.nmkd.de/flowframes/patrons.csv";
            var client = new WebClient();
            var csvData = await client.DownloadStringTaskAsync(new Uri(url));
            patronsLabel.Text = ParsePatreonCsv(csvData);
        }

        public static string ParsePatreonCsv(string csvData)
        {
            try
            {
                List<string> goldPatrons = new List<string>();
                List<string> silverPatrons = new List<string>();
                string str = "Gold:\n";
                string[] lines = csvData.SplitIntoLines();
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Replace(";", ",");
                    string[] values = line.Split(',');
                    if (i == 0 || line.Length < 10 || values.Length < 5) continue;
                    string name = values[0];
                    float amount = float.Parse(values[7], System.Globalization.CultureInfo.InvariantCulture);
                    if (amount >= 4.5f)
                    {
                        if (amount >= 11f)
                            goldPatrons.Add(name);
                        else
                            silverPatrons.Add(name);
                    }
                }

                foreach (string pat in goldPatrons)
                    str += pat + "\n";

                str += "\nSilver:\n";

                foreach (string pat in silverPatrons)
                    str += pat + "\n";

                return str;
            }
            catch (Exception e)
            {
                Logger.Log("Failed to parse Patreon CSV: " + e.Message, true);
                return "Failed to load patron list.";
            }
        }
    }
}
