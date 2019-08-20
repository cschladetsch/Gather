namespace Gather.Shared
{
    using System;

    /// <summary>
    /// Base for all requests sent/received to/from any service.
    /// </summary>
    public class Request
    {
        /// <summary>
        /// The unique id for this request.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// When the request was created.
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// When the request was processed by the server.
        /// </summary>
        public DateTime Serviced { get; }

        private static int _next;

        public Request()
            : this(++_next)
        {
        }

        /// <summary>
        /// Make a request client-side to send to server.
        /// </summary>
        public Request(int id)
        {
            Id = id;
            Created = DateTime.UtcNow;
        }

        /// <summary>
        /// Make a request server-side, from a client-originated request, to send back to client.
        /// </summary>
        /// <param name="received">the original request received</param>
        public Request(Request received)
        {
            Id = received.Id;
            Created = received.Created;
            Serviced = DateTime.UtcNow;
        }

        public override string ToString()
            => $"#{Id}";
    }
}

