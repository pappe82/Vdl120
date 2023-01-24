using System;

namespace Vdl120io
{
    public class VdlMeasurement
    {
        public VdlMeasurement(DateTime timeStamp, float temperature,TemperatureUnit unit, float humidity)
        {
            TimeStamp = timeStamp;
            Temperature = temperature;
            Humidity = humidity;
            TemperatureUnit = unit;
        }
        public DateTime TimeStamp { get; }
        public float Temperature { get; }
        public TemperatureUnit TemperatureUnit { get; }
        public float Humidity { get; }

        public override string ToString()
        {
            return $"{TimeStamp:s} {Temperature} {TemperatureUnit} - {Humidity}%";
        }
    }
}