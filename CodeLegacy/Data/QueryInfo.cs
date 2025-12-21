namespace Flowframes.Data
{
    class QueryInfo
    {
        public string Path;
        public long SizeBytes;
        public string Command = "";

        public QueryInfo(string path, long filesize = 0, string cmd = "")
        {
            Path = path;
            SizeBytes = filesize;
            Command = cmd;
        }

        public override bool Equals(object obj)
        {
            if (obj is QueryInfo other)
                return Path == other.Path && SizeBytes == other.SizeBytes && Command == other.Command;
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (Path?.GetHashCode() ?? 0);
                hash = hash * 31 + SizeBytes.GetHashCode();
                hash = hash * 31 + (Command?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
