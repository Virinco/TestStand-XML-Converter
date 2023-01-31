using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Virinco.WATS.Interface;
using Virinco.WATS.Schemas.WRML;

namespace TestStandXMLConverter
{
    public partial class TestStandXMLConverter : IReportConverter_v2
    {

        #region ISerializer Members

        public TestStandXMLConverter()
        {
            parameters = new Dictionary<string, string>()
            {
                {"operationTypeCode","10" },
                {"partRevision","1.0" },
                {"location", "" },
                {"purpose", "" }
            };
        }

        public TestStandXMLConverter(Dictionary<string, string> args)
        {
            parameters = args;
        }
        
        public List<FileInfo> DeleteFiles = new List<FileInfo>();

        private Dictionary<string, string> parameters;

        public Dictionary<string, string> ConverterParameters => parameters;

        #endregion

        #region Interface.IReportConverter Members

        public Report ImportReport(TDM api, Stream file)
        {
            using (XmlReader reader = XmlReader.Create(file))
            {
                while (reader.Read())
                {
                    if (reader.Name == "TSReport")
                        return ImportReport(api, reader);
                    else if (reader.Name == "Reports")
                        return ImportReport(api, reader);
                }

                throw new InvalidDataException("TSReport or Reports element was not found.");

                //if (reader.ReadToDescendant("TSReport"))
                //    return ImportReport(api, reader);
                //else 
                //if (reader.ReadToDescendant("Reports"))
                //    return ImportReport(api, reader);
                //else
                //    throw new System.IO.InvalidDataException("TSReport element was not found.");
            }
        }

        public void CleanUp()
        {
            foreach (FileInfo fi in DeleteFiles) 
                fi.Delete();
        }

        #endregion

        #region Interface.IReportConverter Helper Methods

        private Report ImportReport(TDM api, XmlReader reader)
        {
            if (reader.Name != "TSReport" && reader.Name != "Reports") 
                throw new ArgumentException("Xml reader must be open and positioned on TSReport element");

            return CreateUUT(api, XElement.Parse(reader.ReadInnerXml()));
        }

        #endregion

