namespace Gather.Shared
{
    /// <summary>
    /// Possible return status for remote requests.
    /// </summary>
    public enum ERequestStatus
    {
        NotFound,
        TimedOut,
        BadRequest,
        Unauthorised,
        Success,
        UnknownResourceId,
        UnknownResourceUri,
        Failed,
        DuplicateId,
        FailedToConvert,
        NetworkError,
        InternalServerError
    }
}

