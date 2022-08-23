using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    public class LoadResult<TestData>
    {
        public bool Success { get; set; }
        public DateTime Start { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Exception Error { get; set; }
        public TestData Input { get; set; }
    }
}
