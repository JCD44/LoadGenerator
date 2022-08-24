using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator.Results
{
    public interface ILoadResults<TestData>
    {
        ILoadSettings<TestData> Settings { get; set; }
        IEnumerable<ILoadResult<TestData>> Results { get; }
        public IEnumerable<ILoadResult<TestData>> Failures { get; }
        public void AddResult(ILoadResult<TestData> result);
        public DateTime StartTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public DateTime EndTime { get; }
        public int TotalFailures { get; }
        public int TotalSuccesses { get; }
        public int TotalResults { get; }

    }
}
