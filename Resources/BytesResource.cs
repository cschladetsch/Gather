namespace Gather.Resources
{
    using UnityEngine.Networking;
    using Impl;

    /// <summary>
    /// A raw sequence of bytes.
    /// </summary>
    public class BytesResource
        : Resource<byte[]>
    {
        public override DownloadHandler NewDownloadHandler()
            => new DownloadHandlerBuffer();

        public BytesResource()
        {
        }

        public BytesResource(string name)
            : base(name)
        {
        }

        public override bool Convert()
        {
            Value = Bytes = _Download.data;
            return true;
        }

        protected override bool FromBytes(byte[] bytes)
        {
            Value = Bytes = bytes;
            return true;
        }
    }
}

