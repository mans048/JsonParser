using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace GliderTechnology
{
    public class SensorReading
    {
        public string Id { get; set; }
        public List< string> Readings { get; set; }
    }
    public class Root
    {
        public List<SensorReading> sensor_readings { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            
            //Getting the command line arguments
            string[] arg = Environment.GetCommandLineArgs();
            string filepathname;
            //standard checks for the expected number of arguments and if help has been asked
            if (arg.Length>2)
            {
                Console.Write("Only 1 argument expected");
                Environment.Exit(0);
            }
            if (arg[1].ToString() == "?" || arg[1].ToString().ToUpper()=="HELP")
            {
                Console.Write("Expecting filepath and name as input only. The JSON output name will be placed in the same directory with a JSON extension instead of CSV");
                Environment.Exit(0);
            }
            //Checking that the file extension is CSV. I know it can be something else etc but easier to force people to use the write ones instead of garbage in
            if (arg[1].Substring(arg[1].Length-3,3).ToString().ToUpper()!="CSV")
            {
                Console.Write("Expecting a filepath and name with a CSV extension");
                Environment.Exit(0);
            }
            // we can check the file exists before loading it for parsing etc
            if (File.Exists(arg[1].ToString())==false)
            {
                Console.Write("The file does not exist. Plese specify a valid CSV file");
                Environment.Exit(0);
            }
            filepathname = string.Concat(arg[1].ToString().Substring(0,arg[1].Length-3),"json");
            // load all the values into a list array
            try
            {
                List<string> SensorID = new List<String>();
                List<double> TempReading = new List<double>();
                List<string> ReadDateTime = new List<string>();
                List<string> TempFormat = new List<String>();
                string[] lines = System.IO.File.ReadAllLines(arg[1]);
                lines = lines.Skip(1).ToArray();

                foreach (string line in lines)
                {

                    string[] values = line.Split(',');
                    if (values.Length >= 4)
                    {
                        SensorID.Add(values[0].ToString().Replace("\"", ""));
                        TempReading.Add(double.Parse(values[1].Replace("\"", "")));
                        ReadDateTime.Add(values[2].ToString().Replace("\"", ""));
                        TempFormat.Add(values[3].ToString().Replace("\"", ""));
                    }

                }
                string[] firstSensorID = SensorID.ToArray();
                double[] firstTempReading = TempReading.ToArray();
                string[] firstReadDateTime = ReadDateTime.ToArray();
                string[] firstTempFormat = TempFormat.ToArray();
                try
                {
                    //next step is to sort the readings into a sensor order before doing any calculations
                    string Tmp1;
                    double Tmp2;
                    string Tmp3;
                    string Tmp4;
                    for (int write = 0; write < firstSensorID.Length; write++)
                    {
                        for (int sort = 0; sort < firstSensorID.Length - 1; sort++)
                        {
                            //using a string comparison for the sensor id value as it is not defined as a numeric type value.
                            if (String.Compare(firstSensorID[sort].ToUpper(), firstSensorID[sort + 1].ToUpper()) > 0)
                            {
                                Tmp1 = firstSensorID[sort + 1];
                                firstSensorID[sort + 1] = firstSensorID[sort];
                                firstSensorID[sort] = Tmp1;
                                Tmp2 = firstTempReading[sort + 1];
                                firstTempReading[sort + 1] = firstTempReading[sort];
                                firstTempReading[sort] = Tmp2;
                                Tmp3 = firstReadDateTime[sort + 1];
                                firstReadDateTime[sort + 1] = firstReadDateTime[sort];
                                firstReadDateTime[sort] = Tmp3;
                                Tmp4 = firstTempFormat[sort + 1];
                                firstTempFormat[sort + 1] = firstTempFormat[sort];
                                firstTempFormat[sort] = Tmp4;
                            }
                        }
                    }
                    // I could be clever and try to do the conversion of the temp values within the loop above but i prefer breaking things down into their own operations rather than cram everything into a single chunk
                    // which makes it harder to understand why/how things are happenings
                    try
                    {
                        for (int write = 0; write < firstSensorID.Length; write++)
                        {
                            //Temp conversion
                            switch (firstTempFormat[write])
                            {
                                case "F":
                                    firstTempReading[write] = ((firstTempReading[write] - 32) * 5 / 9);
                                    break;
                                case "K":
                                    firstTempReading[write] = firstTempReading[write] - 273.15;
                                    break;
                                default:
                                    firstTempReading[write] = firstTempReading[write];
                                    break;
                            }
                            //Date conversion
                            var time = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(firstReadDateTime[write]));

                            // Convert back to ISO string
                            var isoString = time.ToString("yyyy-MM-ddTHH:mm:ssGMT");
                            firstReadDateTime[write] = isoString.ToString();
                            //now I've got the file loaded we can now do some other clever stuff to it.
                        }

                        //get the ID of the first sensor value
                        string ID = firstSensorID[0].ToString();
                        string ReadData = String.Concat(firstReadDateTime[0].ToString(), ",", firstTempReading[0].ToString());
                        Root sensors = new Root();
                        SensorReading sensorreading = new SensorReading();
                        sensorreading.Id = ID.ToString();
                        for (int write = 1; write < firstSensorID.Length; write++)
                        {
                            if (String.Compare(ID.ToUpper(),firstSensorID[write].ToString().ToUpper())==0)
                            {
                                ReadData = String.Concat(ReadData, "|",firstReadDateTime[write].ToString(), ",", firstTempReading[write].ToString());
                            }
                            else 
                            {
                                sensorreading.Readings = ReadData.Split('|').ToList();
                                //sensors.sensor_readings.Add(sensorreading);
                                ID = firstSensorID[write].ToString();
                            }
                        }
                        //now I have the class populated I can write the file
                        string stringjson = JsonConvert.SerializeObject(sensorreading);
                        File.WriteAllText(filepathname, stringjson);
                    }
                    catch
                    {
                        Console.Write("Error sorting the sensors into an order");
                        Environment.Exit(0);
                    }
                }
                catch
                {
                    Console.Write("Error converting reading values into Celcius");
                    Environment.Exit(0);
                }
            }
            catch
            {
                //yes I know I could do more to be more specific about this but if it is not loading into the list properly then there is something wrong with the format
                Console.Write("File did not match expected 4 columns of Sensor,Reading,Time and Format or the data was not consistent with expected inputs types (string,integer,integer,string)");
                Environment.Exit(0);
            }
        }
    }
}
