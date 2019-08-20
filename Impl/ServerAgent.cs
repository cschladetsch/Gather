// ReSharper disable ReplaceWithSingleAssignment.True

namespace Gather
{
    using System;
    using System.Collections;
    using UnityEngine.Networking;
    using Newtonsoft.Json;
    using Flow;
    using Flow.Impl;
    using Dekuple.Agent;
    using Resources;
    using Shared;
    using System.Collections.Generic;
    using App;

    /// <inheritdoc cref="IServerAgent" />
    public class ServerAgent
        : AgentBaseCoro<IServerModel>
        , IServerAgent
    {
        public string Token { get; set; }

        protected TimeSpan _DefaultTimeOut => Model.DefaultTimeOut;

        public ServerAgent(IServerModel model)
            : base(model)
        {
#if DEBUG
            //Verbosity = 10;
            //Verbosity = 100;
            const bool timesOut = false;
            Transient.TimeoutsEnabled = timesOut;
            if (!timesOut)
                Warn("Timeouts are disabled.");
#endif
        }

        public ITimedFuture<bool> GetHealth()
        {
            var result = New.TimedFuture<bool>(TimeSpan.FromSeconds(5));

            Get<HealthResult>("health").Then(health =>
            {
                if (!health.Succeeded())
                {
                    Warn($"Failed to connect to server at {Model.EndPoint}.");
                    result.Value = false;
                }
                else result.Value = health.Value.Typed.Health == "ok";
            });

            return result;
        }

        private struct Storage
        {
            public IResource Resource;
            public DateTime LastAccessed;
        }

        private readonly Dictionary<Guid, Storage> _storage = new Dictionary<Guid, Storage>();

        public ITimedFuture<Response<TResource>> GetResource<TResource>(Guid metadataGuid)
            where TResource : class, IResource, new()
        {
            var request = new Request();
            var result = New.TimedFuture<Response<TResource>>(Model.DefaultTimeOut).Then(res =>
            {
                if (res.Succeeded(out var val))
                {
                    _storage[val.ResourceId] = new Storage
                    {
                        Resource = val,
                        LastAccessed = DateTime.UtcNow
                    };
                }
            });

            if (_storage.TryGetValue(metadataGuid, out var stored))
            {
                if (DateTime.UtcNow - stored.LastAccessed < TimeSpan.FromMinutes(5))
                {
                    Verbose(10, $"Using cached resource metadata and value for {metadataGuid}");
                    result.Value = new Response<TResource>(request, stored.Resource as TResource);
                    return result;
                }
            }

            if (metadataGuid == Guid.Empty)
            {
                Warn("Won't get an empty Guid.");
                result.Value = FailedResponse<TResource>(request);
                return result;
            }

            IEnumerator Coro(IGenerator self)
            {
                var serverMeta = GetMetadata(metadataGuid);
                yield return self.ResumeAfter(serverMeta);

                var hasCachedMetadata = MetadataInCache(metadataGuid, out var cachedMeta);
                if (!serverMeta.Succeeded() && hasCachedMetadata)
                {
                    if (ResourceInCache<TResource>(cachedMeta.Value.Typed, out var res))
                    {
                        result.Value = new Response<TResource>(request, res);
                        yield break;
                    }

                    Verbose(10, $"Found metadata but no resource for {metadataGuid}, will attempt to download from server.");
                }
                else if (hasCachedMetadata)
                {
                    var metadata = cachedMeta.Value.Typed;
                    var cachedVersion = metadata.Version;
                    var downloadedVersion = serverMeta.Value.Typed.Version;

                    if (string.CompareOrdinal(cachedVersion, downloadedVersion) >= 0)
                    {
                        Verbose(10, $"Resource up-to-date: '{metadata.Name}' with id={metadataGuid}, version={cachedVersion} (remote={downloadedVersion}).");
                        if (ResourceInCache<TResource>(metadata, out var res))
                        {
                            result.Value = new Response<TResource>(request, res);
                            yield break;
                        }

                        result.Value = MakeResponse<TResource>(request, ERequestStatus.Failed);
                        yield break;
                    }

                    Verbose(10, $"Cached version is out of date. Downloading {metadata.Name} @{metadata.Guid}.");
                }

                var resource = new TResource();
                resource.Construct(serverMeta.Value.Typed, Kernel);
                var fetch = resource.Fetch();
                yield return self.ResumeAfter(fetch);

                if (!fetch.Available)
                {
                    result.Value = FailedResponse<TResource>(request);
                    yield break;
                }

                if (!fetch.Value)
                {
                    result.Value = MakeResponse<TResource>(request, resource.Result.Status);
                    yield break;
                }

                result.Value = resource.Convert()
                    ? new Response<TResource>(request, resource)
                    : FailedResponse<TResource>(request);

                resource.Id = metadataGuid;
                Model.AddResource(resource);
            }

            Run(Coro);

            return result;
        }

        public ITimedFuture<Response<T>> GetJsonResource<T>(Guid id)
        {
            var request = new Request();
            var result = New.TimedFuture<Response<T>>(Model.DefaultTimeOut);
            GetResource<StringResource>(id).Then(json =>
            {
                if (json.Succeeded())
                {
                    try
                    {
                        var settings = new JsonSerializerSettings { DateFormatString = "yy-MM-ddTHH-mm-ss" };
                        result.Value = new Response<T>(request,
                            JsonConvert.DeserializeObject<T>(json.Value.Typed.Value, settings));
                    }
                    catch (Exception e)
                    {
                        result.Value =
                            new Response<T>(new RequestResult(request, ERequestStatus.FailedToConvert, e.Message));
                    }
                }
                else
                {
                    result.Value = new Response<T>(new RequestResult(request, ERequestStatus.Failed));
                }
            });

            return result;
        }

        public ITimedFuture<Response<T>> Get<T>(string apiCall)
        {
            var request = new Request();
            var result = New.TimedFuture<Response<T>>(_DefaultTimeOut);
            var uri = Model.BuildServerApiCall(apiCall);

            Verbose(10, $"GET: {uri}, {request}");
            ProcessRequest(UnityWebRequest.Get(uri), request, result);

            return result;
        }

        public ITimedFuture<Response<TResult>> Post<TResult, TBody>(string apiCall, TBody body)
        {
            var req = new Request();
            var result = New.TimedFuture<Response<TResult>>(TimeSpan.FromSeconds(3));
            var uri = Model.BuildServerApiCall(apiCall);
            var json = JsonConvert.SerializeObject(body);

            Verbose(10, $"POST: {uri}, {req}");
            var post = UnityWebRequest.Put(uri, json);
            post.method = UnityWebRequest.kHttpVerbPOST;
            ProcessRequest(post, req, result);

            return result;
        }

        public TimeSpan GetTimeOut(int numBytes)
            => Model.DefaultTimeOut;

        private bool MetadataInCache(Guid id, out ITimedFuture<Response<ResourceMetadata>> futureMetadata)
        {
            if (Model.TryGetMetadata(id, out var metadata))
            {
                metadata.LastAccessTime = DateTime.UtcNow;
                futureMetadata = New.TimedFuture(_DefaultTimeOut, new Response<ResourceMetadata>(new Request(), metadata));
                return true;
            }

            futureMetadata = null;

            return false;
        }

        private bool ResourceInCache<TResource>(ResourceMetadata metadata, out TResource futureResource)
            where TResource : class, IResource, new()
        {
            if (Model.TryGetResource<TResource>(metadata, out var resource))
            {
                futureResource = resource as TResource;
                return true;
            }

            futureResource = null;

            return false;
        }

        private ITimedFuture<Response<ResourceMetadata>> GetMetadata(Guid id)
        {
            return Get<ResourceMetadata>(MakeResourceApiCall(id));
        }

        private void ProcessRequest<TResult>(UnityWebRequest request, Request req, ITimedFuture<Response<TResult>> result)
        {
            Verbose(5, $"Requesting {typeof(TResult).Name}, {req}");

            IEnumerator Coro(IGenerator self)
            {
                void SetError(long code, string text = "")
                {
                    Warn($"Error: {code}: {text}, for {req}.");
                    result.Value = new Response<TResult>(new RequestResult(req, ConvertToError(code), text));
                }

                using (request)
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Accept", "application/json");
                    if (!string.IsNullOrEmpty(Token))
                    {
                        request.SetRequestHeader("Authorization", $"Bearer {Token}");
                    }

                    request.SendWebRequest();
                    result.TimedOut += tr =>
                    {
                        SetError(408L, "Timed out");
                        request.Abort();
                        self.Complete();
                    };

                    yield return self.ResumeAfter(() => request.isDone);

                    if (request.isNetworkError || request.isHttpError)
                    {
                        SetError(request.responseCode, request.error);
                        yield break;
                    }

                    try
                    {
                        var settings = new JsonSerializerSettings { DateFormatString ="yy-MM-ddTHH-mm-ss" };
                        var res = JsonConvert.DeserializeObject<TResult>(request.downloadHandler.text, settings);
                        result.Value = new Response<TResult>(req, res);
                        Verbose(10, $"Request succeeded, for {req}");
                    }
                    catch (Exception e)
                    {
                        Error($"Caught error when deserialising:\nMessage: {e.Message}\nInner: {e.InnerException?.Message}");
                        SetError(500L, e.Message);
                    }
                }
            }

            Run(Coro);
        }

        public static ERequestStatus ConvertToError(long? result)
        {
            switch (result)
            {
                case 200:
                case 201:
                case 202:
                    return ERequestStatus.Success;
                case 408:
                    return ERequestStatus.TimedOut;
                case 404:
                    return ERequestStatus.NotFound;
                case 400:
                    return ERequestStatus.BadRequest;
                case 401:
                    return ERequestStatus.Unauthorised;
                case 500:
                    return ERequestStatus.InternalServerError;
                default:
                    break;
            }

            return ERequestStatus.Failed;
        }

        private static Response<TResource> FailedResponse<TResource>(Request request)
            => MakeResponse<TResource>(request, ERequestStatus.Failed);

        private static Response<TResource> TimedOutResponse<TResource>(Request request)
            => MakeResponse<TResource>(request, ERequestStatus.TimedOut);

        private static Response<T> MakeResponse<T>(Request request, ERequestStatus status)
            => new Response<T>(new RequestResult(request, status));

        private static string MakeResourceApiCall(Guid guid)
            => $"resource/guid/{guid}";
    }
}

