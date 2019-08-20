namespace Gather.Shared
{
    using System;

    /// <summary>
    /// Result of sending a request to the server
    /// </summary>
    public class Response
    {
        public Request Request
        {
            get => Result?.Request;
            protected set => Result.Request = value;
        }

        public RequestResult Result { get; set; }
        public DateTime Received { get; }
        public object Payload { get; set; }
        public bool Failed => Result.Failed;
        public bool Succeeded => Result.Succeeded;

        public Response()
        {
        }

        public Response(RequestResult result)
        {
            Received = DateTime.UtcNow;
            Result = result;
        }

        public override string ToString()
            => $"Request={Request}, Result={Result}, Succeeded={Succeeded}";
    }

    /// <inheritdoc />
    /// <summary>
    /// A response given a typed payload.
    /// </summary>
    public class Response<T>
        : Response
    {
        public T Typed { get; set; }

        public Response()
        {
        }

        public Response(RequestResult result)
            : base(result)
        {
        }

        public Response(Request req, T value)
            : this(new RequestResult(req))
        {
            Payload = Typed = value;
        }

        public void Construct(Request request, T value)
        {
            Request = request;
            Payload = Typed = value;
        }
    }
}

