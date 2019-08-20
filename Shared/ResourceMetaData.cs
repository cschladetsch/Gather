namespace Gather.Shared
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// Information about a Resource that is shared with the server.
    /// </summary>
    public class ResourceMetadata
    {
        /// <summary>
        /// The database linear id.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Globally unique id for resource.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// Human-readable name for resource. This is optional.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional description for the resource.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The cloud location of the resource.
        /// </summary>
        public Uri Uri { get; set; }

        /// <summary>
        /// Give an idea to the client how large this resource may be.
        ///
        /// Used to estimate download time.
        /// </summary>
        public long SizeHint { get; set; } = 1000;

        /// <summary>
        /// A version number in the form YYYY-MM-DDThh-mm-ss.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// When this resource was last accessed in the cache.
        /// </summary>
        [JsonIgnore]
        public DateTime LastAccessTime;

        public override string ToString()
            => $"Name={Name}, Id={Id}, Uri={Uri}, SizeHint={SizeHint}";
    }
}

