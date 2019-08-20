namespace Gather.Impl
{
    using System;
    using System.Collections;

    using UnityEngine.Networking;

    using Flow;
    using Dekuple;

    using Shared;

    /// <inheritdoc cref="ResourceBase"/>
    /// <summary>
    /// A resource stored on the cloud, or cached locally.
    /// </summary>
    public abstract class Resource
        : ResourceBase
    {
        public override byte[] Bytes { get; set; }

        protected DownloadHandler _Download { get; private set; }
        protected IKernel _Kernel;
        protected IFactory _Factory => _Kernel?.Factory;
        protected IFactory _New => _Factory;
        protected INode _Root => _Kernel?.Root;

        private readonly TimeSpan _timeOut = TimeSpan.FromSeconds(200);
        private readonly TimeSpan _multiPartWait = TimeSpan.FromMilliseconds(200);

        protected Resource() { }

        protected Resource(string name)
            => Name = name;

        public override bool Construct(ResourceMetadata md, IKernel kernel)
        {
            _Kernel = kernel;
            _Metadata = md;
            return true;
        }

        public override bool Construct(ResourceMetadata meta, byte[] bytes)
        {
            _Metadata = meta;
            return FromBytes(bytes);
        }

        /// <summary>
        /// Get the raw data for the resource from cloud.
        /// </summary>
        /// <returns>A future response.</returns>
        protected virtual ITimedFuture<Response<T>> FetchData<T>()
        {
            Assert.IsNotNull(_Factory);

            var request = new Request();
            var fetch = _Factory.TimedFuture<Response<T>>(_timeOut);

            IEnumerator Coro(IGenerator self)
            {
                UnityWebRequest webRequest = null;
                void Failed(string msg = "")
                {
                    if (!string.IsNullOrEmpty(msg))
                        Error(msg);

                    Fail(request, webRequest, fetch, ServerAgent.ConvertToError(webRequest?.responseCode));
                }

                try
                {
                    webRequest = UnityWebRequest.Get(Uri);
                    webRequest.downloadHandler = NewDownloadHandler();

                    if (webRequest.isNetworkError)
                    {
                        Failed(webRequest.error);
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    Failed($"----> {Uri}: {e.Message}");
                    yield break;
                }

                var asyncOp = webRequest.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    yield return self.ResumeAfter(_multiPartWait);
                    if (!webRequest.isNetworkError)
                        continue;

                    Failed();
                    yield break;
                }

                if (webRequest.isHttpError)
                {
                    Failed();
                    yield break;
                }

                _Download = webRequest.downloadHandler;
                fetch.Value = new Response<T> { Result = new RequestResult(request) };
            }

            _Root.Add(_Factory.Coroutine(Coro));
            return fetch;
        }

        protected void Fail<T>(Request request, UnityWebRequest webRequest, IFuture<Response<T>> futureStatus, ERequestStatus status)
        {
            Error($"WebRequest failed for {Uri}: {webRequest?.error}");
            Result = new RequestResult(request, status, webRequest?.error);
            futureStatus.Value = new Response<T>(Result);
        }

        protected new void Error(string fmt, params object[] args)
            => _Kernel?.Log.Error(fmt, args);
    }

    /// <inheritdoc cref="Resource"/>
    public abstract class Resource<T>
        : Resource
    {
        // Change this to not be publicly settable;
        public new T Value
        {
            get => (T)base.Value;
            set => base.Value = value;
        }

        public static Type Type => typeof(T);

        protected Resource()
        {
        }

        protected Resource(string name)
            : base(name)
        {
        }

        /// <inheritdoc />
        /// <summary>
        /// Fetch and convert data for resource.
        /// </summary>
        /// <returns>A future status.</returns>
        public override IFuture<bool> Fetch()
        {
            var req = new Request();
            var data = FetchData<T>();
            var fetch = _New.TimedFuture<bool>(TimeSpan.FromSeconds(300));

            void SetResult(ERequestStatus status, string text = "")
                => Result = new RequestResult(req, status, text);

            SetResult(ERequestStatus.Failed);

            IEnumerator FetchAndConvertCoro(IGenerator self)
            {
                yield return self.ResumeAfter(data);
                if (!data.Available || data.HasTimedOut)
                {
                    Warn($"Timed out fetching {Uri}.");
                    SetResult(ERequestStatus.TimedOut);
                    fetch.Value = false;
                    yield break;
                }

                if (_Download?.data == null)
                {
                    Warn($"No data fetching {Uri}.");
                    fetch.Value = false;
                    yield break;
                }

                if (Convert())
                {
                    fetch.Value = true;
                    SetResult(ERequestStatus.Success);
                    yield break;
                }

                var error = $"Failed to convert {this} to type {typeof(T).Name}.";
                Error(error);
                SetResult(ERequestStatus.FailedToConvert, error);
                fetch.Value = false;
            }

            _Root.Add(fetch, _Factory.Coroutine(FetchAndConvertCoro).Named("FetchAndConvert"));
            return fetch;
        }
    }
}