        private UUTReport CreateUUT(TDM api, XElement xmlReport)
        {
            api.TestMode = TestModeType.TestStand;
            api.ValidationMode = ValidationModeType.AutoTruncate;
            TSDumpReport dump = new TSDumpReport(xmlReport);
            XElementParser.TEResult xpRoot = dump.RootResult;
            XElementParser xpTS = xpRoot["TS"];

            //If they don't exist, we'll check the xml later for the information we need. 
            XElementParser xpStationInfo = dump.StationInfo;
            var stationInfoExists = xpStationInfo.Element != null;

            string @operator = stationInfoExists ? xpStationInfo.getStringValue("LoginName") : string.Empty;
            string stationId = stationInfoExists ? xpStationInfo.getStringValue("StationID") : string.Empty;
            string location = stationInfoExists ? xpStationInfo.getStringValue("Location") : parameters.ContainsKey("location") ? parameters["location"] : string.Empty;
            string purpose = stationInfoExists ? xpStationInfo.getStringValue("Purpose") : parameters.ContainsKey("purpose") ? parameters["purpose"] : string.Empty;
                         
            TSUUTReport uut = new TSUUTReport(api, true, dump.EngineStarted, @operator, xpRoot.SequenceFileName, xpRoot.SequenceFileVersion, true);
            uut.SetStationInfo(stationId, location, purpose);

            if (stationInfoExists)
            {
                var additionalStationInfos = xpStationInfo["AdditionalData"]?.Element?.Elements("Prop");
                if (additionalStationInfos != null)
                {
                    var first = additionalStationInfos.FirstOrDefault();
                    if (first != null)
                    {
                        var stationInfos = uut.AddAdditionalStationInfo(first);
                        var rest = additionalStationInfos.Skip(1);
                        foreach (var additionalStationInfo in rest)
                            //Adds the contents to the Any list...
                            stationInfos.Contents = additionalStationInfo;
                    }
                }
            }

            if (dump.ID.HasValue && dump.ID.Value != Guid.Empty)
                uut.SetReportId(dump.ID.Value);
            else
                uut.SetReportId(Guid.NewGuid());

            //Use xpStationExists so we know if we have this data in the file or not. 
            if (dump.Start.HasValue)
                uut.StartDateTimeOffset = dump.Start.Value;
            else
            {
                var timeDetails = dump.TimeDetails;
                var dateDetails = dump.DateDetails;

                int? milliseconds = timeDetails.getIntValue("Milliseconds");
                int? seconds = timeDetails.getIntValue("Seconds");
                int? minutes = timeDetails.getIntValue("Minutes");
                int? hours = timeDetails.getIntValue("Hours");
                
                int? day = dateDetails.getIntValue("MonthDay");
                int? month = dateDetails.getIntValue("Month");
                int? year = dateDetails.getIntValue("Year");

                if (!year.HasValue || !month.HasValue || !day.HasValue || !hours.HasValue || !minutes.HasValue || !seconds.HasValue || !milliseconds.HasValue)
                    throw new ArgumentNullException("Missing datetime part");
              
                uut.StartDateTimeOffset = new DateTimeOffset(year.Value, month.Value, day.Value, hours.Value, minutes.Value, seconds.Value, milliseconds.Value, DateTimeOffset.Now.Offset);
            }
                

            //Need this so we can get all elements manually if xpUUT is null or unset. 
            XElementParser xpUut = dump.UUTInfo;

            uut.PartNumber = xpUut.getStringValue("UUTPartNumber");
            if (uut.PartNumber == string.Empty)
                uut.PartNumber = xpUut.getStringValue("PartNumber");

            uut.PartRevisionNumber = xpUut.getStringValue("UUTPartRevisionNumber");
            if (uut.PartRevisionNumber == string.Empty)
                uut.PartRevisionNumber = parameters["partRevision"];

            string operationType = xpUut.getStringValue("UUTOperationType", null);
            if (operationType == null)
                operationType = parameters["operationTypeCode"];
            
            uut.SerialNumber = xpUut.getStringValue("SerialNumber");
            uut.OperationType = api.GetOperationType(operationType);
            uut.Comment = xpUut.getStringValue("Comment");
            uut.FixtureId = xpUut.getStringValue("UUT_Fixture_ID");
            uut.BatchSerialNumber = xpUut.getStringValue("BatchSerialNumber");
            uut.TestSocketIndex = (short)xpUut.getIntValue("TestSocketIndex", 0);
            uut.BatchLoopIndex = xpUut.getIntValue("UUTLoopIndex", 0); 

            uut.ExecutionTime = xpTS.getDoubleValue("TotalTime", 0);
            uut.SetStatus(xpRoot.getStringValue("Status"));
            uut.ErrorCode = xpRoot.getIntValue("Error.Code", 0);
            uut.ErrorMessage = xpRoot.getStringValue("Error.Msg");

            var additionalDataProps = xpUut["AdditionalData"]?.Element?.Elements("Prop"); 
            if (additionalDataProps != null)
            {
                foreach (var additionalDataProp in additionalDataProps)
                {
                    var xpAdditionalData = new XElementParser(additionalDataProp);
                    uut.AddAdditionalData(xpAdditionalData.Name, xpAdditionalData.Element);
                }
            }

            XElementParser xpUutAdditional = xpUut["MiscUUTResult"];
            if (xpUutAdditional != null)
            {
                XElementParser xpUutMiscInfo = xpUutAdditional["Misc_UUT_Info"];
                if (xpUutMiscInfo != null)
                {
                    foreach (XElement el in xpUutMiscInfo.getValues())
                    {
                        XElementParser.MiscInfo xp = XElementParser.Create(el) as XElementParser.MiscInfo;
                        uut.AddMiscUUTInfo(xp);
                    }
                }

                XElementParser xpUutPartInfo = xpUutAdditional["UUT_Part_Info"];
                if (xpUutPartInfo != null)
                {
                    foreach (XElement el in xpUutPartInfo.getValues())
                    {
                        XElementParser.PartInfo xp = XElementParser.Create(el) as XElementParser.PartInfo;
                        uut.AddUUTPartInfo(xp);
                    }
                }
            }
           
            SetSequenceStepData(uut, xpRoot, uut.RootStepRow);
            AddSteps(uut, uut.RootStepRow, xpRoot.GetChildren("TS.SequenceCall.ResultList"));
             
            return uut;
        }

