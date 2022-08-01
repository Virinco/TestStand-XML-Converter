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

        /// <summary>
        /// Not implemented, may be implemented in future versions.
        /// </summary>
        /// <param name="report"></param>
        /// <param name="writer"></param>
        public void WriteReport(WATSReport report, XmlWriter writer)
        {
            throw new NotImplementedException();
        }
        public WATSReport Import(XmlReader reader)
        {
            WATSReport report = new WATSReport();
            ReadReport(report, reader);
            return report;
        }
        public List<FileInfo> DeleteFiles = new List<FileInfo>();

        Dictionary<string, string> parameters;

        public Dictionary<string, string> ConverterParameters => parameters;

        /// <summary>
        /// Imports ATML Document into WATSReport typed dataset.
        /// </summary>
        /// <param name="report">The WATSReport object to import the NI TS Xml Document into</param>
        /// <param name="reader">XmlReader must be open and positioned on Reports or Report Element</param>
        public void ReadReport(WATSReport report, XmlReader reader)
        {
            if (reader.Name == "Reports")
            {
                XmlReader sbreader = reader.ReadSubtree();
                while (sbreader.Read())
                    if (sbreader.Name == "Report") ImportReport(report, sbreader);
            }
            else if (reader.Name == "Report")
                ImportReport(report, reader);
            else
                throw new ArgumentException("No reports found in provided xml reader");
        }
        #endregion
        #region Interface.IReportConverter Members
        public Report ImportReport(TDM api, FileInfo file)
        {
            using (XmlReader reader = XmlReader.Create(file.FullName))
            {
                if (reader.ReadToDescendant("TSReport"))
                    return ImportReport(api, reader);
                else
                    throw new InvalidDataException("TSReport element was not found.");
            }
        }

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
                return null;
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
            foreach (FileInfo fi in DeleteFiles) fi.Delete();
        }
        #endregion
        #region Interface.IReportConverter Helper Methods
        private Report ImportReport(TDM api, XmlReader reader)
        {
            if (reader.Name != "TSReport" && reader.Name != "Reports") throw new ArgumentException("Xml reader must be open and positioned on TSReport element");
            return CreateUUT(api, XDocument.Load(reader));
        }
        #endregion

        /// <summary>
        /// Saves the UUT for sending to server
        /// </summary>
        /// <param name="ctx"></param>
        public void ImportReport(WATSReport report, XmlReader reader)
        {
            TDM api = new TDM
            {
                TestMode = TestModeType.TestStand//Do not perform logic in api, allow custom stepordernumber, index and status insertion, and protected constructor for accessing proteced members in UUTReport
            };
            api.InitializeAPI(false);
            CreateUUT(api, XDocument.Load(reader));
        }

        private UUTReport CreateUUT(TDM api, XDocument xmlReport)
        {
            api.TestMode = TestModeType.TestStand;
            api.ValidationMode = ValidationModeType.AutoTruncate;
            TSDumpReport dump = new TSDumpReport(xmlReport.Root);
            XElementParser.TEResult xpRoot = dump.RootResult;
            XElementParser xpTS = xpRoot["TS"];

            //If they don't exist, we'll check the xml later for the information we need. 
            XElementParser xpUUT = dump.UUTInfo;
            XElementParser xpStationInfo = dump.StationInfo;

            bool xpStationExists = true, xpUUTExists = true;
            string oper, stationId, location, purpose;

            XElement stationInfo = null, Uut = null;
            if (xpStationInfo.DataType != null) //Check for datatype != null since name can cause crashes when not defined. 
                xpStationExists = true;
            else
            {
                //Default to this since xpStation does not have values in these cases. 
                stationInfo = xmlReport.Root.Element("Report").Elements().Where(el => el.Attribute("TypeName").Value == "NI_StationInfo").FirstOrDefault();
                xpStationExists = false;
            }

            if (xpUUT.DataType != null)
                xpUUTExists = true;
            else
            {
                Uut = xmlReport.Root.Element("Report").Elements().Where(el => el.Attribute("TypeName").Value == "UUT").FirstOrDefault();
                xpUUTExists = false;
            }

            //Get from file, if xpStation has values and we do not find anything in file, default to xpStation data.
            if (!xpStationExists && stationInfo.Elements().Where(el => el.Attribute("Name").Value == "LoginName").FirstOrDefault() != null)
                oper = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "LoginName").FirstOrDefault().Value.Trim();
            else if (xpStationExists)
                oper = xpStationInfo.getStringValue("LoginName");
            else
                oper = "";

            if (!xpStationExists && stationInfo.Elements().Where(el => el.Attribute("Name").Value == "StationID").FirstOrDefault() != null)
                stationId = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "StationID").FirstOrDefault().Value.Trim();
            else if (xpStationExists)
                stationId = xpStationInfo.getStringValue("StationID");
            else
                stationId = "";

            if (!xpStationExists && stationInfo.Elements().Where(el => el.Attribute("Name").Value == "Location").FirstOrDefault() != null)
                location = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "Location").FirstOrDefault().Value.Trim();
            else if (!xpStationExists && parameters["location"] != "")
                location = parameters["location"];
            else if (xpStationExists)
                location = xpStationInfo.getStringValue("Location");
            else
                location = "";

            if (!xpStationExists && stationInfo.Elements().Where(el => el.Attribute("Name").Value == "Purpose").FirstOrDefault() != null)
                purpose = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "Purpose").FirstOrDefault().Value.Trim();
            else if (!xpStationExists && parameters["purpose"] != "")
                purpose = parameters["purpose"];
            else if (xpStationExists)
                purpose = xpStationInfo.getStringValue("Purpose");
            else
                purpose = "";


            XElementParser xpRootSequence = xpTS["SequenceCall"];

            TSUUTReport uut = new TSUUTReport(api, true, dump.EngineStarted, oper, xpRoot.SequenceFileName, xpRoot.SequenceFileVersion, true);

            uut.SetStationInfo(stationId, location, purpose);
            IEnumerable<XElement> additionalStationInfos;
            if (xpStationExists)
                additionalStationInfos = xpStationInfo["AdditionalData"]?.Element?.Elements("Prop");
            else
                additionalStationInfos = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "AdditionalData");
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


            // Set ReportId if parsable... If not, generate new
            var guidE = Guid.TryParse(dump.ID.ToString(), out Guid id);

            if (guidE && id.ToString() != "00000000-0000-0000-0000-000000000000")
                uut.SetReportId(id);
            else
                uut.SetReportId(Guid.NewGuid());

            //Use xpStationExists so we know if we have this data in the file or not. 
            if (!xpStationExists)
            {
                var date = xmlReport.Root.Element("Report").Elements().Where(el => el.Attribute("TypeName").Value == "DateDetails").FirstOrDefault();
                var time = xmlReport.Root.Element("Report").Elements().Where(el => el.Attribute("TypeName").Value == "TimeDetails").FirstOrDefault();

                string datetime;
                string datestring, timestring;
                string day, month, year;

                day = date.Elements().Where(el => el.Attribute("Name").Value == "MonthDay").FirstOrDefault().Value.Trim();
                month = date.Elements().Where(el => el.Attribute("Name").Value == "Month").FirstOrDefault().Value.Trim();
                year = date.Elements().Where(el => el.Attribute("Name").Value == "Year").FirstOrDefault().Value.Trim();
                //date
                datestring = day + "/" + month + "/" + year;
                timestring = time.Elements().Where(el => el.Attribute("Name").Value == "Text").FirstOrDefault().Value.Trim();

                datetime = datestring + " " + timestring;
                var dtoE = DateTimeOffset.TryParse(datetime, out DateTimeOffset datetimeoffset);
                if (dtoE)
                    uut.StartDateTimeOffset = datetimeoffset;
                else
                    uut.StartDateTimeOffset = dump.Start.LocalDateTime;
            }
            else
                uut.StartDateTimeOffset = dump.Start.LocalDateTime;

            //Need this so we can get all elements manually if xpUUT is null or unset. 
            if (xpUUTExists)
            {
                uut.PartNumber = xpUUT.getStringValue("UUTPartNumber");
                uut.PartRevisionNumber = xpUUT.getStringValue("UUTPartRevisionNumber");
                uut.SerialNumber = xpUUT.getStringValue("SerialNumber");
                uut.OperationType = api.GetOperationType(xpUUT.getStringValue("UUTOperationType"));
                uut.Comment = xpUUT.getStringValue("Comment");
                uut.FixtureId = xpUUT.getStringValue("UUT_Fixture_ID");
                uut.BatchSerialNumber = xpUUT.getStringValue("BatchSerialNumber");
                uut.TestSocketIndex = (short)xpUUT.getIntValue("TestSocketIndex", 0);
            }
            else if (!xpUUTExists)
            {
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTPartNumber").FirstOrDefault() != null)
                    uut.PartNumber = Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTPartNumber").FirstOrDefault().Value.Trim();
                else
                    uut.PartNumber = Uut.Elements().Where(el => el.Attribute("Name").Value == "PartNumber").FirstOrDefault().Value.Trim();
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTPartRevisionNumber").FirstOrDefault() != null)
                    uut.PartRevisionNumber = Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTPartRevisionNumber").FirstOrDefault().Value.Trim();
                else
                    uut.PartRevisionNumber = "";
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "SerialNumber").FirstOrDefault() != null)
                    uut.SerialNumber = Uut.Elements().Where(el => el.Attribute("Name").Value == "SerialNumber").FirstOrDefault().Value.Trim();
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "Comment").FirstOrDefault() != null)
                    uut.Comment = Uut.Elements().Where(el => el.Attribute("Name").Value == "Comment").FirstOrDefault().Value.Trim();
                else
                    uut.Comment = "";
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "UUT_Fixture_ID").FirstOrDefault() != null)
                    uut.FixtureId = Uut.Elements().Where(el => el.Attribute("Name").Value == "UUT_Fixture_ID").FirstOrDefault().Value.Trim();
                else
                    uut.FixtureId = "";
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "BatchSerialNumber").FirstOrDefault() != null)
                    uut.BatchSerialNumber = Uut.Elements().Where(el => el.Attribute("Name").Value == "BatchSerialNumber").FirstOrDefault().Value.Trim();
                else
                    uut.BatchSerialNumber = "";
                if (Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTOperationType").FirstOrDefault() != null)
                    uut.OperationType = api.GetOperationType(Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTOperationType").FirstOrDefault().Value.Trim());
                else
                    uut.OperationType = api.GetOperationType(parameters["operationTypeCode"]);

                bool bliE = int.TryParse(Uut.Elements().Where(el => el.Attribute("Name").Value == "UUTLoopIndex").FirstOrDefault().Value, out int bli);
                var tsiE = short.TryParse(Uut.Elements().Where(el => el.Attribute("Name").Value == "TestSocketIndex").FirstOrDefault().Value.Trim(), out short tsi);
                if (bliE)
                    uut.BatchLoopIndex = bli;
                if (tsiE)
                    uut.TestSocketIndex = tsi;
            }


            uut.ExecutionTime = xpTS.getDoubleValue("TotalTime", 0);
            uut.SetStatus(xpRoot.getStringValue("Status"));
            uut.ErrorCode = xpRoot.getIntValue("Error.Code", 0);
            uut.ErrorMessage = xpRoot.getStringValue("Error.Msg");
            uut.SetStatus(xpRoot.getStringValue("Status"));

            //We default to trying to fetch data ourselves since generated reports should follow the "old" code.
            IEnumerable<XElement> additionalDataProps;
            if (xpStationExists)
                additionalDataProps = xpStationInfo["AdditionalData"]?.Element?.Elements("Prop");
            else
                additionalDataProps = stationInfo.Elements().Where(el => el.Attribute("Name").Value == "AdditionalData");

            if (additionalDataProps != null)
            {
                foreach (var additionalDataProp in additionalDataProps)
                {
                    var xpAdditionalData = new XElementParser(additionalDataProp);
                    uut.AddAdditionalData(xpAdditionalData.Name, xpAdditionalData.Element);
                }
            }

            //For compatibility with standard reports. 
            if (xpUUTExists)
            {
                XElementParser xpUUTAdditional = xpUUT["MiscUUTResult"];
                XElementParser xpUUTMiscInfo = xpUUTAdditional["Misc_UUT_Info"];
                if (xpUUTMiscInfo != null)
                    foreach (XElement el in xpUUTMiscInfo.getValues())
                    {
                        XElementParser.MiscInfo xp = XElementParser.Create(el) as XElementParser.MiscInfo;
                        uut.AddMiscUUTInfo(xp);
                    }
                XElementParser xpUUTPartInfo = xpUUTAdditional["UUT_Part_Info"];
                if (xpUUTPartInfo != null)
                    foreach (XElement el in xpUUTPartInfo.getValues())
                    {
                        XElementParser.PartInfo xp = XElementParser.Create(el) as XElementParser.PartInfo;
                        uut.AddUUTPartInfo(xp);
                    }
            }
            else if (!xpUUTExists && Uut.Elements().Where(el => el.Attribute("Name").Value == "MiscUUTResult").FirstOrDefault() != null)
            {
                var miscUUT = Uut.Elements().Where(el => el.Attribute("Name").Value == "MiscUUTResult").FirstOrDefault();
                var partInfo = miscUUT.Elements().Where(el => el.Attribute("Name").Value == "UUT_Part_Info").FirstOrDefault();
                var miscInfo = miscUUT.Elements().Where(el => el.Attribute("Name").Value == "Misc_UUT_Info").FirstOrDefault();

                //partInfo
                foreach (XElement el in partInfo.Elements())
                {
                    string partType, partNumber, partSerial, partRevision;
                    partType = el.Elements().Where(e => e.Attribute("Name").Value == "Part_Type").FirstOrDefault().Value.Trim();
                    partNumber = el.Elements().Where(e => e.Attribute("Name").Value == "Part_Number").FirstOrDefault().Value.Trim();
                    partSerial = el.Elements().Where(e => e.Attribute("Name").Value == "Part_Serial_Number").FirstOrDefault().Value.Trim();
                    partRevision = el.Elements().Where(e => e.Attribute("Name").Value == "Part_Revision_Number").FirstOrDefault().Value.Trim();

                    //If any field is empty, do not add as all are required. 
                    if (partType != "" && partNumber != "" && partSerial != "" && partRevision != "")
                        uut.AddUUTPartInfo(partType, partNumber, partSerial, partRevision);
                }

                //MiscInfo
                foreach (XElement el in miscInfo.Elements())
                {
                    string description, stringData;
                    short numericData;
                    bool numericE;

                    description = el.Elements().Where(e => e.Attribute("Name").Value == "Description").FirstOrDefault().Value.Trim();
                    stringData = el.Elements().Where(e => e.Attribute("Name").Value == "Data_String").FirstOrDefault().Value.Trim();
                    numericE = short.TryParse(el.Elements().Where(e => e.Attribute("Name").Value == "Data_Numeric").FirstOrDefault().Value.Trim(), out numericData);

                    //If description is null or empty, do not add. 
                    if (!string.IsNullOrEmpty(description) && numericE && stringData != "")
                        uut.AddMiscUUTInfo(description, stringData, numericData);
                    else if (!string.IsNullOrEmpty(description) && numericE && stringData == "")
                        uut.AddMiscUUTInfo(description, numericData);
                    else if (!string.IsNullOrEmpty(description) && !numericE && stringData != "")
                        uut.AddMiscUUTInfo(description, stringData);

                }
            }

            // SPC Data logging will be discontinued from WATS version 3.0 (?)
            //XElementParser xpUUTSpcData = xpUUTAdditional["SPC_Data"];
            //if (xpUUTSpcData != null)
            //    foreach (XElement el in xpUUTSpcData.getValues())
            //    {
            //        XElementParser xp = new XElementParser(el);
            //        uut.AddSPCData(
            //            xp.getStringValue("Type"),
            //            xp.getStringValue("Var"),
            //            xp.getStringValue("Num")
            //    );
            //    }
            SetSequenceStepData(uut, xpRoot, uut.RootStepRow);
            AddSteps(uut, uut.RootStepRow, xpRoot.GetChildren("TS.SequenceCall.ResultList"), xmlReport);
            //api.Submit(uut); 
            return uut;
        }

        //Small step inbetween to ensure that we have the report steps available. 
        private void AddSteps(TSUUTReport uut, Step_type parentRow, IEnumerable<XElement> steps, XDocument document)
        {
            if (!steps.Any())
            {
                steps = document.Root.Elements("Report");
            }
            AddSteps(uut, parentRow, steps);
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