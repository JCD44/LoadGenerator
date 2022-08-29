using LoadGenerator.Results;
using System;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public class CachedDataLoadTesting<TestData> : AbstractLoadTesting<TestData>
    {
        private ILoadResults<TestData> Execute(CachedDataLoadSettings<TestData> settings)
        {
            var results = CreateResults(settings);
            var data = settings.TestDataGenerator.Invoke(settings);
            var threadData = new ThreadSupportData(new System.Threading.CancellationTokenSource());
            if (settings.MaxExecutionTimeInSeconds > 0) threadData.Source.CancelAfter(settings.MaxExecutionTimeInSeconds * 1000);

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = settings.MaxSimulatedUsers,
                CancellationToken = threadData.Token
            };
            try
            {

                Parallel.ForEach(data, parallelOptions, data =>
                {
                    var result = RunSingleExecution(data, settings, threadData);
                    results.AddResult(result);
                });
            }
            catch (OperationCanceledException)
            {
                //Test timed everything out, so 
            }


            return results;
        }

        protected override ILoadResults<TestData> InternalExecute(ILoadSettings<TestData> settings)
        {
            return Execute((CachedDataLoadSettings<TestData>)settings);
        }
    }
}