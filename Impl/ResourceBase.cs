namespace Gather.Impl
{
    using System;

    using UnityEngine.Networking;

    using Flow;
    using Dekuple.Model;

    using Shared;

    /// <inheritdoc cref="IResource" />
    /// <summary>
    /// Base for all resources possibly stored in the cloud.
    /// </summary>
    public abstract class ResourceBase
        : ModelBase
        , IResource
    {
        public new string Name
        {
            get => _Metadata.Name;
            set => _Metadata.Name = value;
        }

        public RequestResult Result { get; protected set; }
        public ResourceMetadata Metadata => _Metadata ?? (_Metadata = new ResourceMetadata());
        public object Value { get; set; }
        public string Description { get; set; }
        public Uri Uri { get => Metadata.Uri; set => Metadata.Uri = value; }
        public Guid ResourceId { get => Metadata.Guid; set => Metadata.Guid = value; }
        public bool Exists => Result != null && !Result.Failed && ResourceId != Guid.Empty;
        public abstract byte[] Bytes { get; set; }

        protected ResourceMetadata _Metadata;

        public abstract bool Construct(ResourceMetadata meta, IKernel kernel);

        public abstract bool Construct(ResourceMetadata meta, byte[] bytes);

        public abstract DownloadHandler NewDownloadHandler();

        public abstract IFuture<bool> Fetch();

        public abstract bool Convert();

        public override string ToString()
            => $"Name='{Metadata.Name}', id={ResourceId}";

        protected abstract bool FromBytes(byte[] bytes);
    }
}

