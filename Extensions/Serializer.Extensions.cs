namespace TestStandXMLConverter
{
    public static class SerializerExtensions
    {
        internal static TestStandXMLConverter.XElementParser.Chart GetChartData(this TestStandXMLConverter.XElementParser.TEResult step)
        {
            return new TestStandXMLConverter.XElementParser.Chart(step["Chart"].Element);
        }

    }
}
