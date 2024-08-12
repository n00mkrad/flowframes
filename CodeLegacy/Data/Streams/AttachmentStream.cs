namespace Flowframes.Data.Streams
{
    public class AttachmentStream : Stream
    {
        public string Filename { get; } = "";
        public string MimeType { get; } = "";

        public AttachmentStream(string codec, string codecLong, string filename, string mimeType)
        {
            base.Type = StreamType.Attachment;
            Codec = codec;
            CodecLong = codecLong;
            Filename = filename;
            MimeType = mimeType;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Filename: {Filename} - MIME Type: {MimeType}";
        }
    }
}
