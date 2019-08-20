namespace Gather.Resources
{
    using System.Text;
    using UnityEngine.Networking;

    /// <inheritdoc />
    /// <summary>
    /// A string resource.
    /// </summary>
    public class StringResource
        : BytesResource
    {
        public new string Value;

        public override bool Convert()
        {
            if (_Download is DownloadHandlerBuffer resource)
            {
                base.Value = Bytes = resource.data;
                Value = resource.text;

                return true;
            }

            return false;
        }

        protected override bool FromBytes(byte[] bytes)
        {
            base.FromBytes(bytes);
            Value = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
            return true;
        }
    }
}

