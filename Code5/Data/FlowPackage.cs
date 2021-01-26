using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.IO
{
    public struct FlowPackage
    {
        public string friendlyName;
        public string fileName;
        public int downloadSizeMb;
        public string desc;

        public FlowPackage(string friendlyNameStr, string fileNameStr, int downloadSizeMbInt, string description)
        {
            friendlyName = friendlyNameStr;
            fileName = fileNameStr;
            downloadSizeMb = downloadSizeMbInt;
            desc = description;
        }
    }
}
