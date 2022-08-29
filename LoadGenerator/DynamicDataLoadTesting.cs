using LoadGenerator.Events;
using LoadGenerator.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public class DynamicDataLoadTesting<TestData> : AbstractLoadTesting<TestData>
    {
        private readonly List<TaskTracker> timedOutTasks = new List<TaskTracker>();
        protected DateTime LastEventExecution { get; set; } = DateTime.Now;
        private readonly List<TaskTracker> eventTasks = new List<TaskTracker>();

        protected bool ShouldRunEvents(DynamicDataLoadSettings<TestData> settings)
        {
            if (settings.Events.Count == 0) return false;
            if (LastEventExecution.AddSeconds(settings.EventFrequencyInSeconds) < DateTime.Now)
            {
                LastEventExecution = DateTime.Now;
                return true;
            }

            return false;
        }
        protected class TaskTracker
        {
            public Task Task { get; set; }
            public DateTime InitTime { get; set; }
            public DateTime StartTime { get; set; } = DateTime.MinValue;
            public int SecondsRun()
            {
                if (StartTime == DateTime.MinValue) return 0;

                var end = DateTime.Now;
                return (int)(end - StartTime).TotalSeconds;
            }
            public ThreadSupportData ThreadData { get; set; }
            public TestData Data { get; set; }
            //Only used for events.
            public IEvent<TestData> Event { get; set; }

            public void Dispose()
            {
                if (Task.IsCompleted || Task.IsFaulted || Task.IsCanceled) Task.Dispose();
                ThreadData?.Dispose();
            }
        }

        private static void DisposeTasks(List<TaskTracker> list)
        {
            foreach (var item in list)
            {
                try
                {
                    item.Dispose();
                }
                catch (Exception ex)
                {
                    //Don't know what will cause this to happen at present... TODO needs to be logged.
                }
            }
        }

        private static void ProcessCompletedTasks(List<TaskTracker> tasks)
        {
            List<TaskTracker> tasksToDispose = tasks.Where(a => a.Task.IsCompleted || a.Task.IsCompletedSuccessfully || a.Task.IsCanceled).ToList();

            if (tasksToDispose.Count > 0)
            {
                //Unsafe operation... so let's do it safely
                lock (tasks)
                {
                    tasks.RemoveAll(a => tasksToDispose.Contains(a));
                }

                DisposeTasks(tasksToDispose);

            }
        }

        private void NotifyAndRemoveTimedOutTasks(List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings)
        {
            List<TaskTracker> tasksToDispose = tasks.Where(a => a.SecondsRun() > settings.MaxTestExecutionTimeInSeconds).ToList();

            if (tasksToDispose.Count > 0)
            {
                foreach (var item in tasksToDispose)
                {
                    item.ThreadData.Source.Cancel();
                }
                //Unsafe operation... so let's do it safely
                lock (tasks)
                {
                    tasks.RemoveAll(a => tasksToDispose.Contains(a));
                    timedOutTasks.AddRange(tasksToDispose);
                }
            }

            ProcessCompletedTasks(timedOutTasks);
        }

        private void RemoveCompletedTasks(List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings, bool force = false)
        {
            if (force || tasks.Count >= settings.MaxSimulatedUsers)
            {

                ProcessCompletedTasks(tasks);
                NotifyAndRemoveTimedOutTasks(tasks, settings);
                ProcessCompletedEvents();
            }
        }

        private void ProcessCompletedEvents()
        {
            ProcessCompletedTasks(eventTasks);
        }

        protected bool IsDone(ILoadResults<TestData> results, List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings)
        {
            if (HasExecutedMaxResults(results, tasks, settings)) return true;

            return HasExecutedMaxTime(results, tasks, settings);
        }

        protected bool HasExecutedMaxTime(ILoadResults<TestData> results, List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings)
        {
            //Never execute past max time if Max is set to 0 or less.
            if (settings.MaxExecutionTimeInSeconds <= 0) return false;
            return results.StartTime.AddSeconds(settings.MaxExecutionTimeInSeconds) <= DateTime.Now;
        }

        protected bool HasExecutedMaxResults(ILoadResults<TestData> results, List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings)
        {
            var result = results.Results.Count() + timedOutTasks.Count + tasks.Count >= settings.MaxMethodExecutions;
            lock (tasks)
            {
                if (result)
                {
                    RemoveCompletedTasks(tasks, settings, true);
                }

                return results.Results.Count() + timedOutTasks.Count + tasks.Count >= settings.MaxMethodExecutions;
            }

        }

        private static void Write(string s)
        {
            DebugLog(s);
        }
        private static int counter;
        protected void CreateNewTaskIfOrElseWait(List<TaskTracker> tasks, DynamicDataLoadSettings<TestData> settings, ILoadResults<TestData> results)
        {
            if (tasks.Count < settings.MaxSimulatedUsers)
            {
                var localId = counter++;
                var data = settings.TestDataGenerator.Invoke(settings);
                var threadData = CreateSupportData(settings);
                var task = new TaskTracker()
                {
                    InitTime = DateTime.Now,
                    ThreadData = threadData,
                    Data = data,
                };
                Action action = () =>
                                   {

                                       while (threadData.Task == null) { Thread.Sleep(1); }
                                       //Until now, it's not really "Started"
                                       task.StartTime = DateTime.Now;
                                       Write($"Starting task @ {task.StartTime} {localId}");


                                       threadData.Token.Register(() => { Write($"Cancel requested {localId}"); });
                                       var result = RunSingleExecution(data, settings, threadData);
                                       results.AddResult(result);
                                   };
                Write($"About to create a task {localId}");
                task.Task = Task.Run(action, threadData.Token);

                threadData.Task = task.Task;

                tasks.Add(task);
            }
            else
            {
                settings = RunEvents(settings, results);
                results.Settings = settings;

                Thread.Sleep(1);
            }

        }

        protected virtual DynamicDataLoadSettings<TestData> RunEvents(DynamicDataLoadSettings<TestData> settings, ILoadResults<TestData> results, bool forceExecution = false)
        {
            if (ShouldRunEvents(settings))
            {
                ProcessCompletedEvents();

                foreach (var e in settings.Events)
                {
                    if (forceExecution || e.ShouldExecute(results, settings))
                    {
                        if (e is ISettingsUpdate)
                        {
                            settings = (DynamicDataLoadSettings<TestData>)e.Execute(results, settings);
                            UpdatePool(settings);
                        }
                        else
                        {
                            if (!eventTasks.Any(a => a.Event == e))
                            {
                                eventTasks.Add(new TaskTracker()
                                {
                                    Task = Task.Run(() =>
                                                        {
                                                            e.Execute(results, settings);
                                                        }),
                                    InitTime = DateTime.Now,
                                    Event = e,

                                });
                            }
                            else
                            {
                                Write("Skipping event");
                            }

                        }
                    }

                }
            }
            return settings;
        }

        protected void InitEvents(DynamicDataLoadSettings<TestData> settings)
        {
            LastEventExecution = DateTime.Now;

            foreach (var e in settings.Events)
            {
                e.Init(settings);
            }
        }

        private ILoadResults<TestData> Execute(DynamicDataLoadSettings<TestData> settings)
        {
            var results = CreateResults(settings);
            var tasks = new List<TaskTracker>();
            InitEvents(settings);

            while (!IsDone(results, tasks, settings))
            {
                RemoveCompletedTasks(tasks, settings);
                CreateNewTaskIfOrElseWait(tasks, settings, results);
            }

            while (tasks.Count > 0)
            {
                RemoveCompletedTasks(tasks, settings, true);
                RunEvents(settings, results);
                Thread.Sleep(1);
            }

            RunEvents(settings, results, true);
            Thread.Sleep(100);
            ProcessCompletedTasks(timedOutTasks);
            foreach (var item in timedOutTasks)
            {
                var startResult = CreateResult(item.Data);
                startResult.ExecutionTime = TimeSpan.FromSeconds(settings.MaxTestExecutionTimeInSeconds);
                startResult.ErrorResult = new Exception("Test timed out, results maybe asynchronusly added after this point");
                startResult.Success = false;

                results.AddResult(startResult);
            }

            if (!IsDone(results, tasks, settings))
            {
                IsDone(results, tasks, settings);
            }

            return results;
        }
        /// <summary>
        /// Every time this is called, the CTS must be managed and dispose must be called properly.
        /// </summary>
        protected ThreadSupportData CreateSupportData(ILoadSettings<TestData> settings)
        {
            var cts = new CancellationTokenSource();
            //Warning: This timeout is based upon the queueing of the task, not the task starting up.
            //After spending lots of time using this API, I concluded I needed to roll my own.
            //if (settings.TimeoutInSeconds > 0) cts.CancelAfter(settings.TimeoutInSeconds * 1000);

            return new ThreadSupportData(cts);
        }

        protected override ILoadResults<TestData> InternalExecute(ILoadSettings<TestData> settings)
        {
            return Execute((DynamicDataLoadSettings<TestData>)settings);
        }
    }
}
