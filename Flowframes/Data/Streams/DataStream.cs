namespace Flowframes.Data.Streams
{
    public class DataStream : Stream
    {
        public DataStream(string codec, string codecLong)
        {
            base.Type = StreamType.Data;
            Codec = codec;
            CodecLong = codecLong;
        }

        public override string ToString()
        {
            return $"{base.ToString()}";
        }
    }
}
