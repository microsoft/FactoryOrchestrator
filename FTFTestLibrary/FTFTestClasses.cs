using System;
using System.Collections.Generic;

namespace FTFTestLibrary
{
    public class FactoryTest
    {
        public FactoryTest()
        {
            IsTAEF = false;
        }

        // protected instance variables
        public DateTime LastTimeRun { get; set; }
        public bool? Result { get; set; }
        protected String LogFilePath { get; set; }
        public String TestPath;
        public String TestName;
        public bool IsTAEF { get; set; }
        public List<String> Arguments;
    }

    public class TAEFTest : FactoryTest
    {
        public TAEFTest()
        {
            IsTAEF = true;
        }

        private List<TAEFTestCase> _testCases;
        private String _wtlFilePath;
    }
    public class TAEFTestCase
    {
        public TAEFTestCase()
        {
        }

        private String _name;
        private bool? _result;
    }

    public class TestList
    {
        public List<FactoryTest> Tests;

        private bool? _result;
    }
}
