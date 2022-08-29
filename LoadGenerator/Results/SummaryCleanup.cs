using System;

namespace LoadGenerator.Results
{
    internal class SummaryCleanup : ISummaryCleanup
    {
        public string CleanupErrorMessage(Exception error)
        {
            var message = error.Message;
            var type = $"({error.GetType().Name})";

            if (message == null) return "type";
            return $"{type} \"{message}\"";
        }
    }
}