        private void AddSteps(TSUUTReport uut, Step_type parentRow, IEnumerable<XElement> steps)
        {
            foreach (XElement el in steps)
            {
                XElementParser.TEResult step = new XElementParser.TEResult(
                    (from te in el.Elements("Prop") where te.Attribute("Type").Value == "TEResult" select te).First()
                    );

                Step_type stepRow = uut.AddStep(parentRow, step.StepName, step.StepIndex);
                StepResultType status = SetSequenceStepData(uut, step, stepRow);
                if (status == StepResultType.Skipped) continue; // Skip measurement logging if status==skipped!
                int MeasIndex = 0; // Measurement runtime Index counter
                if (step.Exists("TS.SequenceCall"))
                {
                    uut.AddSequenceCall(stepRow, step.SequenceName, step.SequenceFileName, step.SequenceFileVersion);
                    //Recurse this list
                    if (step.Exists("TS.SequenceCall.ResultList"))
                        AddSteps(uut, stepRow, step.GetChildren("TS.SequenceCall.ResultList"));
                }
                if (step.Exists("TS.PostAction.ResultList"))
                {
                    uut.AddSequenceCall(stepRow, step.SequenceName, step.SequenceFileName, step.SequenceFileVersion);
                    //Recurse this list
                    AddSteps(uut, stepRow, step.GetChildren("TS.PostAction.ResultList"));
                }
                if (step.Exists("Numeric")) // Single numeric limit
                {
                    // Inherit status from step:
                    MeasurementResultType meas_result = stepRow.Status == StepResultType.Passed ? MeasurementResultType.Passed : MeasurementResultType.Failed;

                    NumericLimit_type nlrow = uut.AddNumericLimit(stepRow, MeasIndex, meas_result);
                    // Set measurement data:
                    nlrow.NumericValue = step.getDoubleValue("Numeric", double.NaN);
                    nlrow.Units = step.getStringValue("Units");
                    if (step.Exists("Limits"))
                    {
                        if (step.Exists("Limits.Low"))
                            nlrow.LowLimit = step.getDoubleValue("Limits.Low", double.NaN);
                        if (step.Exists("Limits.High"))
                            nlrow.HighLimit = step.getDoubleValue("Limits.High", double.NaN);
                    }
                    if (step.Exists("Comp"))
                    {
                        nlrow.CompOperator = step.getStringValue("Comp");
                        if (step.getStringValue("Comp") == "EQT") ComputeActualLimits(step, nlrow);
                    }
                    nlrow.MeasOrderNumber = uut.GetNextMeasOrderNumber();
                }
                if (step.Exists("PassFail")) // Single PassFail (bool)
                {
                    bool pfValue;
                    if (!bool.TryParse(step.getStringValue("PassFail"), out pfValue)) pfValue = stepRow.Status == StepResultType.Passed; // Default value == inherit step's status (if unspecified or unparsable)
                    PassFail_type pfrow = uut.AddPassFail(stepRow, MeasIndex++, pfValue);
                }
                if (step.Exists("String")) // Single StringValue
                {
                    StringValue_type svrow = uut.AddStringValue(stepRow, MeasIndex++);
                    //WATSReport.StringValueDataTable svtbl = svrow.Table as WATSReport.StringValueDataTable;
                    // Inherit status from step:
                    svrow.Status = stepRow.Status == StepResultType.Passed ? MeasurementResultType.Passed : MeasurementResultType.Failed;
                    // Set measurement data:
                    svrow.StringValue = step.getStringValue("String");
                    svrow.StringLimit = step.getStringValue("Limits.String", string.Empty);
                    svrow.CompOperator = step.getStringValue("Comp", string.Empty); ;
                }
                if (step.Exists("Chart")) // Chart data
                {
                    XElementParser.Chart chart = step.GetChartData();
                    Chart_type crow = uut.AddChart(stepRow, chart.ChartLabel, chart.Xlabel, chart.Xunit, chart.Ylabel, chart.Yunit, chart.ChartType);
                    int n = 1;
                    foreach (XElementParser.ChartPlot plot in chart.plots)
                        uut.AddPlot(crow, n++, plot.PlotName, plot.PlotDataType, plot.PlotData);
                }
                if (step.Exists("Measurement"))
                    foreach (XElement meas in step.Measurements)
                    {
                        XElementParser.TEMeasurement measurement = new XElementParser.TEMeasurement(meas.Elements("Prop").First()); // TestStand 3.0 does not use TypeName, cannot filter on Type=="Obj"
                        //XElementParser.TEMeasurement measurement = new XElementParser.TEMeasurement((from me in meas.Elements("Prop") where me.Attribute("Type").Value == "Obj" select me).First());
                        // Analyze measurement type
                        MeasurementResultType measresult;
                        switch (measurement.MeasurementType)
                        {
                            case MeasurementTypes.Numeric:

                                if (!Enum.TryParse<MeasurementResultType>(measurement.getStringValue("Status"), out measresult))
                                    measresult = MeasurementResultType.Failed; // Anything but 'Passed' => Failed !
                                NumericLimit_type nlrow = uut.AddNumericLimit(stepRow, MeasIndex++, measresult);
                                // Set measurement data:
                                nlrow.Name = measurement.Name;
                                nlrow.NumericValue = measurement.getDoubleValue("Data", double.NaN);
                                nlrow.Units = measurement.getStringValue("Units");
                                if (measurement.Exists("Limits"))
                                {
                                    if (measurement.Exists("Limits.Low"))
                                        nlrow.LowLimit = measurement.getDoubleValue("Limits.Low", double.NaN);
                                    if (measurement.Exists("Limits.High"))
                                        nlrow.HighLimit = measurement.getDoubleValue("Limits.High", double.NaN);
                                }
                                if (measurement.Exists("Comp"))
                                {
                                    nlrow.CompOperator = measurement.getStringValue("Comp");
                                    if (measurement.getStringValue("Comp") == "EQT") ComputeActualLimits(measurement, nlrow);
                                }
                                nlrow.MeasOrderNumber = uut.GetNextMeasOrderNumber();
                                break;
                            case MeasurementTypes.String:
                                StringValue_type svrow = uut.AddStringValue(stepRow, MeasIndex++);

                                // Set measurement data:
                                svrow.Name = measurement.getStringValue("MeasName");
                                svrow.StringValue = measurement.StringData;
                                if (measurement.Exists("StringLimit"))
                                    svrow.StringLimit = measurement.StringLimit;

                                if (!Enum.TryParse<MeasurementResultType>(measurement.getStringValue("Status"), out measresult))
                                    measresult = MeasurementResultType.Failed; // Anything but 'Passed' => Failed !

                                svrow.Status = measresult;
                                if (measurement.Exists("Comp"))
                                    svrow.CompOperator = measurement.Comp;
                                svrow.MeasOrderNumber = uut.GetNextMeasOrderNumber();
                                break;
                            case MeasurementTypes.Boolean:

                                PassFail_type pfrow = uut.AddPassFail(stepRow, MeasIndex++, measurement.BoolData);
                                // Set measurement data:
                                pfrow.Name = measurement.getStringValue("MeasName");
                                pfrow.MeasOrderNumber = uut.GetNextMeasOrderNumber();
                                break;
                            default:
                                // Unknown Measurement type....
                                break;
                        }

                    }
                // if (step.Exists("SPC_Data") && SPC_Data.Length>0) --> Add SPCData ??? // SPC Data logging discontinued from version 3.0 !
                if (step.Exists("ButtonHit"))
                {
                    uut.AddMessageBoxResult(stepRow, step.getIntValue("ButtonHit", 0), step.getStringValue("Response"));
                }
                if (step.Exists("ExitCode"))
                {
                    uut.AddCallExecutableResult(stepRow, step.getIntValue("ExitCode", 0));
                }
                if (step.Exists("NumPropertiesRead") || step.Exists("NumPropertiesApplied"))
                {
                    uut.AddPropertyLoaderResult(stepRow, step.getIntValue("NumPropertiesRead", 0), step.getIntValue("NumPropertiesApplied", 0));
                }
                if (step.Exists("AttachedFileData"))
                {
                    string fpath = step.getStringValue("AttachedFileData.FullFileName");
                    if (!String.IsNullOrEmpty(fpath))
                    {
                        FileInfo file = new FileInfo(fpath);
                        if (file.Exists)
                        {
                            using (FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                            {
                                uut.AddFileAttachment(stepRow, step.getStringValue("AttachedFileData.FileName"), step.getStringValue("AttachedFileData.MIMEtype"), fs);
                            }
                            DeleteFiles.Add(file); //Always remove temp file
                        }
                    }
                }
                if (step.Exists("XMLString"))
                {
                    MemoryStream stream = new MemoryStream(System.Text.Encoding.Default.GetBytes(step.getStringValue("XMLString").Replace("\\r\n", "\n")));
                    uut.AddFileAttachment(stepRow, "stepdata.xml", "text/xml", stream);
                }
                //                }
                if (step.Exists("AdditionalResults"))
                {
                    int index = 0;
                    foreach (XElement addres in step.AdditionalResults)
                    {
                        var ares = GetAdditionalResults(addres);
                        uut.AddAdditionalResult(stepRow, index++, ares.Name, ares.Element);
                    }
                }
            }
        }

        private XElementParser GetAdditionalResults(XElement arrayIndexElement)
        {
            //AddAdditionalResults adds each child XElement of the element. 
            //When the child elements are values, we send the element above so the prop element will be included.

            var prop = new XElementParser(arrayIndexElement.Element("Prop"));
            if (prop.Element.Elements("Value").Any())
            {
                var extraProp = new XElement("Prop");
                extraProp.SetAttributeValue("Name", prop.Name);
                extraProp.Add(prop.Element);
                return new XElementParser(extraProp);
            }
            else
                return prop;
        }

        private void ComputeActualLimits(XElementParser element, NumericLimit_type nlrow)
        {
            if (element.Exists("Limits.ThresholdType"))
            {
                double nominal_value = element.getDoubleValue("RawLimits.Nominal", double.NaN);
                double threshold_low = element.getDoubleValue("RawLimits.Low", double.NaN);
                double threshold_high = element.getDoubleValue("RawLimits.High", double.NaN);
                switch (element.getStringValue("Limits.ThresholdType").ToLower())
                {
                    case "percentage":
                        if (!double.IsNaN(threshold_low)) nlrow.LowLimit = nominal_value - (Math.Abs(nominal_value) * threshold_low / 100);
                        if (!double.IsNaN(threshold_high)) nlrow.HighLimit = nominal_value + (Math.Abs(nominal_value) * threshold_high / 100);
                        break;
                    case "ppm":
                        if (!double.IsNaN(threshold_low)) nlrow.LowLimit = nominal_value - (Math.Abs(nominal_value) * threshold_low / 1000000);
                        if (!double.IsNaN(threshold_high)) nlrow.HighLimit = nominal_value + (Math.Abs(nominal_value) * threshold_high / 1000000);
                        break;
                    case "delta":
                        if (!double.IsNaN(threshold_low)) nlrow.LowLimit = nominal_value - threshold_low;
                        if (!double.IsNaN(threshold_high)) nlrow.HighLimit = nominal_value + threshold_high;
                        break;
                }
            }
        }

        private StepResultType SetSequenceStepData(TSUUTReport uut, XElementParser.TEResult step, Step_type stepRow)
        {
            DateTime dtStart = uut.GetStartTime(step.StartTime);
            StepGroup_type group;
            if (Enum.TryParse<StepGroup_type>(step.StepGroup, out group)) stepRow.Group = group;
            else group = StepGroup_type.Main;
            stepRow.TSGuid = step.StepId;
            stepRow.StepType = step.StepType;
            stepRow.total_time = step.StepTime;
            stepRow.total_timeSpecified = true;
            int? ecode = step.ErrorCode;
            if (ecode.HasValue) stepRow.StepErrorCode = ecode.Value;
            stepRow.StepErrorCodeSpecified = ecode.HasValue;
            stepRow.StepErrorMessage = step.ErrorMessage;
            stepRow.ReportText = step.ReportText;
            StepResultType result;
            if (Enum.TryParse<StepResultType>(step.StepStatusText, out result))
                stepRow.Status = result;
            else
                stepRow.Status = StepResultType.Unknown;
            DateTime def = DateTime.Parse("01-01-1970");
            if (dtStart >= def)
                stepRow.Start = dtStart;
            else
                stepRow.StartSpecified = false;

            if (step.Exists("TS.StepCausedSequenceFailure"))
            {
                stepRow.StepCausedSequenceFailure = step.getBooleanValue("TS.StepCausedSequenceFailure", false);
                stepRow.StepCausedSequenceFailureSpecified = true;
            }
            Step_typeLoop loop = null;
            if (step.Exists("TS.NumLoops"))
            {
                if (loop == null) loop = new Step_typeLoop();
                short? value = step.getShortValue("TS.NumLoops");
                if (value.HasValue) loop.num = value.Value;
                loop.numSpecified = value.HasValue;
            }
            if (step.Exists("TS.EndingLoopIndex"))
            {
                if (loop == null) loop = new Step_typeLoop();
                short? value = step.getShortValue("TS.EndingLoopIndex");
                if (value.HasValue) loop.ending_index = value.Value;
                loop.ending_indexSpecified = value.HasValue;
            }
            if (step.Exists("TS.NumPassed"))
            {
                if (loop == null) loop = new Step_typeLoop();
                short? value = step.getShortValue("TS.NumPassed");
                if (value.HasValue) loop.passed = value.Value;
                loop.passedSpecified = value.HasValue;
            }
            if (step.Exists("TS.NumFailed"))
            {
                if (loop == null) loop = new Step_typeLoop();
                short? value = step.getShortValue("TS.NumFailed");
                if (value.HasValue) loop.failed = value.Value;
                loop.failedSpecified = value.HasValue;
            }
            if (step.Exists("TS.LoopIndex"))
            {
                if (loop == null) loop = new Step_typeLoop();
                short? value = step.getShortValue("TS.LoopIndex");
                if (value.HasValue) loop.index = value.Value;
                loop.indexSpecified = value.HasValue;
            }
            if (loop != null) stepRow.Loop = loop;
            return stepRow.Status;
        }

    }
}