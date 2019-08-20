namespace Gather.Resources
{
    using UnityEngine;
    using UnityEngine.Networking;
    using Impl;

    /// <inheritdoc />
    /// <summary>
    /// A texture2d resource fetched from the cloud.
    /// </summary>
    public class Texture2DResource
        : Resource<Texture2D>
    {
        public override DownloadHandler NewDownloadHandler()
            => new DownloadHandlerTexture();

        public Texture2DResource()
        {
        }

        public Texture2DResource(string name)
            : base(name)
        {
        }

        public override bool Convert()
        {
            Bytes = _Download.data;
            if (_Download is DownloadHandlerTexture resource)
            {
                Value = resource.texture;
                return true;
            }

            return false;
        }

        protected override bool FromBytes(byte[] bytes)
        {
            Bytes = bytes;
            Value = null;

            var tex = new Texture2D(1, 1);
            if (!tex.LoadImage(Bytes))
                return false;

            Value = tex;
            return true;
        }
    }
}

