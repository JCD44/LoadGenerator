using System;
using System.Collections.Generic;

namespace LoadGenerator
{
    /// <summary>
    /// Useful if you have a datasource of preloaded data, however this method is 
    /// much more limited than the dynamic method.  There is no test-level timeout.
    /// </summary>
    public class CachedDataLoadSettings<TestData> : ILoadSettings<TestData>
    {
        public Action<ILoadSettings<TestData>, ThreadSupportData, TestData> TestMethod { get; set; }
        public Func<ILoadSettings<TestData>, IEnumerable<TestData>> TestDataGenerator { get; set; }
        public int MaxSimulatedUsers { get; set; } = 4;
        /// <summary>
        /// This is the max time of the entire test, not the max time of any given test.
        /// </summary>
        public int MaxExecutionTimeInSeconds { get; set; }


    }
}
