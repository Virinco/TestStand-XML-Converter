using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Virinco.WATS.Interface;
using Virinco.WATS.Schemas.WRML;

namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        /// <summary>
        /// Private helperclass for accessing internal (protected) wrml class objects in TDM-API's UUTReport class.
        /// </summary>
        private class TSUUTReport : UUTReport
        {
            /// <summary>
            /// internal constructor
            /// </summary>
            /// <param name="apiRef">Reference to active TDM-API</param>
            /// <param name="createHeader">Create (default) Report Header</param>
            /// <param name="EngineStarted">TestStand EngineStart datetime. All TestStand internal time properties are relative to this value.</param>
            internal TSUUTReport(TDM apiRef, bool createHeader, DateTime EngineStarted, string operatorName, string sequenceName, string sequenceVersion, bool initializeRootSequence)
                : base(apiRef, createHeader)
            {
                _engineStarted = EngineStarted;
                base.InitializeUutHeader(
                    operatorName,//.Truncate(reportDataSet.UUTHeader.USER_LOGIN_NAMEColumn.MaxLength),
                    sequenceName,//.Truncate(reportDataSet.UUTHeader.SequenceFilenameColumn.MaxLength),
                    sequenceVersion,//.Truncate(reportDataSet.UUTHeader.SequenceFileversionColumn.MaxLength),
                    initializeRootSequence);
            }
            //internal Virinco.WATS.WATSReport report { get { return this.reportDataSet; } }
            //internal Virinco.WATS.WATSReport.StepRow FindRow(Virinco.WATS.Interface.Step apiStep)
            //{
            //    return base.reportDataSet.Step.FindByReport_IDStepOrderNumber(this.reportRow.Report_ID, apiStep.StepOrderNumber);
            //}
            //private bool nl_measordernumber_exists, sv_measordernumber_exists, pf_measordernumber_exists;

            private DateTime _engineStarted;

            //internal DateTime EngineStarted { get { return _engineStarted; } }

            internal DateTime GetStartTime(double TSEngineTime) { return _engineStarted.AddSeconds(TSEngineTime); }
            /// <summary>
            /// Add new StepRow with Specified Parent-step, StepOrderNumber, Name and StepIndex.
            /// Beware: StepIndex is specified as Int32 - but storage is currently Int16. Step indexes with overflow will be truncated to Int16 max/min value.
            /// </summary>
            /// <param name="parentRow">Parent step</param>
            /// <param name="StepOrderNumber">Step order number must be unique within a report.</param>
            /// <param name="Name">Step Name - may be string.Empty, but this is not recommended</param>
            /// <param name="StepIndex">Designtime step index (within sequence).</param>
            /// <returns>New step-row with minimum data, Status(enum) is initialized to Unknown. Returned row is a member of report dataset</returns>
            /// <exception cref="System.Data.ConstraintException">The addition invalidates a constraint.</exception>
            /// <exception cref="System.Data.NoNullAllowedException">The addition tries to put a null in a <see cref="System.Data.DataColumn">DataColumn</see> where <see cref="System.Data.DataColumn.AllowDBNull">AllowDBNull</see> is false.</exception>
            internal Step_type AddStep(Step_type parentstep, /*int StepOrderNumber,*/ string Name, int StepIndex)
            {
                //base.reportRow;
                //base.GetRootSequenceCall();
                Step_type row = new Step_type();
                if (parentstep != null) { row.ParentStepID = parentstep.StepID; row.ParentStepIDSpecified = true; }
                row.StepID = this.GetNextStepOrder();
                row.Name = Name;
                row.StepIndex = StepIndex;
                row.StepIndexSpecified = true;
                row.Status = StepResultType.Unknown;
                base.reportRow.Items.Add(row);
                return row;
            }
            internal NumericLimit_type AddNumericLimit(Step_type step, int MeasIndex, MeasurementResultType Status)
            {
                NumericLimit_type row = new NumericLimit_type();
                //WATSReport.NumericLimitRow row = reportDataSet.NumericLimit.NewNumericLimitRow();
                row.StepID = step.StepID;
                row.MeasIndex = MeasIndex;
                row.Status = Status;
                row.MeasOrderNumber = GetNextMeasOrderNumber();
                reportRow.Items.Add(row);
                return row;
            }

            internal PassFail_type AddPassFail(Step_type step, int MeasIndex, bool Passed)
            {
                PassFail_type row = new PassFail_type()
                {
                    StepID = step.StepID,
                    MeasIndex = MeasIndex,
                    MeasIndexSpecified = true,
                    MeasOrderNumber = GetNextMeasOrderNumber(),
                    MeasOrderNumberSpecified = true,
                    Status = Passed ? MeasurementResultType.Passed : MeasurementResultType.Failed
                };
                reportRow.Items.Add(row);
                return row;
            }
            internal StringValue_type AddStringValue(Step_type step, int MeasIndex)
            {
                StringValue_type row = new StringValue_type()
                {
                    StepID = step.StepID,
                    MeasIndex = MeasIndex,
                    MeasIndexSpecified = true,
                    MeasOrderNumber = GetNextMeasOrderNumber(),
                    MeasOrderNumberSpecified = true
                };
                reportRow.Items.Add(row);
                return row;
            }
            private short _measOrderNumber = 0; // Internal MeasOrderNumber counter.
            internal short GetNextMeasOrderNumber()
            {
                if (_measOrderNumber < short.MaxValue) return _measOrderNumber++;
                else return _measOrderNumber;
            }

            internal bool SetStatus(string StatusText)
            {
                ReportResultType status;
                if (Enum.TryParse<ReportResultType>(StatusText, out status))
                {
                    reportRow.Result = status;
                    reportRow.ResultSpecified = true;
                    return true;
                }
                else
                    return false;
            }

            internal Chart_type AddChart(Step_type step, string ChartLabel, string Xlabel, string Xunit, string Ylabel, string Yunit, string ChartType)
            {
                Chart_type row = new Chart_type()
                {
                    StepID = step.StepID,
                    idx = 0,
                    idxSpecified = true,
                    ChartType = ChartType,
                    Label = ChartLabel,
                    XLabel = Xlabel,
                    XUnit = Xunit,
                    YLabel = Ylabel,
                    YUnit = Yunit
                };
                reportRow.Items.Add(row);
                return row;
            }
            internal Chart_type AddPlot(Chart_type chart, int Index, string Name, string DataType, byte[] Data)
            {
                Chart_type row = new Chart_type()
                {
                    StepID = chart.StepID,
                    idx = (short)Index,
                    idxSpecified = true,
                    PlotName = Name,
                    DataType = DataType,
                    Data = Data
                };
                reportRow.Items.Add(row);
                return row;
            }

            internal void SetReportId(Guid ReportId)
            {
                this.reportRow.ID = ReportId.ToString();
            }

            internal MessagePopup_type AddMessageBoxResult(Step_type step, int ButtonPressed, string TextResponse)
            {
                MessagePopup_type row = new MessagePopup_type()
                {
                    StepID = step.StepID,
                    Button = (short)ButtonPressed,
                    ButtonSpecified = true,
                    Response = TextResponse,
                    MeasIndex = 0,
                    MeasIndexSpecified = true
                };
                reportRow.Items.Add(row);
                return row;
            }

            internal Callexe_type AddCallExecutableResult(Step_type step, double ExitCode)
            {
                Callexe_type row = new Callexe_type()
                {
                    StepID = step.StepID,
                    ExitCode = ExitCode,
                    ExitCodeSpecified = true
                };
                reportRow.Items.Add(row);
                return row;
            }

            internal PropertyLoader_type AddPropertyLoaderResult(Step_type step, int PropsLoaded, int PropsApplied)
            {
                PropertyLoader_type row = new PropertyLoader_type()
                {
                    StepID = step.StepID,
                    Read = (short)PropsLoaded,
                    ReadSpecified = true,
                    Applied = (short)PropsApplied,
                    AppliedSpecified = true
                };
                reportRow.Items.Add(row);
                return row;
            }

            internal AdditionalResults_type AddAdditionalResult(Step_type step, int Index, string Name, XElement contents)
            {
                AdditionalResults_type row = new AdditionalResults_type()
                {
                    Idx = Index,
                    IdxSpecified = true,
                    Name = Name
                };
                if (step != null) // Support Report level additional result?
                {
                    row.StepID = step.StepID;
                    row.StepIDSpecified = true;
                }

                row.Any.AddRange((new XmlDocument().ReadNode(contents.CreateReader()) as XmlElement).ChildNodes.OfType<XmlElement>());

                reportRow.Items.Add(row);
                return row;
            }

            internal void AddFileAttachment(Step_type step, string FileName, string MimeType, Stream Data)
            {
                /*
                "Header": 
                    INSERT INTO STEP_CHARTDATA([STEP_RESULT], [PLOT_IDX], [CHART_LABEL],[CHART_TYPE])
                    VALUES(@pSTEP_RESULT, 0, @pFILE_NAME, 'ATTACHMENT')
                Attachment:
                    INSERT INTO STEP_CHARTDATA([STEP_RESULT], [PLOT_IDX], [PLOT_NAME],[PLOT_DATA])
                    VALUES(@pSTEP_RESULT, 1, @pMIME_TYPE,0x00)
                */
                Chart_type hdr = new Chart_type()
                {
                    StepID = step.StepID,
                    idx = 0,
                    Label = FileName,
                    ChartType = "ATTACHMENT"
                };
                reportRow.Items.Add(hdr);
                Chart_type att = new Chart_type()
                {
                    StepID = step.StepID,
                    idx = 1,
                    PlotName = MimeType
                };
                int len = (int)Data.Length;
                att.Data = new byte[len]; ;
                Data.Read(att.Data, 0, len);
                reportRow.Items.Add(att);
            }

            internal SequenceCall_type AddSequenceCall(Step_type step, string SequenceName, string SequenceFileName, string SequenceFileVersion)
            {

                // Locate existing sequence by step-order-number:
                SequenceCall_type row = reportRow.Items.OfType<SequenceCall_type>().SingleOrDefault(sc => sc.StepID == step.StepID);
                if (row == null)
                {
                    // Not found, create:
                    row = new SequenceCall_type()
                    {
                        StepID = step.StepID,
                        Name = SequenceName ?? "",
                        Version = SequenceFileVersion ?? "",
                        Filepath = SequenceFileName ?? "",
                        Filename = ""
                    };
                    reportRow.Items.Add(row);
                }
                else
                {
                    // Found, update values:
                    row.Name = SequenceName ?? "";
                    row.Version = SequenceFileVersion ?? "";
                    row.Filepath = SequenceFileName ?? "";
                    row.Filename = "";
                }
                return row;
            }

            public Step_type RootStepRow
            {
                get
                {
                    // Return the one and only rootstep (will fail if missing, or multiple!). Consider changing to FirstOrDefault???
                    return reportRow.Items.OfType<Step_type>().Single(s => s.ParentStepIDSpecified == false);
                }
            }

            internal void SetStationInfo(string StationId, string Location, string Purpose)
            {
                reportRow.MachineName = StationId;
                reportRow.Location = Location;
                reportRow.Purpose = Purpose;
            }

            internal PartInfo_type AddUUTPartInfo(XElementParser.PartInfo xp)
            {
                // Get PartInfo count to use as new index:
                short pi_count = (short)reportRow.Items.OfType<PartInfo_type>().Count();
                PartInfo_type row = new PartInfo_type()
                {
                    idx = pi_count,
                    idxSpecified = true,
                    order_no = pi_count,
                    order_noSpecified = true,
                    PartType = xp.PartType,
                    PN = xp.PartNumber,
                    SN = xp.SerialNumber,
                    Rev = xp.RevisionNumber
                };
                reportRow.Items.Add(row);
                return row;
            }

            internal MiscInfo_type AddMiscUUTInfo(XElementParser.MiscInfo xp)
            {
                short pi_count = (short)reportRow.Items.OfType<MiscInfo_type>().Count();
                MiscInfo_type row = new MiscInfo_type()
                {
                    idx = pi_count,
                    idxSpecified = true,
                    order_no = pi_count,
                    order_noSpecified = true,
                    Typedef = "",
                    Description = xp.Description,
                    Value = xp.StringValue,
                };
                short? numvalue = xp.NumericValue;
                if (numvalue.HasValue)
                {
                    row.Numeric = numvalue.Value;
                    row.NumericSpecified = true;
                }
                reportRow.Items.Add(row);
                return row;
            }
        }
    }
}
