using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadGenerator.Results
{
    public class SummaryData<TestData> : ISummaryData<TestData>
    {
        public int Passes { get; set; } = 0;
        public int Fails { get; set; } = 0;
        public int TotalResults { get; set; } = 0;
        public Dictionary<string, int> ErrorCountGroupedByMessage { get; set; } = new Dictionary<string, int>();
        public double TransactionsPerSecond { get; set; } = 0;
        public TimeSpan RunTime { get; set; } = TimeSpan.MinValue;
        public ResultStatusEnum Status { get; set; } = ResultStatusEnum.NotStarted;

        public ISummaryData<TestData> CreateSummary(ILoadResults<TestData> results, ISummaryCleanup cleanup)
        {
            var ResultDetails = results.Results;
            var list = new List<ILoadResult<TestData>>();
            lock (ResultDetails) { list.AddRange(ResultDetails); }

            Fails = list.Count(a => !a.Success);
            Passes = list.Count(a => a.Success);
            TotalResults = list.Count;

            var failures = list.Count(a => !a.Success);
            RunTime = results.EndTime - results.StartTime;
            TransactionsPerSecond = Math.Round((TotalResults - Fails) / RunTime.TotalSeconds, 2);
            Status = results.Status;

            foreach (var result in list.Where(a => a.ErrorResult != null))
            {
                var message = cleanup.CleanupErrorMessage(result.ErrorResult);
                if (ErrorCountGroupedByMessage.ContainsKey(message))
                {
                    ErrorCountGroupedByMessage[message]++;
                }
                else
                {
                    ErrorCountGroupedByMessage.Add(message, 1);
                }

            }

            return this;

        }

        public override string ToString()
        {
            return $"Passes: {Passes} Fails: {Fails} Total: {TotalResults} - TPS: {TransactionsPerSecond} over {RunTime}";
        }

        public string ToStringDetails()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{ToString()}.");
            sb.AppendLine("Error Details:");
            foreach (var item in ErrorCountGroupedByMessage)
            {
                sb.AppendLine($"    * {item.Key} occurred {item.Value} times");
            }

            return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }
    }
}
