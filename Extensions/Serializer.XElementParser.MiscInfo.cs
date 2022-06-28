using System.Xml.Linq;


namespace TestStandXMLConverter
{
    partial class TestStandXMLConverter
    {
        partial class XElementParser
        {
            /// <summary>
            /// Represents a WATS PartInfo object (TypeName='ET_UUT_Part_Info'). Inherits from XElementParser, and provides property based access to data.
            /// </summary>
            internal class MiscInfo : XElementParser
            {
                internal MiscInfo(XElement element)
                    : base(element)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_Misc_UUT_Info" select el).FirstOrDefault();
                }
                internal MiscInfo(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_Misc_UUT_Info" select el).FirstOrDefault();
                }
                //private XElement pi_element;
                public string Description { get { return getValueAsString(_element, "Description", string.Empty); } }
                public string StringValue { get { return getValueAsString(_element, "Data_String", string.Empty); } }
                public short? NumericValue { get { return getValueAsShort(_element, "Data_Numeric"); } }
            }
        }
    }
}
