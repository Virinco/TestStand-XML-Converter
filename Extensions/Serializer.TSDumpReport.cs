using System;
using System.Linq;
using System.Xml.Linq;

namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        internal class TSDumpReport
        {
            private XElement _root;
            public TSDumpReport(XElement root)
            {
                _root = root;
                var guid = GetReportInfo("ID");
                if (guid != null)
                    ID = new Guid(guid);
                else
                    ID = new Guid();

                DateTimeOffset dt;
                DateTime es, rw;
                if (DateTimeOffset.TryParse(GetReportInfo("Start"), out dt)) Start = dt;
                if (DateTimeOffset.TryParse(GetReportInfo("StartUTC"), out dt)) StartUTC = dt;
                if (DateTime.TryParse(GetReportInfo("EngineStarted"), out es)) EngineStarted = es;
                if (DateTime.TryParse(GetReportInfo("ReportWritten"), out rw)) ReportWritten = rw;
            }

            public XElementParser MainResult
            {
                get
                {
                    var parser = new XElementParser(_root);

                    if (parser.Exists("MainSequenceResults"))
                        return new XElementParser(_root, "MainSequenceResults");
                    else
                    {
                        XElement xElement = _root.Element("Report").Elements("Prop").Where(el => el.Attribute("Type").Value == "TEResult").FirstOrDefault();
                        return new XElementParser(xElement);

                }
            }
            public XElementParser UUTInfo { get { return new XElementParser(_root, "UUT"); } }
            public XElementParser StationInfo { get { return new XElementParser(_root, "StationInfo"); } }
            internal Guid ID { get; private set; }//{get{return new Guid(GetReportInfo("ID"));}}
            internal DateTimeOffset Start { get; private set; }//{get{return DateTime.Parse(GetReportInfo("Start"));}}
            internal DateTimeOffset StartUTC { get; private set; }//{get{return DateTime.Parse(GetReportInfo("StartUTC"));}}
            internal DateTime EngineStarted { get; private set; }//{get{return DateTime.Parse(GetReportInfo("EngineStarted"));}}
            internal DateTime ReportWritten { get; private set; }//{get{return DateTime.Parse(GetReportInfo("ReportWritten"));}}

            internal XElementParser.TEResult RootResult
            {
                get
                {
                    XElementParser.TEResult result;
                    XElementParser mr = this.MainResult;
                    XAttribute atrType = mr.Element.Attribute("Type");
                    string sType;
                    XElement tmp = null;
                    //Need to check if it's an object. In which case, we need to look for the TEResult type to find the data we need. 
                    if (atrType.Value.ToLower().Contains("obj"))
                    {
                        //There's probably a better way to do this without all of the Where searches..
                        tmp = _root.Element("Report").Elements("Prop").Where(el => el.Attribute("Type").Value == "TEResult").FirstOrDefault();
                        tmp = tmp.Elements("Prop").Where(el => el.Attribute("Name").Value == "TS").FirstOrDefault();
                        tmp = tmp.Elements("Prop").Where(el => el.Attribute("Name").Value == "SequenceCall").FirstOrDefault();
                        tmp = tmp.Elements("Prop").Where(el => el.Attribute("Name").Value == "ResultList").FirstOrDefault();
                        sType = tmp.Attribute("Type").Value;
                    }
                    else
                        sType = atrType.Value; //Cannot use this due to different layout of xml. We would get type = obj instead of TEResult or Array.

                    switch (sType)
                    {
                        case "TEResult":
                            result = new XElementParser.TEResult(mr.Element);
                            break;
                        case "Array":
                            result = new XElementParser.TEResult(tmp.Elements("Value").First().Elements("Prop").Where(el => el.Attribute("Type").Value == "TEResult").FirstOrDefault());
                            break;
                        default:
                            result = null;
                            break;
                    }
                    return result;
                }
            }
            public string GetReportInfo(string key)
            {
                return (from t in _root.Elements("ReportInfo") where t.Attribute("key").Value == key select t.Attribute("value").Value).FirstOrDefault();
            }
            /*
              <ReportInfo key="ID" value="426b39f8-2b38-4157-8745-8c1a0b065920" type="System.Guid" />
              <ReportInfo key="Start" value="2010-08-06T10:59:46.5710000+02:00" type="System.DateTime" />
              <ReportInfo key="StartUTC" value="2010-08-06T08:59:46.5710000Z" type="System.DateTime" />
              <ReportInfo key="EngineStarted" value="2010-08-06T08:56:58.4780000Z" type="System.DateTime" />
              <ReportInfo key="ReportWritten" value="2010-08-06T10:59:55.7800661+02:00" type="System.DateTime" />
            */

        }
    }
}
