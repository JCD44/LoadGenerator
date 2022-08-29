using System;
using System.Collections.Generic;

namespace LoadGenerator.Results
{
    public interface ISummaryData<TestData>
    {
        public int Passes { get; }
        public int Fails { get; }
        public int TotalResults { get; }
        public Dictionary<String, int> ErrorCountGroupedByMessage { get; }
        public double TransactionsPerSecond { get; }
        public TimeSpan RunTime { get; }
        public ResultStatusEnum Status { get; }
        string ToStringDetails();
        ISummaryData<TestData> CreateSummary(ILoadResults<TestData> results, ISummaryCleanup cleanup);

    }
}
