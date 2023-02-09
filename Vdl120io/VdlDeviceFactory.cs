using System;
using System.Collections.Generic;
using System.Linq;
using LibUsbDotNet;

namespace Vdl120io
{
    public class VdlDeviceFactory
    {
        private readonly int _tempBias;
        private readonly int _humBias;
        private readonly int _readTimeout;
        private readonly int _writeTimeout;

        public VdlDeviceFactory(TimeSpan readTimeout, TimeSpan writeTimeout, int tempBias, int humBias)
        {
            _tempBias = tempBias;
            _humBias = humBias;
            _readTimeout = (int)readTimeout.TotalMilliseconds;
            _writeTimeout = (int)writeTimeout.TotalMilliseconds;
        }
        
        public IList<VdlDevice> GetDevices()
        {
            return UsbDevice.AllDevices.Select(d => new VdlDevice(d, _readTimeout, _writeTimeout)).ToList();
        }

        public VdlMeasurementReader CreateReader(VdlDevice device) => new(device, _readTimeout, _writeTimeout, _tempBias, _humBias);
    }
}