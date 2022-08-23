using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LoadGenerator
{
    public interface ILoadTesting<TestData>
    {
        public LoadResults<TestData> Execute(ILoadSettings<TestData> settings);
    }
}
