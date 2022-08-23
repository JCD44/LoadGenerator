using LoadGenerator.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace LoadGenerator
{
    public class DynamicDataLoadSettings<TestData> : ILoadSettings<TestData>
    {
        public Action<ILoadSettings<TestData>, ThreadSupportData, TestData> TestMethod { get; set; }
        public Func<ILoadSettings<TestData>, TestData> TestDataGenerator { get; set; }
        public int MaxSimulatedUsers { get; set; } = 4;
        public int MaxMethodExecutions { get; set; }
        public int MaxExecutionTimeInSeconds { get; set; } = 60;
        public int MaxTestExecutionTimeInSeconds { get; set; } = 0;
        public List<IEvent<TestData>> Events { get; private set; } = new List<IEvent<TestData>>();
        /// <summary>
        /// Events are inspected on the main thread.  To keep total cost down, the ShouldExecute method
        /// will not be run at a frequency of x seconds where x is the value set here.
        /// </summary>
        public int EventFrequencyInSeconds { get; set; } = 60;
    }
}
