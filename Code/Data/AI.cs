using Flowframes.IO;

namespace Flowframes.Data
{
    public struct AI
    {
        public string aiName;
        public string aiNameShort;
        public string friendlyName;
        public string description;
        public FlowPackage pkg;
        public bool supportsAnyExp;

        public AI(string aiNameArg, string friendlyNameArg, string descArg, FlowPackage pkgArg, bool supportsAnyExpArg)
        {
            aiName = aiNameArg;
            aiNameShort = aiNameArg.Split(' ')[0];
            aiNameShort = aiNameArg.Split('_')[0];
            friendlyName = friendlyNameArg;
            description = descArg;
            pkg = pkgArg;
            supportsAnyExp = supportsAnyExpArg;
        }
    }
}
