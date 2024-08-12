namespace Flowframes.Data
{
    class QueryInfo
    {
        public string path;
        public long filesize;
        public string cmd = null;

        public QueryInfo(string path, long filesize, string cmd = null)
        {
            this.path = path;
            this.filesize = filesize;
            this.cmd = cmd;
        }
    }
}
