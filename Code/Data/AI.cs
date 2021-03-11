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
        public string pkgDir;
        public bool supportsAnyExp;

        public AI(string aiNameArg, string friendlyNameArg, string descArg, string pkgDirArg, bool supportsAnyExpArg)
        {
            aiName = aiNameArg;
            aiNameShort = aiNameArg.Split(' ')[0].Split('_')[0];
            friendlyName = friendlyNameArg;
            description = descArg;
            pkgDir = pkgDirArg;
            supportsAnyExp = supportsAnyExpArg;
        }
    }
}
