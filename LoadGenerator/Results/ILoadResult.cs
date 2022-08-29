using System;

namespace LoadGenerator.Results
{
    public interface ILoadResult<TestData>
    {
        bool Success { get; set; }
        DateTime StartTime { get; set; }
        TimeSpan ExecutionTime { get; set; }
        Exception ErrorResult { get; set; }
        TestData Input { get; set; }
        DateTime EndTime { get; }
    }
}
