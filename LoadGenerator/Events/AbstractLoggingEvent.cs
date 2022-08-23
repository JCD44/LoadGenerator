using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadGenerator.Events
{
    public abstract class AbstractLoggingEvent<TestData> : AbstractTimeBasedEvent<TestData>
    {

        public virtual string ResultToString(LoadResults<TestData> result)
        {
            var end = DateTime.Now;
            var ts = end - result.StartTime;
            var resultCount = result.TotalResults;

            var failures = result.FailureCount;
            var tps = (resultCount - failures) / ts.TotalSeconds;
            tps = Math.Round(tps, 2);


            return $"Test has been running for {ts} @ {tps} transactions per second with {resultCount} results and {failures} failures.";
        }
        public override ILoadSettings<TestData> Execute(LoadResults<TestData> result, ILoadSettings<TestData> settings)
        {
            Log(ResultToString(result));

            return settings;
        }

        public abstract void Log(String s);

    }
}
