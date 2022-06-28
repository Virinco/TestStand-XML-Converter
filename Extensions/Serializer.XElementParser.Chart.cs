using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        partial class XElementParser
        {
            /// <summary>
            /// Represents a WATS Chart-data object (TypeName='WATS_Chart_Data'). Inherits from XElementParser, and provides property based access to data.
            /// </summary>
            internal class Chart : XElementParser
            {
                internal Chart(XElement element)
                    : base(element)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                internal Chart(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                //private XElement pi_element;
                public string ChartLabel { get { return getValueAsString(_element, "ChartLabel", string.Empty); } }
                public string Xlabel { get { return getValueAsString(_element, "Xlabel", string.Empty); } }
                public string Xunit { get { return getValueAsString(_element, "Xunit", string.Empty); } }
                public string Ylabel { get { return getValueAsString(_element, "Ylabel", string.Empty); } }
                public string Yunit { get { return getValueAsString(_element, "Yunit", string.Empty); } }
                public string ChartType { get { return getValueAsString(_element, "ChartType", string.Empty); } }
                private ChartPlot[] _plots;
                public ChartPlot[] plots
                {
                    get { if (_plots == null) parsePlots(); return _plots; }
                }
                private void parsePlots()
                {
                    List<ChartPlot> lPlots = new List<ChartPlot>();

                    IEnumerable<XElement> plotnodes = from el in this["Plots"].Element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "WATS_Chart_Plots" && el.Parent.Name == "Value" select el;
                    foreach (XElement plotnode in plotnodes)
                        lPlots.Add(new ChartPlot(plotnode));
                    _plots = lPlots.ToArray();
                }
            }
        }
    }
}
