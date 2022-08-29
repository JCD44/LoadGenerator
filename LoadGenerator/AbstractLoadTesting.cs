using LoadGenerator.Results;
using System;
using System.Threading;

namespace LoadGenerator
{
    public abstract class AbstractLoadTesting<TestData> : ILoadTesting<TestData>
    {
        private static readonly bool WriteLog = false;
        /// <summary>
        /// How many additional threads does the threadpool need to allocate in order to support 
        /// whatever nuget packages you have that may also be using tasks.  By default we give 
        /// a 20% + a fixed count.  That means if your loadtest has 100 max simulated users with no events,
        /// we'd have a threadpool with 120 + fixed count.
        /// 
        /// Values are decimal based, so .2 means 20% while 1 is 100%.
        /// </summary>
        public double TaskOverheadPercentage { get; set; } = .20;
        /// <summary>
        /// A fixed additional overhead of threads for the thread pool.  This value is added after
        /// the percentage overhead is calculated.
        /// </summary>
        public int TaskOverheadFixed { get; set; } = 1;
        protected int PreviousMaxWorkerThreads { get; private set; }
        protected int PreviousMaxCompletionPortThreads { get; private set; }
        protected int PreviousMinWorkerThreads { get; private set; }
        protected int PreviousMinCompletionPortThreads { get; private set; }


        public ILoadResults<TestData> Execute(ILoadSettings<TestData> settings)
        {
            return Executor(settings);
        }

        protected abstract ILoadResults<TestData> InternalExecute(ILoadSettings<TestData> settings);

        protected virtual ILoadResults<TestData> Cleanup(ILoadSettings<TestData> settings, ILoadResults<TestData> results)
        {
            RevertPool();

            return Finish(results);
        }

        protected ILoadResults<TestData> Executor(ILoadSettings<TestData> settings)
        {
            Init(settings);

            var results = InternalExecute(settings);

            return Cleanup(settings, results);
        }

        protected virtual void Init(ILoadSettings<TestData> settings)
        {
            InitPool(settings);
        }
        /// <summary>
        /// Allows you to replace results with your own object.
        /// </summary>
        protected virtual ILoadResults<TestData> CreateResults(ILoadSettings<TestData> settings)
        {
            return new LoadResults<TestData>()
            {
                Settings = settings,
                StartTime = DateTime.Now,
                Status = ResultStatusEnum.Started,
            };
        }

        protected virtual ILoadResults<TestData> Finish(ILoadResults<TestData> results)
        {
            var end = DateTime.Now;

            results.Status = ResultStatusEnum.Completed;
            results.ExecutionTime = end - results.StartTime;

            return results;
        }
        /// <summary>
        /// Allows replacement of result with your own object.
        /// </summary>
        protected virtual ILoadResult<TestData> CreateResult(TestData data)
        {
            return new LoadResult<TestData>
            {
                StartTime = DateTime.Now,
                Input = data
            };
        }

        protected ILoadResult<TestData> RunSingleExecution(TestData data, ILoadSettings<TestData> settings, ThreadSupportData threadData)
        {
            var result = CreateResult(data);

            try
            {
                settings.TestMethod.Invoke(settings, threadData, data);
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorResult = ex;
            }

            var end = DateTime.Now;
            result.ExecutionTime = end - result.StartTime;

            return result;
        }


        protected virtual void RevertPool()
        {
            ThreadPool.SetMaxThreads(PreviousMaxWorkerThreads, PreviousMaxCompletionPortThreads);
            ThreadPool.SetMinThreads(PreviousMinWorkerThreads, PreviousMinCompletionPortThreads);
        }

        protected virtual void UpdatePool(ILoadSettings<TestData> settings)
        {
            //Give ourselves x% overhead, with at least y additional thread. 
            var workerThreads = (int)(settings.MaxSimulatedUsers * (1 + TaskOverheadPercentage)) + TaskOverheadFixed;
            ThreadPool.SetMaxThreads(workerThreads, PreviousMaxCompletionPortThreads);
            ThreadPool.SetMinThreads(workerThreads, PreviousMinCompletionPortThreads);

        }

        protected virtual void InitPool(ILoadSettings<TestData> settings)
        {
            int workerThreads, completionPortThreads;
            ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            PreviousMaxWorkerThreads = workerThreads;
            PreviousMaxCompletionPortThreads = completionPortThreads;

            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            PreviousMinWorkerThreads = workerThreads;
            PreviousMinCompletionPortThreads = completionPortThreads;

            UpdatePool(settings);
        }
        protected static void DebugLog(string s)
        {
            if (!WriteLog) return;
            Console.WriteLine($"ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")} - {s}");
        }



    }
}
