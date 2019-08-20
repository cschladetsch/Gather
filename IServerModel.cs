namespace Gather
{
    using System;
    using System.Net;
    using UniRx;
    using Dekuple.Model;
    using Shared;

    /// <inheritdoc />
    /// <summary>
    /// Local reactive data representation of remote server.
    /// </summary>
    public interface IServerModel
        : IModel
    {
        /// <summary>
        /// The address <b>and</b> port of the server.
        /// </summary>
        IPEndPoint EndPoint { get; }

        /// <summary>
        /// Indicates when the user is (dis)connected from the server.
        /// </summary>
        IReadOnlyReactiveProperty<bool> IsConnected { get; }

        /// <summary>
        /// Default timeout for requests from server.
        /// </summary>
        TimeSpan DefaultTimeOut { get; }

        /// <summary>
        /// Add a new resource to the cache(s).
        /// </summary>
        /// <param name="resource">The resource to add to the cache.</param>
        void AddResource(IResource resource);

        /// <summary>
        /// Attempt to retrieve a resource with the given Id from cache.
        /// </summary>
        /// <param name="metadata">The metadata of the resource to find.</param>
        /// <param name="resource">The found resource, if any.</param>
        /// <returns>True if the resource was found in any available cache.</returns>
        bool TryGetResource<T>(ResourceMetadata metadata, out IResource resource)
            where T : class, IResource, new();

        /// <summary>
        /// Make a complete Uri for an api call, given the path.
        /// </summary>
        /// <returns>The Uri that can invoked to make the server request.</returns>
        Uri BuildServerApiCall(string path);

        bool TryGetMetadata(Guid id, out ResourceMetadata metadata);
    }
}

