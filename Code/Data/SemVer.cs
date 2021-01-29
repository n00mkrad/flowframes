namespace Flowframes.Data
{
    public struct SemVer
    {
        public int major;
        public int minor;
        public int patch;

        public SemVer(int majorNum, int minorNum, int patchNum)
        {
            major = majorNum;
            minor = minorNum;
            patch = patchNum;
        }

        public SemVer(string versionStr)
        {
            string[] nums = versionStr.Trim().Split('.');
            major = nums[0].GetInt();
            minor = nums[1].GetInt();
            patch = nums[2].GetInt();
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{patch}";
        }
    }
}
