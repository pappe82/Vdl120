using System.IO;
using LibUsbDotNet.Main;

namespace Vdl120io
{
    public class VdlDevice
    {
        internal readonly UsbRegistry RegDevice;
        private readonly int _readTimeout;
        private readonly int _writeTimeout;

        public VdlDevice(UsbRegistry regDevice, int readTimeout, int writeTimeout)
        {
            RegDevice = regDevice;
            _readTimeout = readTimeout;
            _writeTimeout = writeTimeout;
        }

        public int Vid => RegDevice.Vid;

        public int Pid => RegDevice.Pid;

        public string Name => RegDevice.Name;

        public VdlConfig ReadConfig()
        {
            using var con = new VdlConnector(RegDevice, _readTimeout, _writeTimeout);

            con.Write(0x00, 0x10, 0x01);

            var readResponse = con.Read(3);

            var configBytes = con.Read(64);

            return VdlConfig.Build(configBytes);
        }

        public void SetConfig(VdlConfig config)
        {
            using (var con = new VdlConnector(RegDevice, _readTimeout, _writeTimeout))
            {
                con.Write(0x01, 0x40, 0x00);

                con.Write(config.ToArray());

                var setResponse = con.Read(3);

                if (setResponse.Length != 1 || setResponse[0] != 0xff)
                    throw new IOException($"could not set config, error {setResponse[0]:X2}");
            }
        }
    }
}