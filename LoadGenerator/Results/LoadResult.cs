using System;

namespace LoadGenerator.Results
{
    public class LoadResult<TestData> : ILoadResult<TestData>
    {
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public Exception ErrorResult { get; set; }
        public TestData Input { get; set; }

        public DateTime EndTime
        {
            get
            {
                var ts = ExecutionTime;
                if (ts == TimeSpan.MinValue) ts = DateTime.Now - StartTime;

                return StartTime.Add(ts);
            }
        }

        public override string ToString()
        {
            return $"{Success}, {StartTime}, {EndTime}, {ExecutionTime}, {FixToCsv(ErrorResult?.Message)}, {FixToCsv(Input?.ToString())}";
        }

        private static string FixToCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return AddQuote(value);

            return AddQuote(value.Replace("\"", "\\\""));
        }

        private static string AddQuote(string value)
        {
            return "\"" + value + "\"";
        }
    }
}
