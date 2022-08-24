using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadGenerator.Results
{
    public class LoadResults<TestData> : ILoadResults<TestData>
    {
        public ILoadSettings<TestData> Settings { get; set; }
        public IEnumerable<ILoadResult<TestData>> Results { get; private set; } = new Queue<ILoadResult<TestData>>();
        public IEnumerable<ILoadResult<TestData>> Failures { get { return Results.Where(a => !a.Success); } }
        public int TotalFailures { get { return Results.Count(a => !a.Success);  } }
        public int TotalSuccesses { get { return Results.Count(a => a.Success);  } }
        public int TotalResults { get { return Results.Count();  } }
        public void AddResult(ILoadResult<TestData> result)
        {
            //This is a weird way to do it but I didn't find a easier way to determine something is a "Queue"... This maybe something to look into.
            var isQueue = Results.GetType().GetMembers().Any(a => a.Name == "Enqueue");

            lock (Results)
            {
                if (isQueue)
                {
                    ((Queue<ILoadResult<TestData>>)Results).Enqueue(result);
                }
                else
                {
                    ((IList)Results).Add(result);
                }
            }
        }
        public DateTime StartTime { get; set; }
        public TimeSpan ExecutionTime { get; set; } = TimeSpan.MinValue;
        public DateTime EndTime { 
            get 
            {
                var ts = ExecutionTime;
                if (ts == TimeSpan.MinValue) ts = DateTime.Now - StartTime;

                return StartTime.Add(ts);
            } 
        }
    }
}
