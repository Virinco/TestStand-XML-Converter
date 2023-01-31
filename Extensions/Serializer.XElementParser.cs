using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        internal partial class XElementParser
        {
            internal XElementParser(XElement element)
            {
                _element = element;
                if (element.Attribute("Type") != null)
                    _datatype = getDataType(element.Attribute("Type").Value);
                else
                    _datatype = null;
            }
            internal XElementParser(XElement element, string NamePath)
            {
                _element = getElement(element, NamePath, out _datatype);
            }
            public static XElementParser Create(XElement element, string NamePath)
            {
                XElement tmpElement = getElement(element, NamePath);
                if (tmpElement == null)
                    return null; //TODO: Find out what to do when TS.SequenceCall is missing ResultList
                else
                    return Create(tmpElement);
            }
            public static XElementParser Create(XElement element)
            {
                /*
                 May be called on Value element or Value's child Prop element.
                 * If called on Value element, check First Prop element
                 * If called on Prop element directly descending from Value element, 
                 */
                XElementParser xp = null;
                if (element.Name == "Value")
                    element = element.Element("Prop");
                XAttribute atrType = element.Attribute("Type");
                string sType = atrType != null ? atrType.Value : "";
                XAttribute atrTypeName = element.Attribute("TypeName");
                string sTypeName = atrTypeName != null ? atrTypeName.Value : "";
                //System.Type type = getDataType(element.Attribute("Type").Value);
                //if (atrType != null)
                //    type = getDataType(element.Attribute("Type").Value);
                //else type = typeof(object);
                if (element != null)
                {
                    switch (sType)
                    {
                        case "TEResult": xp = new XElementParser.TEResult(element); break;
                        case "Obj":
                            switch (sTypeName)
                            {
                                case "ET_UUT_Part_Info": xp = new XElementParser.PartInfo(element); break;
                                case "ET_Misc_UUT_Info": xp = new XElementParser.MiscInfo(element); break;
                                case "WATS_Chart_Data": xp = new XElementParser.Chart(element); break;
                                case "WATS_Chart_Plots": xp = new XElementParser.ChartPlot(element); break;
                                default:
                                    if (element.Parent.Name == "Value") xp = new XElementParser.TEMeasurement(element);
                                    else xp = new XElementParser(element);
                                    break;
                            }
                            break;
                        // The two following cases is a TestStand 3.0 compatibility workaround. TS 3.0 use type as dynamic value, while 3.1+ use TypeName for dynamic types (type='Obj')
                        case "ET_UUT_Part_Info": xp = new XElementParser.PartInfo(element); break;
                        case "ET_Misc_UUT_Info": xp = new XElementParser.MiscInfo(element); break;
                        default:
                            xp = new XElementParser(element);
                            break;
                    }
                }
                return xp;
            }
            private XElement _element;
            public XElement Element { get { return _element; } }
            private System.Type _datatype;
            public System.Type DataType { get { return _datatype; } }
            public string TypeName
            {
                get
                {
                    if (_element == null) return null;
                    XAttribute atr = _element.Attribute("TypeName");
                    return atr != null ? atr.Value : null;
                }
            }
            public string Name
            {
                get { XAttribute atr = _element.Attribute("Name"); return (atr != null) ? atr.Value : null; }
            }

            internal XElementParser this[string NamePath]
            {
                get { return XElementParser.Create(_element, NamePath); }
            }
            /// <summary>
            /// Determines wheter the spesified node exists
            /// </summary>
            /// <param name="NamePath">Name path as dot-separated list of Named Prop elements</param>
            /// <returns>Returns true if specified node exists</returns>
            public bool Exists(string NamePath) { return getElement(_element, NamePath) != null; }
            public int getIntValue(string NamePath, int DefaultValue)
            {
                return getValueAsInt(_element, NamePath, DefaultValue);
            }
            public int? getIntValue(string NamePath)
            {
                return getValueAsInt(_element, NamePath);
            }
            public short? getShortValue(string NamePath)
            {
                return getValueAsShort(_element, NamePath);
            }
            public double getDoubleValue(string NamePath, double DefaultValue)
            {
                return getValueAsDouble(_element, NamePath, DefaultValue);
            }

            public string getStringValue(string NamePath)
            {
                return getValueAsString(_element, NamePath, string.Empty);
            }
            public string getStringValue(string NamePath, string DefaultValue)
            {
                return getValueAsString(_element, NamePath, DefaultValue);
            }
            public bool getBooleanValue(string NamePath, bool DefaultValue)
            {
                return getValueAsBoolean(_element, NamePath, DefaultValue);
            }
            public IEnumerable<XElement> getValues()
            {
                return _element.Elements("Value");
            }
            public IEnumerable<XElement> getValues(string namePath)
            {
                return this[namePath]?.Element.Elements("Value") ?? new List<XElement>();
            }

            public XElement getValue()
            {
                return _element.Element("Value");
            }
            public XElement getValue(string namePath)
            {
                return this[namePath]?.Element.Element("Value");
            }

            #region Static Helper functions
            internal static int getValueAsInt(XElement element, string NamePath, int defaultValue)
            {
                double value;
                if (double.TryParse(getValueAsString(element, NamePath, string.Empty), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out value)) return (int)value;
                else return defaultValue;
            }
            internal static int? getValueAsInt(XElement element, string NamePath)
            {
                double value;
                if (double.TryParse(getValueAsString(element, NamePath, string.Empty), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out value)) return (int)value;
                else return null;
            }
            internal static short? getValueAsShort(XElement element, string NamePath)
            {
                double value;
                if (double.TryParse(getValueAsString(element, NamePath, string.Empty), System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out value)) return (short)value;
                else return null;
            }

            internal static double getValueAsDouble(XElement element, string NamePath, double defaultValue)
            {
                return ParseTSDoubleString(getValueAsString(element, NamePath, string.Empty), defaultValue);
            }
            internal static bool getValueAsBoolean(XElement element, string NamePath, bool defaultValue)
            {
                bool value;
                if (bool.TryParse(getValueAsString(element, NamePath, string.Empty), out value)) return value;
                else return defaultValue;
            }
            internal static string getValueAsString(XElement element, string NamePath, string defaultValue)
            {
                System.Type dataType;
                element = getValueElement(element, NamePath, out dataType);
                return (element != null) ? element.Value : defaultValue;
            }
            internal static object getValue(XElement element, string NamePath, out System.Type dataType)
            {
                element = getValueElement(element, NamePath, out dataType);
                return (element != null) ? element.Value : null;
            }
            internal static XElement getValueElement(XElement element, string NamePath, out System.Type dataType)
            {
                element = getElement(element, NamePath, out dataType);
                if (element != null)
                    return (from el in element.Elements("Value") select el).FirstOrDefault();
                else
                    return null;
            }
            internal static XElement getElement(XElement element, string NamePath, out System.Type dataType)
            {
                element = getElement(element, NamePath);
                if (element != null && element.Attribute("Type") != null)
                    dataType = getDataType(element.Attribute("Type").Value);
                else
                    dataType = null;
                return element;
            }
            internal static XElement getElement(XElement element, string NamePath)
            {
                string[] path = NamePath.Split('.');
                if (path != null && path.Length > 0)
                    foreach (string s in path)
                    {
                        element = (from el in element.Elements("Prop") where el.Attribute("Name")?.Value == s select el).FirstOrDefault();
                        if (element == null) break;
                    }
                return element;
            }
            internal static System.Type getDataType(string TypeString)
            {
                switch (TypeString)
                {
                    case "String": return typeof(string);
                    case "Boolean": return typeof(bool);
                    case "Number": return typeof(double);
                    case "Array": return typeof(Array);
                    case "TEResult": return typeof(TEResult);
                    case "Obj":
                    default:
                        return typeof(object);

                }
            }
            protected static Guid ParseTSGuidString(string s)
            {
                System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex("^ID#:[a-zA-Z0-9/+]{22}");
                if (re.Match(s).Success)
                    return new Guid(System.Convert.FromBase64String(s.Substring(4, 22) + "=="));
                else
                    return Guid.Empty;
            }
            internal static double ParseTSDoubleString(string svalue, double defaultValue)
            {
                double value;
                if (!double.TryParse(svalue, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out value))
                {
                    switch (svalue.Trim().ToLower())
                    {
                        case "infinity":
                        case "inf":
                        case "+infinity":
                        case "+inf":
                        case "positiveinfinity":
                            value = double.PositiveInfinity; break;
                        case "-infinity":
                        case "-inf":
                        case "negativeinfinity":
                            value = double.NegativeInfinity; break;
                        case "nan":
                        case "ind":
                            value = double.NaN; break;
                        default:
                            value = defaultValue; break;
                    }
                }
                return value;
            }
            #endregion
        }
    }
}
