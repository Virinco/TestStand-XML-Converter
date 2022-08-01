using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using Virinco.WATS.Interface;

namespace TestStandXMLConverter
{
    [TestClass]
    public class ConverterTests : TDM
    {
        [TestMethod]
        public void SetupClient()
        {
            SetupAPI(null, "Location", "Purpose", true);
            RegisterClient("Insert WATS domain here", "", "Insert Token here");
            InitializeAPI(true);
        }

        [TestMethod]
        public void TestStandConverterTest()
        {
            InitializeAPI(true);
            string fn = @"Examples\TSWithWats.xml";
            TestStandXMLConverter converter = new TestStandXMLConverter();
            using (FileStream file = new FileStream(fn, FileMode.Open))
            {
                var tmp = converter.ImportReport(this, file);
                Submit(tmp);
            }
        }

        [TestMethod]
        public void TestStandConverterTest2()
        {
            InitializeAPI(true);
            string fn = @"Examples\TsWithoutWats.xml";
            TestStandXMLConverter converter = new TestStandXMLConverter();
            using (FileStream file = new FileStream(fn, FileMode.Open))
            {
                var tmp = converter.ImportReport(this, file);
                Submit(tmp);
            }
        }

        [TestMethod]
        public void TestStandConverterTest3()
        {
            InitializeAPI(true);
            string fn = @"Examples\TSWithWats2.xml";
            TestStandXMLConverter converter = new TestStandXMLConverter();
            using (FileStream file = new FileStream(fn, FileMode.Open))
            {
                var tmp = converter.ImportReport(this, file);
                Submit(tmp);
            }
        }

        [TestMethod]
        public void TestStandConverterTestMulti()
        {
            InitializeAPI(true);
            string[] files = new string[]
            {
                "TSWithWATS.xml", "TsWithoutWats.xml", "TSWithWats2.xml"
            };

            TestStandXMLConverter converter = new TestStandXMLConverter();
            foreach (string fn in files)
            {
                string f = Path.Combine(@"Examples\", fn);
                using (FileStream file = new FileStream(f, FileMode.Open))
                {
                    var tmp = converter.ImportReport(this, file);
                    Submit(tmp);
                }
            }
        }
    }
}
