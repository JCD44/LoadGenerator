using LoadGenerator.Results;
using System;

namespace LoadGenerator.Events
{
    public abstract class AbstractLoggingEvent<TestData> : AbstractTimeBasedEvent<TestData>
    {
        public bool IncludeDetails { private get; set; } = true;
        public virtual string ResultToString(ILoadResults<TestData> results)
        {
            if (IncludeDetails) return results.CreateSummary().ToStringDetails();
            return results.CreateSummary().ToString();
        }
        public override ILoadSettings<TestData> Execute(ILoadResults<TestData> results, ILoadSettings<TestData> settings)
        {
            Log(ResultToString(results));

            return settings;
        }

        public abstract void Log(String s);

    }
}
