using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator.Events
{
    public abstract class AbstractTimeBasedEvent<TestData> : IEvent<TestData>
    {
        public TimeSpan TimeBetweenCalls { get; set; } = TimeSpan.MaxValue;
        private DateTime TimeLastRun { get; set; } = DateTime.MinValue;
        public bool MainThreadUpdateSettings { get; private set; } = false;

        public abstract ILoadSettings<TestData> Execute(LoadResults<TestData> result, ILoadSettings<TestData> settings);

        public bool ShouldExecute(LoadResults<TestData> result, ILoadSettings<TestData> settings)
        {
           var end = DateTime.Now;
           var shouldExecute = (int)(end - TimeLastRun).TotalSeconds> TimeBetweenCalls.TotalSeconds;
            if (shouldExecute) TimeLastRun = DateTime.Now;

            return shouldExecute;
        }

        public void Init(ILoadSettings<TestData> settings)
        {
            TimeLastRun = DateTime.Now;
        }
    }
}
