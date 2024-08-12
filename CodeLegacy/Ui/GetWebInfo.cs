using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    class GetWebInfo
    {
        public static async Task LoadNews (Label newsLabel)
        {
            try
            {
                string url = $"https://raw.githubusercontent.com/n00mkrad/flowframes/main/changelog.txt";
                var client = new WebClient();
                var str = await client.DownloadStringTaskAsync(new Uri(url));
                newsLabel.Text = str;
            }
            catch(Exception e)
            {
                Logger.Log($"Failed to load news: {e.Message}", true);
            }
        }

        public static async Task LoadPatronListCsv(Label patronsLabel)
        {
            try
            {
                string url = $"https://raw.githubusercontent.com/n00mkrad/flowframes/main/patrons.csv";
                var client = new WebClient();
                var csvData = await client.DownloadStringTaskAsync(new Uri(url));
                patronsLabel.Text = ParsePatreonCsv(csvData);
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to load patreon CSV: {e.Message}", true);
            }
        }

        public static string ParsePatreonCsv(string csvData)
        {
            try
            {
                Logger.Log("Parsing Patrons from CSV...", true);
                List<string> goldPatrons = new List<string>();
                List<string> silverPatrons = new List<string>();
                string str = "Gold:\n";
                string[] lines = csvData.SplitIntoLines().Select(x => x.Replace(";", ",")).ToArray();

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] values = line.Split(',');
                    if (i == 0 || line.Length < 10 || values.Length < 5) continue;
                    string name = values[0].Trim();
                    string status = values[4].Trim();
                    string tier = values[9].Trim();

                    if (status.Contains("Active"))
                    {
                        if (tier.Contains("Gold"))
                            goldPatrons.Add(name.Trunc(30));

                        if (tier.Contains("Silver"))
                            silverPatrons.Add(name.Trunc(30));
                    }
                }

                Logger.Log($"Found {goldPatrons.Count} Gold Patrons, {silverPatrons.Count} Silver Patrons", true);

                foreach (string pat in goldPatrons)
                    str += pat + "\n";

                str += "\nSilver:\n";

                foreach (string pat in silverPatrons)
                    str += pat + "\n";

                return str;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to parse Patreon CSV: {e.Message}\n{e.StackTrace}", true);
                return "Failed to load patron list.";
            }
        }
    }
}
