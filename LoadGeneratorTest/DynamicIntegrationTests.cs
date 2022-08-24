using LoadGenerator;
using LoadGenerator.Events;
using LoadGenerator.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGeneratorTest
{
    [TestClass]
    public class DynamicIntegrationTests
    {
        private static Random random = new Random();

        public void UpdateThreadName()
        {
            if (GlobalVariables.HasUpdatedThreadName) return;
            GlobalVariables.HasUpdatedThreadName = true;
            System.Threading.Thread.CurrentThread.Name = "Main Thread";

        }
        private static int counter = 0;
        private static TestData CreateData(ILoadSettings<TestData> settings)
        {
            return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++ };
        }

        private static TestData CreateDataLongWait(ILoadSettings<TestData> settings)
        {
            return new TestData() { Data = random.Next(1, 1000) + " - abc" + counter++, WaitTimeInMs = 5000 };
        }

        public static void RunTest(ILoadSettings<string> settings, string data)
        {
            Write(data);
            System.Threading.Thread.Sleep(1000);
        }
        private static void WaitWithToken(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            Write(data.Data);
            threadData.Task.Wait(data.WaitTimeInMs, threadData.Token);
        }
        private static void WaitWithoutToken(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            Write(data.Data);
            System.Threading.Thread.Sleep(data.WaitTimeInMs);
        }

        private static void ThrowError(ILoadSettings<TestData> settings, ThreadSupportData threadData, TestData data)
        {
            throw new Exception("Error thrown");

        }

        private class TestData
        {
            public int WaitTimeInMs { get; set; } = 2000;
            public int WaitTimeInSeconds { get { return WaitTimeInMs / 1000; } }
            public string Data { get; set; } = "abc";

            public override string ToString()
            {
                return Data;
            }
        }

        public static void Write(string s)
        {
            Console.WriteLine($"ID: {System.Threading.Thread.CurrentThread.ManagedThreadId}, {DateTime.Now.ToString("hh:mm:ss.fff")} - {s}");
        }

        private void LogResults(ILoadResults<TestData> results)
        {
            Write("Logging for Debug:");
            Write($"Total Time: {results.ExecutionTime}");
            foreach (var r in results.Results)
            {
                Write($"{r.Input} - {r.Success} - {r.StartTime} - {r.ExecutionTime} - {r?.Error?.Message}");
            }
        }

        private void BasicAsserts(DynamicDataLoadSettings<TestData> settings, ILoadResults<TestData> results)
        {
            Assert.IsTrue(settings.MaxMethodExecutions <= results.Results.Count(), "Requested number of executions equals results");
            foreach (var r in results.Results) 
            {
                var hasError = r.Error != null;
                Assert.AreEqual(hasError, !r.Success, "Success marked true when it doesn't have an exception");
            }
        }

        [TestMethod]
        public void TestsAreNotCancelWithWaitingTokenWhenTimeoutLonger()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {

                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateData,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 3,
            };
            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds < settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds} < {settings.MaxTestExecutionTimeInSeconds} - Execution time must be shorter than timeout.");
                Assert.IsTrue(r.Error==null, $"No Error: {r.Error}");
            }
            BasicAsserts(settings, results);
        }

        [TestMethod]
        public void SettingsCanBeAdjustedByEvents()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 1,
                MaxMethodExecutions = 50,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 10,
                EventFrequencyInSeconds = 0
            };

            var logging = new AdjustSettingsEvent<TestData>()
            {
                TimeBetweenCalls = TimeSpan.FromSeconds(1),
            };
            settings.Events.Add(logging);


            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            Assert.IsTrue(settings.MaxSimulatedUsers>=50, $"Settings was updated to be ~50 users from 1. Settings: {settings.MaxSimulatedUsers}");
            Assert.IsTrue(results.ExecutionTime.TotalSeconds <= 40, $"Total time taken should not be over 40 seconds (it was {results.ExecutionTime.TotalSeconds}) given the update in settings.");

            BasicAsserts(settings, results);
        }

        private class AdjustSettingsEvent<TestData> : AbstractTimeBasedEvent<TestData>, ISettingsUpdate
        {
            public override ILoadSettings<TestData> Execute(ILoadResults<TestData> results, ILoadSettings<TestData> settings)
            {
                var ts = results.EndTime - results.StartTime;

                var resultCount = results.TotalResults;
                var failures = results.TotalFailures;
                var tps = (resultCount - failures) / ts.TotalSeconds;

                if(tps<=5 && settings.MaxSimulatedUsers<50) settings.MaxSimulatedUsers += 50;
                Write("Updating settings");

                return settings;
            }
        }


        private class ConsoleLoggingEventCounter<TestData> : ConsoleLoggingEvent<TestData>
        {
            public int Counter { get; set; }
            public override void Log(string s)
            {
                Counter++;
                System.Console.WriteLine(s);
            }

        }

        [TestMethod]
        public void EventsAreExecutedCorrectNumberOfTimes()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 50,
                MaxMethodExecutions = 100,
                TestDataGenerator = CreateData,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 3,
                EventFrequencyInSeconds = 0
            };
            var logging = new ConsoleLoggingEventCounter<TestData>()
            {
                TimeBetweenCalls = TimeSpan.FromSeconds(1),
            };
            settings.Events.Add(logging);

            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            
            BasicAsserts(settings, results);
            Assert.IsTrue(logging.Counter > 0, $"{logging.Counter} > 0");

        }


        [TestMethod]
        public void ThrowErrorDuringLoad()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateData,
                TestMethod = ThrowError,
                MaxTestExecutionTimeInSeconds = 3,
            };

            var results = new DynamicDataLoadTesting<TestData>().Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.Error != null, "Error");
            }
            BasicAsserts(settings, results);

        }

        [TestMethod]
        public void OperationIsCancelledDueToTokenTimeout()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 10,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxTestExecutionTimeInSeconds = 1,
            };
            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxTestExecutionTimeInSeconds} - Execution time must be longer than settings");
                //Timeout shouldn't be too long.
                Assert.IsNotNull(r.Error);
                Assert.IsTrue(r.Error.Message.Contains("operation was canceled"), "Error shows operation was canceled");

            }
            BasicAsserts(settings, results);

        }

        [TestMethod]
        public void EndTestExecutionAFterExecutionTimeout()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 1,
                MaxMethodExecutions = 100,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithToken,
                MaxExecutionTimeInSeconds = 1,
            };
            var d = new DynamicDataLoadTesting<TestData>();
            var results = d.Execute(settings);

            LogResults(results);

            Write("Asserts:");

            foreach (var r in results.Results)
            {
                var hasError = r.Error != null;
                Assert.AreEqual(hasError, !r.Success, "Success marked true when it doesn't have an exception");
            }

            Assert.IsTrue(results.ExecutionTime.TotalSeconds < 15, "Timespan should be much less than 15 seconds as each test takes 5 seconds");
            Assert.IsTrue(results.Results.Count() <= 2, "No more than 2 results should be recorded.");


        }

        [TestMethod]
        public void PlaceholderResultsUsedWhenNoCancelTokenExists()
        {
            UpdateThreadName();
            var settings = new DynamicDataLoadSettings<TestData>
            {
                MaxSimulatedUsers = 4,
                MaxMethodExecutions = 4,
                TestDataGenerator = CreateDataLongWait,
                TestMethod = WaitWithoutToken,
                MaxTestExecutionTimeInSeconds = 1,
            };

            var results = new DynamicDataLoadTesting<TestData>().Execute(settings);

            LogResults(results);

            Write("Asserts:");
            foreach (var r in results.Results)
            {
                Assert.IsTrue(r.ExecutionTime.TotalSeconds + .01 >= settings.MaxTestExecutionTimeInSeconds, $"{r.ExecutionTime.TotalSeconds + .01} >= {settings.MaxTestExecutionTimeInSeconds} - Execution time must be longer than settings");
                Assert.IsTrue(r.Error.Message.Contains("Test timed out"), "Error shows test timed out.");

            }
            BasicAsserts(settings, results);

        }

    }
}
