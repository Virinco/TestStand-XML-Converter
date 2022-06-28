using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        public enum MeasurementTypes { Unknown = 0x0, Numeric = 0x1, String = 0x2, Boolean = 0x4 }
        partial class XElementParser
        {
            /// <summary>
            /// Represents a Measurement element type. Inherits from XElementParser, and provides property based access to data belonging to measurement.
            /// </summary>
            internal class TEMeasurement : XElementParser
            {
                internal TEMeasurement(XElement element)
                    : base(element)
                {
                }
                internal TEMeasurement(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                }
                #region Common Measurement properties
                public string Name
                {
                    get
                    {
                        XAttribute atr = this.Element.Attribute("Name");
                        return atr != null ? atr.Value : null;
                    }
                }
                public MeasurementTypes MeasurementType
                {
                    get
                    {
                        MeasurementTypes retVal;
                        if (Exists("Data")) retVal = MeasurementTypes.Numeric;
                        else if (Exists("StringData")) retVal = MeasurementTypes.String;
                        else if (Exists("PassFail")) retVal = MeasurementTypes.Boolean;
                        else retVal = MeasurementTypes.Unknown;
                        return retVal;
                    }
                }
                public string Status { get { return this.getStringValue("Status", string.Empty); } }
                public string Comp { get { return this.getStringValue("Comp", string.Empty); } }
                #endregion
                #region NumericLimit Properties
                public double LowLimit { get { return this.getDoubleValue("Limits.Low", double.NaN); } }
                public double HighLimit { get { return this.getDoubleValue("Limits.High", double.NaN); } }
                public double NumericData { get { return this.getDoubleValue("Data", double.NaN); } }
                public string Units { get { return this.getStringValue("Units", string.Empty); } }
                #endregion
                #region StringValue Properties
                public string StringData { get { return this.getStringValue("StringData", string.Empty); } }
                public string StringLimit { get { return this.getStringValue("StringLimit", string.Empty); } }
                #endregion
                #region Boolean Properties
                public bool BoolData { get { return this.getBooleanValue("PassFail", false); } }
                #endregion
            }
        }
    }
}