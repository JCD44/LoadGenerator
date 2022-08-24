using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator.Results
{
    public interface ILoadResult<TestData>
    {
        bool Success { get; set; }
        DateTime StartTime { get; set; }
        TimeSpan ExecutionTime { get; set; }
        Exception Error { get; set; }
        TestData Input { get; set; }
        DateTime EndTime { get; }
    }
}
