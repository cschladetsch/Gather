namespace Gather
{
    using System;
    using UnityEngine.Networking;
    using Dekuple;
    using Dekuple.Model;
    using Flow;
    using Shared;

    /// <inheritdoc cref="IHasId" />
    /// <summary>
    /// Representation of a resource either stored locally (cached)
    /// or obtained from remote storage.
    /// </summary>
    public interface IResource
        : IModel
        , INamed
    {
        /// <summary>
        /// Information about the resource such as its Name, Description, Size, Location, etc.
        /// </summary>
        ResourceMetadata Metadata { get; }

        /// <summary>
        /// Globally resource value.
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Globally unique id for resource.
        /// </summary>
        Guid ResourceId { get; set; }

        /// <summary>
        /// Optional description for the resource.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// The cloud location of the resource.
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// The status of the resource request.
        /// </summary>
        RequestResult Result { get; }

        /// <summary>
        /// The raw bytes of the resource.
        /// </summary>
        byte[] Bytes { get; set; }

        /// <summary>
        /// Construct from remote.
        /// </summary>
        bool Construct(ResourceMetadata meta, IKernel kernel);

        /// <summary>
        /// Make a new resource given metadata and a byte sequence.
        /// </summary>
        bool Construct(ResourceMetadata data, byte[] bytes);

        /// <summary>
        /// Make a new type-specific handler for this resource.
        /// </summary>
        DownloadHandler NewDownloadHandler();

        /// <summary>
        /// Obtain from cache or fetch from cloud.
        /// </summary>
        /// <returns>A future success indicator for the operation. More info can be found in <see cref="Result"/></returns>
        IFuture<bool> Fetch();

        /// <summary>
        /// Convert from raw byte-sequence data to a ready-to-use resource of type &lt;T&gt;.
        /// </summary>
        /// <returns>True if conversion succeeded.</returns>
        bool Convert();
    }

    /// <inheritdoc />
    /// <summary>
    /// Base for resources of a specific type &lt;T&gt;.
    /// </summary>
    /// <typeparam name="T">The type of the resource.</typeparam>
    public interface IResource<out T>
        : IResource
    {
        new T Value { get; }
    }
}

