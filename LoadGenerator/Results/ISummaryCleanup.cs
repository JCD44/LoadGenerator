using System;

namespace LoadGenerator.Results
{
    public interface ISummaryCleanup
    {
        public string CleanupErrorMessage(Exception exception);
    }
}
