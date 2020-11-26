using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Flowframes.UI
{
    class GetWebInfo
    {
        public static async Task LoadNews (Label newsLabel)
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
    }
}
