using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LoadGenerator
{
    public class LoadResults<TestData>
    {
        public ILoadSettings<TestData> Settings { get; set; }
        public IEnumerable<LoadResult<TestData>> Results { get; private set; } = new Queue<LoadResult<TestData>>();
        public IEnumerable<LoadResult<TestData>> Failures { get { return Results.Where(a => !a.Success); } }
        public int FailureCount { get { return Results.Count(a => !a.Success);  } }
        public int TotalResults { get { return Results.Count();  } }
        public void AddResult(LoadResult<TestData> result)
        {
            lock (Results)
            {
                //This is a weird way to do it but I didn't find a easier way to determine something is a "Queue"... This maybe something to look into.
                if (Results.GetType().GetMembers().Any(a=>a.Name== "Enqueue"))
                {
                    ((Queue<LoadResult<TestData>>)Results).Enqueue(result);
                }
                else
                {
                    ((IList)Results).Add(result);
                }
            }
        }
        public DateTime StartTime { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }
}
