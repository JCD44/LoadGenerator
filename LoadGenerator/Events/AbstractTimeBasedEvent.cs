using LoadGenerator.Results;
using System;

namespace LoadGenerator.Events
{
    public abstract class AbstractTimeBasedEvent<TestData> : IEvent<TestData>
    {
        public TimeSpan TimeBetweenCalls { get; set; } = TimeSpan.MaxValue;
        private DateTime TimeLastExecutionRequested { get; set; } = DateTime.MinValue;

        public abstract ILoadSettings<TestData> Execute(ILoadResults<TestData> results, ILoadSettings<TestData> settings);

        public bool ShouldExecute(ILoadResults<TestData> results, ILoadSettings<TestData> settings)
        {
            var shouldExecute = (int)(results.EndTime - TimeLastExecutionRequested).TotalSeconds > TimeBetweenCalls.TotalSeconds;
            if (shouldExecute) TimeLastExecutionRequested = DateTime.Now;

            return shouldExecute;
        }

        public void Init(ILoadSettings<TestData> settings)
        {
            TimeLastExecutionRequested = DateTime.Now;
        }
    }
}
