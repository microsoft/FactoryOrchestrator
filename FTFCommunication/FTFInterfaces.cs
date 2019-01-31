using System;
using FTFTestExecution;

namespace FTFInterfaces
{

    public interface IFTFCommunication
    {
        TestList EnumerateTests(string path, bool onlyTAEF);

        bool RunTestList(TestList listToRun, bool runInParallel);
    }

}
