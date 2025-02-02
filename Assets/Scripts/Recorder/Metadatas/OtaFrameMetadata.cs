namespace ota.ndi
{
    /// <summary>
    /// A metadata encoded each frame into video file.
    /// </summary>
    internal sealed partial class OtaFrameMetadata
    {
        public BasicMetadata camera { get; set; }
        public BackgroundMetadata background { get; set; }

    }
}
