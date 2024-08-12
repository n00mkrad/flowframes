namespace Flowframes.Data.Streams
{
    public class Stream
    {
        public enum StreamType { Video, Audio, Subtitle, Data, Attachment, Unknown }
        public StreamType Type;
        public int Index = -1;
        public bool IsDefault = false;
        public string Codec = "";
        public string CodecLong = "";
        public string Language = "";
        public string Title = "";

        public override string ToString()
        {
            return $"Stream #{Index.ToString().PadLeft(2, '0')}{(IsDefault ? "*" : "")} - {Codec} {Type}";
        }
    }
}
