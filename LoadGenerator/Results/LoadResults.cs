using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LoadGenerator.Results
{
    public class LoadResults<TestData> : ILoadResults<TestData>
    {
        public ResultStatus Status { get; set; } = ResultStatus.NotStarted;
        public ILoadSettings<TestData> Settings { get; set; }
        public IEnumerable<ILoadResult<TestData>> ResultDetails { get; private set; } = new Queue<ILoadResult<TestData>>();
        public IEnumerable<ILoadResult<TestData>> Failures { get { return ResultDetails.Where(a => !a.Success); } }
        public int TotalFailures { get { return ResultDetails.Count(a => !a.Success); } }
        public int TotalSuccesses { get { return ResultDetails.Count(a => a.Success); } }
        public int TotalResults { get { return ResultDetails.Count(); } }
        public void AddResult(ILoadResult<TestData> result)
        {
            //This is a weird way to do it but I didn't find a easier way to determine something is a "Queue"... This maybe something to look into.
            var isQueue = ResultDetails.GetType().GetMembers().Any(a => a.Name == "Enqueue");

            lock (ResultDetails)
            {
                if (isQueue)
                {
                    ((Queue<ILoadResult<TestData>>)ResultDetails).Enqueue(result);
                }
                else
                {
                    ((IList)ResultDetails).Add(result);
                }
            }
        }
        public DateTime StartTime { get; set; }
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.MinValue;
        public DateTime EndTime
        {
            get
            {
                var ts = ExecutionTime;
                if (ts == TimeSpan.MinValue) ts = DateTime.Now - StartTime;

                return StartTime.Add(ts);
            }
        }

        public ISummaryData<TestData> CreateSummary()
        {
            return new SummaryData<TestData>().CreateSummary(this, new SummaryCleanup());

        }
    }
}
