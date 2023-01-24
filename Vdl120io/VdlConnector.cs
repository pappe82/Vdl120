using System;
using System.IO;
using System.Linq;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace Vdl120io
{
    public class VdlConnector : IDisposable
    {
        private readonly int _readTimeout;
        private readonly int _writeTimeout;

        private readonly UsbDevice _device;
        private readonly UsbEndpointReader _reader;
        private readonly UsbEndpointWriter _writer;
        
        public VdlConnector(UsbRegistry regDevice, int readTimeout, int writeTimeout)
        {
            _readTimeout = readTimeout;
            _writeTimeout = writeTimeout;
            
            regDevice.Open(out _device);

            _reader = _device.OpenEndpointReader((ReadEndpointID)0x81);
            _reader.Flush();

            _writer = _device.OpenEndpointWriter((WriteEndpointID)0x02);
        }

        public void Write(params byte[] message)
        {
            ThrowOnError(_writer.Write(message,_writeTimeout,out int transferred));
            
            if(transferred!=message.Length)
                ThrowOnError(ErrorCode.WriteFailed);
        }

        public byte[] Read(int packetSize)
        {
            var buffer = new byte[packetSize];
            ThrowOnError(_reader.Read(buffer, _readTimeout, out int transferred));

            return buffer.Take(transferred).ToArray();
        }

        private void ThrowOnError(ErrorCode errorCode)
        {
            if (errorCode != ErrorCode.None)
                throw new IOException($"writing failed, error:{errorCode}");
        }
        
        public void Dispose()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            ((IDisposable)_device)?.Dispose();
        }
    }
}