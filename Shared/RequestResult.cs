namespace Gather.Shared
{
    /// <summary>
    /// The result of a request made to a server.
    /// </summary>
    public class RequestResult
    {
        /// <summary>
        /// The original request that was made.
        /// </summary>
        public Request Request { get; set; }

        /// <summary>
        /// The returned status of the request.
        /// </summary>
        public ERequestStatus Status { get; }

        /// <summary>
        /// Supporting text to go with the status.
        /// </summary>
        public string Message { get; set; }

        public bool Failed => Status != ERequestStatus.Success;
        public bool Succeeded => !Failed;

        public RequestResult(Request req, ERequestStatus status = ERequestStatus.Success, string message = "")
        {
            Request = req;
            Status = status;
            Message = message;
        }

        public override string ToString()
            => $"Status={Status} Msg='{Message}'";
    }
}

