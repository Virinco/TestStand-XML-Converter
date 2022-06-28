using System;
using System.Collections.Generic;
using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        partial class XElementParser
        {
            /// <summary>
            /// Represents a TEResult element type. Inherits from XElementParser, and provides property based access to data belonging to stepnode.
            /// </summary>
            internal class TEResult : XElementParser
            {
                internal TEResult(XElement element)
                    : base(element)
                {
                }
                internal TEResult(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                }
                /// <summary>
                /// Returns true if SequenceCall node exists
                /// </summary>
                public bool isSequenceCall { get { return this.Exists("TS.SequenceCall"); } }
                /// <summary>
                /// Returns TS.Id as int. Returns -1 if non-exisiting.
                /// </summary>
                public int StepOrderNumber { get { return getIntValue("TS.Id", -1); } }
                /// <summary>
                /// Returns TS.Index as int. Returns -1 if non-exisiting.
                /// </summary>
                public int StepIndex { get { return getIntValue("TS.Index", -1); } }
                /// <summary>
                /// Returns TS.StepId. Returns string.Empty if non-exisiting. StepId is TestStand 'stepguid', always starts with 'ID#:' and a 22 character Base64 encoded value (16byte).
                /// </summary>
                public string StepId { get { return getStringValue("TS.StepId", string.Empty); } }
                /// <summary>
                /// Returns TS.StepId. Returns Guid.Empty if non-exisiting or unparsable. StepId is TestStand 'stepguid', always starts with 'ID#:' and a 22 character Base64 encoded value (16byte).
                /// </summary>
                public Guid StepIdAsGuid { get { return ParseTSGuidString(StepId); } }
                /// <summary>
                /// Returns StepGroup as string, should be one of the following: Setup, Main, Cleanup
                /// </summary>
                public string StepGroup { get { return getStringValue("TS.StepGroup", string.Empty); } }
                /// <summary>
                /// Returns StepType as string, may be TestStand standard values, WATS Standard values or customized steptypes.
                /// </summary>
                public string StepType { get { return getStringValue("TS.StepType", string.Empty); } }
                /// <summary>
                /// Returns StepName (defaults to designtime value, but might have been modified at runtime)
                /// </summary>
                public string StepName { get { return getStringValue("TS.StepName", string.Empty); } }
                /// <summary>
                /// Returns StepStatus as string, should be one of the following: Passed, Skipped, Failed, Error, Terminated.
                /// But might be set to anything during execution. Persistor should be able to handle any value.
                /// </summary>
                public string StepStatusText { get { return getStringValue("Status", string.Empty); } }
                /// <summary>
                /// Returns SequenceName (Name of Called Sequence), only applies to SequenceCalls (isSequenceCall=true)
                /// </summary>
                public string SequenceName { get { return getStringValue("TS.SequenceCall.Sequence", string.Empty); } }
                /// <summary>
                /// Returns SequenceFilePath (SequenceFile containing Sequence), only applies to SequenceCalls (isSequenceCall=true)
                /// </summary>
                public string SequenceFileName { get { return getStringValue("TS.SequenceCall.SequenceFile", string.Empty); } }
                /// <summary>
                /// Returns SequenceFile version 
                /// </summary>
                public string SequenceFileVersion { get { return getStringValue("TS.SequenceCall.SequenceFileVersion", null) ?? getStringValue("SeqFileVersion", string.Empty); } }
                /// <summary>
                /// Returns StartTime relative to Engine-start datetime (seconds after enginestart)
                /// </summary>
                public double StartTime { get { return getDoubleValue("TS.StartTime", 0); } }
                /// <summary>
                /// Returns ErrorCode
                /// </summary>
                public int? ErrorCode { get { return getIntValue("Error.Code"); } }
                /// <summary>
                /// Returns Error Message
                /// </summary>
                public string ErrorMessage { get { return getStringValue("Error.Msg", string.Empty); } }
                /// <summary>
                /// Returns StepTime in seconds
                /// </summary>
                public double StepTime { get { return getDoubleValue("TS.TotalTime", 0); } }
                /// <summary>
                /// Returns ModuleTime in seconds
                /// </summary>
                public double ModuleTime { get { return getDoubleValue("TS.ModuleTime", 0); } }
                /// <summary>
                /// Returns true if this step caused the Sequence to fail.
                /// </summary>
                public bool StepCausedSequenceFailure { get { return getBooleanValue("StepCausedSequenceFailure", false); } }
                /// <summary>
                /// Returns ReportText
                /// </summary>
                public string ReportText { get { return getStringValue("ReportText", string.Empty); } }
                /*
                public IEnumerable<XElement> SequenceCallChildren
                {
                    get { return GetChildren("TS.SequenceCall.ResultList"); }
                }
                public IEnumerable<XElement> PostActionChildren
                {
                    get { return GetChildren("TS.PostAction.ResultList"); }
                }
                */
                public IEnumerable<XElement> GetChildren(string path)
                {
                    XElementParser children = this[path];
                    if (children != null && children.Element != null)
                        return children.Element.Elements("Value");
                    else
                        return new List<XElement>();
                }

                public IEnumerable<XElement> Measurements
                {
                    get
                    {
                        XElementParser children = this["Measurement"];
                        if (children != null && children.Element != null)
                            return children.Element.Elements("Value");
                        else
                            return new List<XElement>();
                    }
                }
                public IEnumerable<XElement> AdditionalResults
                {
                    get
                    {
                        return getValues("AdditionalResults");
                    }
                }
                internal TestStandXMLConverter.XElementParser.Chart GetChartData()
                {
                    return new TestStandXMLConverter.XElementParser.Chart(this["Chart"].Element);
                }


            }
        }
    }
}
