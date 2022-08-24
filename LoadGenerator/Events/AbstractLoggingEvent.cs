using LoadGenerator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadGenerator.Events
{
    public abstract class AbstractLoggingEvent<TestData> : AbstractTimeBasedEvent<TestData>
    {
        public virtual string ResultToString(ILoadResults<TestData> results)
        {
            var ts = results.EndTime - results.StartTime;
            var resultCount = results.TotalResults;

            var failures = results.TotalFailures;
            var tps = Math.Round((resultCount - failures) / ts.TotalSeconds, 2);

            return $"Test has been running for {ts} @ {tps} transactions per second with {resultCount} results and {failures} failures.";
        }
        public override ILoadSettings<TestData> Execute(ILoadResults<TestData> result, ILoadSettings<TestData> settings)
        {
            Log(ResultToString(result));

            return settings;
        }

        public abstract void Log(String s);

    }
}
