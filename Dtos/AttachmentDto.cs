namespace pviBase.Dtos
{
    public class AttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] FileData { get; set; } = Array.Empty<byte>();
    }

}
