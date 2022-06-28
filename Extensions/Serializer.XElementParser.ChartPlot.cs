using System;
using System.Linq;
using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        partial class XElementParser
        {
            /// <summary>
            /// Represents a WATS Chart-plot object (TypeName='WATS_Chart_Plots'). Inherits from XElementParser, and provides property based access to data.
            /// </summary>
            internal class ChartPlot : XElementParser
            {
                internal ChartPlot(XElement element)
                    : base(element)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                internal ChartPlot(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                //private XElement pi_element;
                public string PlotName { get { return getValueAsString(_element, "PlotName", string.Empty); } }
                public string PlotDataType { get { return getValueAsString(_element, "PlotDataType", string.Empty); } }
                public byte[] PlotData
                {
                    get
                    {
                        if (!_isParsed) parsePlotData();
                        byte[] buffer = new byte[_plotdata.Length * 8]; //[_plotdata.GetLength(0) * _plotdata.GetLength(1) * 8];
                        int n = 0;
                        foreach (double value in _plotdata)
                        {
                            Array.Copy(BitConverter.GetBytes(value), 0, buffer, (n++) * 8, 8);
                        }
                        return buffer;
                    }
                }
                public double[,] PlotDataAs2DDbl
                {
                    get
                    {
                        if (!_isParsed) parsePlotData();
                        return _plotdata;
                    }
                }
                private bool _isParsed = false;
                private double[,] _plotdata;
                private int _iD1Lb;
                public int LBoundD1 { get { if (!_isParsed) parsePlotData(); return _iD1Lb; } }
                private int _iD2Lb;
                public int LBoundD2 { get { if (!_isParsed) parsePlotData(); return _iD2Lb; } }
                private int _iD1Hb;
                public int HBoundD1 { get { if (!_isParsed) parsePlotData(); return _iD1Hb; } }
                private int _iD2Hb;
                public int HBoundD2 { get { if (!_isParsed) parsePlotData(); return _iD2Hb; } }

                private void parsePlotData()
                {
                    _isParsed = true;
                    System.Text.RegularExpressions.Regex re = new System.Text.RegularExpressions.Regex(@"(?:\[(\d*)\]){2}");
                    XElement pd = getElement(_element, "PlotData");
                    System.Text.RegularExpressions.Match mLBound = re.Match(pd.Attribute("LBound").Value);
                    System.Text.RegularExpressions.Match mHBound = re.Match(pd.Attribute("HBound").Value);
                    //mHBound.Groups[1].Captures[0..1]
                    if (mLBound.Groups.Count < 2 || mHBound.Groups.Count < 2) { _iD1Lb = 1; _iD2Lb = 1; _iD1Hb = 0; _iD2Hb = 0; _plotdata = new double[0, 0]; return; }
                    if (mLBound.Groups[1].Captures.Count != 2 || mHBound.Groups[1].Captures.Count != 2) { _iD1Lb = 1; _iD2Lb = 1; _iD1Hb = 0; _iD2Hb = 0; _plotdata = new double[0, 0]; return; }
                    _iD1Lb = Int32.Parse(mLBound.Groups[1].Captures[0].Value);
                    _iD2Lb = Int32.Parse(mLBound.Groups[1].Captures[1].Value);
                    _iD1Hb = Int32.Parse(mHBound.Groups[1].Captures[0].Value);
                    _iD2Hb = Int32.Parse(mHBound.Groups[1].Captures[1].Value);
                    _plotdata = new double[_iD2Hb - _iD2Lb + 1, _iD1Hb - _iD1Lb + 1];
                    var x = from val in pd.Elements("Value") select val;
                    foreach (XElement v in x)
                    {
                        // Parse ID:
                        string id = v.Attribute("ID").Value;
                        System.Text.RegularExpressions.Match m = re.Match(id);
                        if (m.Groups.Count < 2 || m.Groups[1].Captures.Count != 2) continue;
                        int iD1 = Int32.Parse(m.Groups[1].Captures[0].Value);
                        int iD2 = Int32.Parse(m.Groups[1].Captures[1].Value);
                        _plotdata[iD2 - _iD2Lb, iD1 - _iD1Lb] = ParseTSDoubleString(v.Value, double.NaN); //Double.Parse(v.Value, System.Globalization.NumberFormatInfo.InvariantInfo);
                    }
                }
            }
        }
    }
}
