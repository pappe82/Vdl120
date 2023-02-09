using System;
using System.IO;
using System.Linq;
using System.Text;
using Vdl120io;

namespace Vdl120Cli
{
    static class TempEx
    {
        public static string ToText(this TemperatureUnit unit) => unit == TemperatureUnit.Celsius ? "°C" : "F";
    }

    internal class Program
    {
        private static readonly VdlDeviceFactory Factory = new(
            readTimeout:TimeSpan.FromSeconds(5),
            writeTimeout:TimeSpan.FromSeconds(2),
            Properties.Settings.Default.TempBias,
            Properties.Settings.Default.HumBias);

        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                return ShowSyntax();
            }

            try
            {
                var device = Factory.GetDevices().FirstOrDefault();

                if (device == null)
                {
                    return ShowNoDeviceFoundMessage();
                }

                Console.WriteLine($"Found Sensor (VID:{device.Vid:X4} PID:{device.Pid:X4}) - {device.Name}");

                return args[0] switch
                {
                    "-p" => ShowData(device),
                    "-i" => ShowConfig(device),
                    "-s" => SaveData(device, args),
                    "-c" => Configure(device, args),
                    _ => ShowInvalidCommandLine(args[0])
                };
            }
            catch
            {
                Console.WriteLine("Unexpected error occurred. try disconnecting device and retry after plug in.");
                return -4;
            }
        }

        private static int ShowNoDeviceFoundMessage()
        {
            Console.WriteLine("No Sensor found. Check if device:");
            Console.WriteLine("- is plugged in correctly");
            Console.WriteLine("- is shown in device manager");
            Console.WriteLine("- uses the WinUSB driver provided with this software.");
            return -1;
        }

        private static int ShowSyntax()
        {
            Console.WriteLine("Voltcraft DL-120TH Management tool v1.0 (c) farcast.de");
            Console.WriteLine();
            Console.WriteLine("vdl120cli [-c <name> <number> <interval> | -i | -p | -s <filename>]");
            Console.WriteLine(" -c  configure new measurement cycle");
            Console.WriteLine(" -i  show device configuration");
            Console.WriteLine(" -p  print measurements ");
            Console.WriteLine(" -s  save measurements as csv file");
            Console.WriteLine();
            Console.WriteLine("Note: all operations stop the current active measurement cycle.");
            return 0;
        }

        private static int ShowData(VdlDevice device)
        {
            Console.WriteLine("Reading measurements");
            var reader = Factory.CreateReader(device);

            var mess = reader.ReadMeasurements(new Progress<int>(ShowProgress));
            foreach (var measurement in mess)
            {
                Console.WriteLine(
                    $"{measurement.TimeStamp:s}: " +
                    $"{measurement.Temperature:F1} {measurement.TemperatureUnit.ToText()} - {measurement.Humidity:F1} %");
            }

            Console.WriteLine("\nTotal count: " + mess.Count);

            return 0;
        }

        private static void ShowProgress(int progress)
        {
            Console.Write($"{progress}%\r");
        }

        private static int ShowConfig(VdlDevice device)
        {
            var config = device.ReadConfig();

            Console.WriteLine("last measurement start time: " + config.Time.ToString("s"));
            Console.WriteLine(" measurement name: " + config.Name);
            Console.WriteLine(" measurement interval: " + config.Interval);
            Console.WriteLine(" maximum measurements: " + config.NumDataConf);
            Console.WriteLine(" measurement count: " + config.NumDataRec);
            Console.WriteLine(" temperature unit: " + config.TemperatureUnit.ToText());
            Console.WriteLine();
            Console.WriteLine("alert enabled: " + config.AlarmEnabled);
            Console.WriteLine("alert thresholds:");
            Console.WriteLine(" humidity high: "+config.HumidityHighThreshold);
            Console.WriteLine(" humidity low: " + config.HumidityLowThreshold);
            Console.WriteLine(" temperature high: " + config.TempHighThreshold);
            Console.WriteLine(" temperature low:" + config.TempLowThreshold);
            Console.WriteLine();
            Console.WriteLine("LED flash interval: " + (int)config.FlashInterval.TotalSeconds);
            Console.WriteLine("automatic measurement start:" + config.AutoStart);

            return 0;
        }

        
        

        private static int SaveData(VdlDevice device, string[] args)
        {
            if (args.Length == 2)
            {
                using var writer = new StreamWriter(new FileStream(args[1], FileMode.Create), Encoding.UTF8);

                Console.WriteLine("Saving measurements to " + args[1]);

                var reader = Factory.CreateReader(device);
                var measurements = reader.ReadMeasurements(new Progress<int>(ShowProgress));
                //reader.Config is only available after reading the measurements

                writer.WriteLine("Measurement|"+reader.Config.Name);
                writer.WriteLine("Start time|" + reader.Config.Time.ToString("s"));
                writer.WriteLine("Total Count|" + measurements.Count);
                writer.WriteLine();
                writer.WriteLine($"Timestamp|Temp [{reader.Config.TemperatureUnit.ToText()}]|rH [%]");

                foreach (var m in measurements)
                {
                    writer.WriteLine($"{m.TimeStamp:s}|{m.Temperature:F1}|{m.Humidity:F1}");
                }

                return 0;
            }

            return InvalidOptionCount();
        }

        static int InvalidOptionCount()
        {
            Console.WriteLine("invalid option count");
            return -3;
        }

        private static int Configure(VdlDevice device, string[] args)
        {
            if (args.Length == 4)
            {
                var config = new VdlConfig
                {
                    AlarmEnabled = false,
                    FlashInterval = TimeSpan.FromSeconds(10),
                    HumidityHighThreshold = 100,
                    HumidityLowThreshold = 0,
                    TempHighThreshold = 70,
                    TempLowThreshold = 0,
                    Time = DateTime.Now,
                    AutoStart = true,
                };

                if (!TrySetValue(args[2], "number", v =>
                    {
                        config.NumDataConf = v;
                    })) return -3;

                if (!TrySetValue(args[3], "interval", v =>
                    {
                        config.Interval = v;
                    })) return -3;

                if (!TrySetValue("0", "name length", _ =>
                    {
                        config.Name = args[1];
                    })) return -3;

                device.SetConfig(config);

                Console.WriteLine("Sensor configured, unplug to start measurement");

                return 0;
            }

            return InvalidOptionCount();
        }

        private static bool TrySetValue(string input, string item, Action<short> action)
        {
            if (short.TryParse(input, out var number))
            {
                try
                {
                    action(number);
                    return true;
                }
                catch (ArgumentOutOfRangeException e)
                {
                    Console.WriteLine(item + " must be in range " + e.ParamName);
                }
            }
            else
            {
                Console.WriteLine("invalid argument value: " + input);
            }

            return false;
        }

        private static int ShowInvalidCommandLine(string option)
        {
            Console.WriteLine("Unknown option: " + option);
            return -2;
        }
    }
}
