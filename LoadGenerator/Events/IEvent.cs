using LoadGenerator.Results;
using System;
using System.Threading;

namespace LoadGenerator.Events
{
    public interface IEvent<TestData>
    {
        /// <summary>
        /// This should be a very fast call as it can impact general performance, as it runs on the main thread 
        /// (to limit overhead)
        /// </summary>
        /// <returns></returns>
        bool ShouldExecute(ILoadResults<TestData> results, ILoadSettings<TestData> settings);
        ILoadSettings<TestData> Execute(ILoadResults<TestData> results, ILoadSettings<TestData> settings);
        /// <summary>
        /// Run once before load test is started.
        /// </summary>
        /// <param name="settings"></param>
        void Init(ILoadSettings<TestData> settings);

    }
 
}
