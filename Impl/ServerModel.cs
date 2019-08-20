#pragma warning disable 649

namespace Gather
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Linq;
    using System.Net.Sockets;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Dekuple.Model;
    using UniRx;
    using Shared;

    /// <inheritdoc cref="IServerModel"/>
    public class ServerModel
        : ModelBase
        , IServerModel
    {
        public IPEndPoint EndPoint { get; }
        public IReadOnlyReactiveProperty<bool> IsConnected => _isConnected;

        // TODO: dynamically `guess` the timeout given estimated network
        // TODO: speed and size of the download if known.
        public TimeSpan DefaultTimeOut { get; } = TimeSpan.FromSeconds(300);

        /// <summary>
        /// The end-point for the server to use.
        /// e.g. 'staging.api.liminalvr.com/api/'
        /// </summary>
        public string EndPointBase => $"{UriScheme}://{EndPoint}/{ApiRoot}/"
            .Replace(":0", "")
            .Replace("[::1]", "localhost");
        private const long MaxCacheSize = 2500L << 20;    // 2.5GB
        private const string UriScheme = "http";
        private const string ApiRoot = "api";

        private readonly ReactiveProperty<bool> _isConnected = new ReactiveProperty<bool>(false);
        private readonly Dictionary<Guid, IResource> _resources = new Dictionary<Guid, IResource>();

        public ServerModel(IPEndPoint endPoint)
        {
            //Verbosity = 100;

            Verbose(10, $"Using server {endPoint}.");

            EndPoint = endPoint;

            Directory.CreateDirectory(CachePathname);
        }

        public Uri BuildServerApiCall(string path)
            => new Uri($"{EndPointBase}{path}");

        public void AddResource(IResource resource)
        {
            if (resource?.Bytes == null)
            {
                Warn("Attempt to add null resource.");
                return;
            }

            if (_resources.ContainsKey(resource.Id))
            {
                Warn($"Attempt to add duplicate resource {resource}.");
                return;
            }

            _resources[resource.Id] = resource;

            if (!StoreLocally(resource))
                Warn($"Failed to store {resource}.");
        }

        public bool TryGetMetadata(Guid id, out ResourceMetadata metadata)
        {
            var metadataPathname = $"{MakeLocalRootName(id)}.json";
            if (!File.Exists(metadataPathname))
            {
                metadata = null;
                return false;
            }

            var json = File.ReadAllText(metadataPathname);
            metadata = JsonConvert.DeserializeObject<ResourceMetadata>(json);
            return true;
        }

        public bool TryGetResource<T>(ResourceMetadata metadata, out IResource resource)
            where T : class, IResource, new()
        {
            if (_resources.TryGetValue(metadata.Guid, out resource))
                return true;

            var pathName = GetFileFromGuid(metadata.Guid);
            if (!File.Exists(pathName))
            {
                Warn($"Resource {metadata.Name}, id={metadata.Guid} not found. Deleting metadata.");
                File.Delete($"{pathName}.json");
                return false;
            }

            resource = new T();
            var constructed = resource.Construct(metadata, File.ReadAllBytes(pathName));
            if (constructed)
                _resources[resource.ResourceId] = resource;

            return constructed;
        }

        private bool StoreLocally(IResource resource)
        {
            long resourceSize = resource.Bytes.Length;
            var currentSize = CalcCacheSize();
            var required = currentSize + resourceSize;
            if (required > MaxCacheSize)
            {
                if (!FreeCacheSpace(currentSize, resourceSize))
                {
                    Warn($"Cache full: {required} > {MaxCacheSize}");
                    return false;
                }
            }

            var name = MakeLocalRootName(resource.ResourceId);
            File.WriteAllText(name + ".json",JsonConvert.SerializeObject(resource.Metadata, Formatting.Indented));
            File.WriteAllBytes(MakeResourceName(resource, name), resource.Bytes);

            return true;
        }

        private static bool FreeCacheSpace(long current, long required)
        {
            var released = 0L;
            var spare = MaxCacheSize - current;
            var files = CacheFiles.OrderBy(f => f.LastAccessTime).Reverse().ToList();
            while (spare + released < required)
            {
                if (files.Count == 0)
                    return false;

                var index = files.Count - 1;
                var file = files[index];
                released += file.Length;
                files.RemoveAt(index);

                File.Delete(Path.Combine(CachePathname, file.Name));
            }

            return true;
        }

        private static FileInfo[] CacheFiles
            => new DirectoryInfo(CachePathname).GetFiles();

        private static string MakeResourceName(IResource resource, string name)
        {
            var resourceName = Sanitise(resource.Name);
            if (!string.IsNullOrEmpty(resourceName))
                resourceName = $"-{resourceName}";

            return $"{name}-{resource.GetType().Name}{resourceName}";
        }

        private static string Sanitise(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var ch in text)
            {
                if (char.IsLetterOrDigit(ch))
                    sb.Append(ch);
            }

            return sb.ToString();
        }

        private static string GetFileFromGuid(Guid metadataGuid)
            => Directory.GetFiles(CachePathname, $"{metadataGuid}*", SearchOption.TopDirectoryOnly).FirstOrDefault();

        private static long CalcCacheSize()
            => CacheFiles.Sum(file => file.Length);

        private static string CachePathname
            => Path.Combine(Application.persistentDataPath, "ResourceCache");

        private static string MakeLocalRootName(Guid id)
            => Path.Combine(CachePathname, $"{id.ToString()}");
    }
}

