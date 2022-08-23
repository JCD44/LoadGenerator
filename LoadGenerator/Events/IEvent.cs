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
        bool ShouldExecute(LoadResults<TestData> result, ILoadSettings<TestData> settings);
        ILoadSettings<TestData> Execute(LoadResults<TestData> result, ILoadSettings<TestData> settings);
        void Init(ILoadSettings<TestData> settings);
        /// <summary>
        /// If settings are updated it will run on the main thread, meaning it may impact performance.  Only set 
        /// this to true if "Execute" method is intended to update settings.
        /// </summary>
        bool MainThreadUpdateSettings { get; }
    }
 
}
