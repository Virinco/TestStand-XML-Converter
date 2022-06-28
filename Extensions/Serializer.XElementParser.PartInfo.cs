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
            internal class PartInfo : XElementParser
            {
                internal PartInfo(XElement element)
                    : base(element)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                internal PartInfo(XElement element, string NamePath)
                    : base(element, NamePath)
                {
                    //pi_element = (from el in _element.Descendants("Prop") where el.Attribute("Type").Value == "Obj" && el.Attribute("TypeName").Value == "ET_UUT_Part_Info" select el).FirstOrDefault();
                }
                //private XElement pi_element;
                public string PartType { get { return getValueAsString(_element, "Part_Type", string.Empty); } }
                public string PartNumber { get { return getValueAsString(_element, "Part_Number", string.Empty); } }
                public string SerialNumber { get { return getValueAsString(_element, "Part_Serial_Number", string.Empty); } }
                public string RevisionNumber { get { return getValueAsString(_element, "Part_Revision_Number", string.Empty); } }
            }
        }
    }
}
