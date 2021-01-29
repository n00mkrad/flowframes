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
