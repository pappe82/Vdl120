using System;
using System.Collections.Generic;

namespace Vdl120io
{
    public class VdlMeasurementReader
    {
        private readonly VdlDevice _device;
        private readonly int _readTimeout;
        private readonly int _writeTimeout;
        private readonly int _tempBias;
        private readonly int _humBias;

        public VdlMeasurementReader(VdlDevice parent, int readTimeout, int writeTimeout, int tempBias, int humBias)
        {
            _device = parent;
            _readTimeout = readTimeout;
            _writeTimeout = writeTimeout;
            _tempBias = tempBias;
            _humBias = humBias;
        }

        private readonly List<VdlMeasurement> _measurements = new();
        public VdlConfig Config { get; private set; }

        public IList<VdlMeasurement> ReadMeasurements(IProgress<int> progress = null)
        {
            _measurements.Clear();

            Config = _device.ReadConfig();

            using (var con = new VdlConnector(_device.RegDevice, _readTimeout, _writeTimeout))
            {
                while (MeasurementsPending())
                {
                    var bytesToRead = CalculateSizeAndSendClusterTransferRequest(con);

                    var dataSet = con.Read(bytesToRead);
                    
                    AddDataPoints(dataSet);

                    progress?.Report(_measurements.Count*100/Config.NumDataRec);
                }
            }

            return _measurements;
        }
        
        private bool MeasurementsPending() => _measurements.Count < Config.NumDataRec;

        private int CalculateSizeAndSendClusterTransferRequest(VdlConnector con)
        {
            var totalBlocks = (short)Math.Ceiling(Config.NumDataRec / 16d);
            var transferredBlocks = _measurements.Count / 16;
            var remainingBlocks = totalBlocks - transferredBlocks;

            var cluster = (byte)(transferredBlocks / 0x40);
            var blocksInClusterToRead = (byte)(remainingBlocks > 0x40 ? 0x40 : remainingBlocks);
            
            con.Write(0, cluster, blocksInClusterToRead);
            var readResponse = con.Read(3);

            return blocksInClusterToRead * 0x40;
        }

        private void AddDataPoints(byte[] dataSet)
        {
            for (int i = 0; i < dataSet.Length && MeasurementsPending(); i+=4)
            {
                var tempRaw = ReadValue(dataSet, i);
                var humRaw = ReadValue(dataSet, i + 2);

                _measurements.Add(
                    new VdlMeasurement(
                        CalcMeasurementTime(),
                        ConvertToValue(tempRaw) + _tempBias,
                        Config.TemperatureUnit,
                        ConvertToValue(humRaw) + _humBias));
            }
        }

        private static int ReadValue(byte[] dataSet, int i) => dataSet[i] + (dataSet[i + 1] << 8);

        private static float ConvertToValue(int rawValue) => rawValue * 0.1f;

        private DateTime CalcMeasurementTime() => Config.Time.AddSeconds(Config.Interval * _measurements.Count);
    }
}