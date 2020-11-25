using Flowframes.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flowframes.Data
{
    public struct AI
    {
        public string aiName;
        public string aiNameShort;
        public string friendlyName;
        public string description;
        public FlowPackage pkg;
        public int minPkgVer;

        public AI(string aiNameArg, string friendlyNameArg, string descArg, FlowPackage pkgArg, int minPkgVerForThisBuild)
        {
            aiName = aiNameArg;
            aiNameShort = aiNameArg.Split(' ')[0];
            aiNameShort = aiNameArg.Split('_')[0];
            friendlyName = friendlyNameArg;
            description = descArg;
            pkg = pkgArg;
            minPkgVer = minPkgVerForThisBuild;
        }
    }
}
