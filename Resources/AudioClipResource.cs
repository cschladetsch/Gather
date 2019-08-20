namespace Gather.Resources
{
    using UnityEngine;
    using UnityEngine.Networking;

    using Impl;

    /// <summary>
    /// An audio clip resource downloaded from cloud.
    /// </summary>
    public class AudioClipResource
        : Resource<AudioClip>
    {
        public override DownloadHandler NewDownloadHandler()
            => new DownloadHandlerAudioClip(Metadata.Uri, AudioType.OGGVORBIS);

        public AudioClipResource()
        {
        }

        public AudioClipResource(string name) : base(name)
        {
        }

        public override bool Convert()
        {
            if (_Download is DownloadHandlerAudioClip audio)
            {
                Value = audio.audioClip;
                return true;
            }

            return false;
        }

        protected override bool FromBytes(byte[] bytes)
        {
            throw new System.NotImplementedException();
        }
    }
}

