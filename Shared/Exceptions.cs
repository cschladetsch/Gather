namespace Gather.Shared
{
    using System;

    public class UnknownResourceException
        : Exception
    {
        public UnknownResourceException(Guid id)
            : base($"Unknown Resource: {id}")
        {
        }
    }
}

