namespace Gather
{
    using System;
    using Dekuple.Agent;
    using Flow;
    using Shared;

    /// <inheritdoc />
    /// <summary>
    /// How to interact with a remote server.
    /// </summary>
    public interface IServerAgent
        : IAgent<IServerModel>
    {
        /// <summary>
        /// The bearer token used for Htpp requests after logging in.
        /// </summary>
        string Token { get; set; }

        /// <summary>
        /// Return true if the server is up and running and contactable.
        /// </summary>
        ITimedFuture<bool> GetHealth();

        /// <summary>
        /// Get a typed resource from a Guid.
        /// </summary>
        /// <typeparam name="TResource">The type of resource to get.</typeparam>
        /// <param name="id">The guid of the resource.</param>
        ITimedFuture<Response<TResource>> GetResource<TResource>(Guid id)
            where TResource : class, IResource, new();

        /// <summary>
        /// Make a GET Http request to the server Api.
        /// </summary>
        /// <typeparam name="TResult">The expected response type.</typeparam>
        /// <param name="apiCall">The tail of the Url- doesn't include the endpoint address or api level.</param>
        ITimedFuture<Response<TResult>> Get<TResult>(string apiCall);

        /// <summary>
        /// Make a POST Http request to the server Api.
        /// </summary>
        ITimedFuture<Response<TResult>> Post<TResult, TBody>(string api, TBody body);

        /// <summary>
        /// Get a structure described by Json.
        /// </summary>
        ITimedFuture<Response<TResult>> GetJsonResource<TResult>(Guid id);

        /// <summary>
        /// Get an estimated time out for fetching a number of bytes.
        /// </summary>
        TimeSpan GetTimeOut(int numBytes);
    }
}

