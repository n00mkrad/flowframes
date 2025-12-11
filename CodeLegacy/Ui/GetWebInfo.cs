using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.Ui
{
    class GetWebInfo
    {
        private static async Task<string> GetWebText(string url, System.Text.Encoding encoding = null)
        {
            try
            {
                using (var client = new WebClient { Encoding = encoding ?? System.Text.Encoding.UTF8 })
                {
                    string text = await client.DownloadStringTaskAsync(new Uri(url));
                    return text;
                }
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to load text from URL: {e.Message}", true);
                return "";
            }
        }

        public static async Task LoadNews (Label newsLabel)
        {
            string text = await GetWebText("https://raw.githubusercontent.com/n00mkrad/flowframes/main/changelog.txt");
            newsLabel.Invoke(() => newsLabel.Text = text);
        }

        public static async Task LoadPatronListCsv(Label patronsLabel)
        {
            string csvData = await GetWebText("https://raw.githubusercontent.com/n00mkrad/flowframes/main/patrons.csv");
            var patronsText = ParsePatreonCsv(csvData);
            patronsLabel.Invoke(() => patronsLabel.Text = patronsText);
        }

        public static string ParsePatreonCsv(string csvData)
        {
            var badNamesEnc = new List<string>() { "bmlnZ2Vy" };
            var badNames = badNamesEnc.Select(n => Convert.FromBase64String(n)).Select(b => System.Text.Encoding.UTF8.GetString(b)).ToList();

            try
            {
                List<string> goldPatrons = new List<string>();
                List<string> silverPatrons = new List<string>();
                string patronsStr = "";
                string[] lines = csvData.SplitIntoLines().Select(l => Regex.Replace(l, @";{2,}", ";").Replace(";", ",").Trim(',')).ToArray();

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string[] parts = line.Split(',');
                    if (parts.Length < 3)
                        continue;
                    string name = parts[0].Trim().Trunc(45);
                    string status = parts[1].Trim();
                    string tier = parts[2].Trim();

                    if (!status.StartsWith("Active") || badNames.Contains(name))
                        continue;

                    if (tier.Contains("Gold"))
                        goldPatrons.Add(name);

                    if (tier.Contains("Silver"))
                        silverPatrons.Add(name);
                }

                // Logger.Log($"Found {goldPatrons.Count} Gold Patrons, {silverPatrons.Count} Silver Patrons", true);

                if(goldPatrons.Count > 0)
                {
                    patronsStr += $"Gold:\n{string.Join("\n", goldPatrons)}\n\n";
                }

                if(silverPatrons.Count > 0)
                {
                    patronsStr += $"Silver:\n{string.Join("\n", silverPatrons)}\n\n";
                }

                return patronsStr;
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to parse Patreon CSV: {e.Message}\n{e.StackTrace}", true);
                return "Failed to load patron list.";
            }
        }
    }
}
