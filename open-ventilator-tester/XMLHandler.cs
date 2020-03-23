using System;
using System.IO;
using System.Xml.Serialization;
using System.ComponentModel;

namespace open_ventilator_tester
{
    class XMLHandler
    {
        public BindingList<Cyclepoint> DeSerializeCyclepointsFromXML(string XmlFilename)
        {
            try
            {
                if (File.Exists(XmlFilename))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(BindingList<Cyclepoint>));
                    TextReader textReader = new StreamReader(XmlFilename);
                    BindingList<Cyclepoint> points;
                    points = (BindingList<Cyclepoint>)deserializer.Deserialize(textReader);
                    textReader.Close();

                    return points;
                } else {
                    return new BindingList<Cyclepoint>();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void SerializeCyclepoints2XML(BindingList<Cyclepoint> points, string xmlFilename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BindingList<Cyclepoint>));
            TextWriter textWriter = new StreamWriter(xmlFilename);
            serializer.Serialize(textWriter, points);
            textWriter.Close();
        }

    }
}
