using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Vdl120io
{
    public enum TemperatureUnit
    {
        Farenheit,
        Celsius
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct VdlConfig
    {
        public static VdlConfig Build(byte[] data)
        {
            if (data.Length != 64)
                throw new InvalidDataException("config requires 64 bytes of init data");

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var config = (VdlConfig)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(VdlConfig));
            handle.Free();

            return config;
        }

        public byte[] ToArray()
        {
            //Create empty copy and transfer user data
            var newCfg = new VdlConfig
            {
                //setup set_config values
                _configBegin = 0x0ce,
                _configEnd = 0xce,
                _startCfg = _startCfg,
                NumDataConf = NumDataConf,
                Interval = Interval,
                Time = Time,
                _humidityLowThreshold = _humidityLowThreshold,
                _humidityHighThreshold = _humidityHighThreshold,
                _tempLowThreshold = _tempLowThreshold,
                _tempHighThreshold = _tempHighThreshold,
                _isFarenheit = _isFarenheit,
                _ledConf = _ledConf,
                Name = Name ?? string.Empty
            };


            byte[] arr = new byte[Marshal.SizeOf(newCfg)];

            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(Marshal.SizeOf(this));
                Marshal.StructureToPtr(newCfg, ptr, true);
                Marshal.Copy(ptr, arr, 0, Marshal.SizeOf(newCfg));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return arr;
        }

        private static T ValidateMinMax<T>(T min, T max, T value)
        {
            var comp = Comparer<T>.Default;

            if (comp.Compare(value, min)<0 || comp.Compare(value, max)>0)
                throw new ArgumentOutOfRangeException($"{min}-{max}");

            return value;
        }

        //  0- 3 0xce = set config, 0x00 = logger is active 
        private int _configBegin;

        //  4- 7 number of data configured
        private int _numDataConf;

        public int NumDataConf
        {
            get => _numDataConf;
            set => _numDataConf = ValidateMinMax(1, 16000, value);
        }

        //  8-11 number of data recorded
        private readonly int _numDataRec;

        public int NumDataRec => _numDataRec;
        
        // 12-15 log interval in seconds 
        private int _interval;

        public int Interval
        {
            get => _interval;
            set => _interval = ValidateMinMax(1, 86400, value);
        }

        // 16-19 
        private int TimeYear;
        
        // 20-21 
        private readonly short _padding20;

        // 22-23 
        private ushort _tempLowThreshold;

        public short TempLowThreshold
        {
            get => _tempLowThreshold.Decode();
            set => _tempLowThreshold = ValidateMinMax<short>(-40, 100, value).Encode();
        }
        
        // 24-25 
        private readonly short _padding24;

        // 26-27 
        private ushort _tempHighThreshold;

        public short TempHighThreshold
        {
            get => _tempHighThreshold.Decode();
            set => _tempHighThreshold = ValidateMinMax<short>(-40, 100, value).Encode();
        }

        // 28  start time, local (!) timezone
        private byte _timeMonth;

        // 29
        private byte _timeDay;

        // 30
        private byte _timeHour;

        // 31
        private byte _timeMin;

        // 32
        private byte _timeSec;

        public DateTime Time
        {
            get => new(TimeYear, _timeMonth, _timeDay, _timeHour, _timeMin, _timeSec);
            set
            {
                TimeYear = value.Year;
                _timeMonth = (byte)value.Month;
                _timeDay = (byte)value.Day;

                _timeHour = (byte)value.Hour;
                _timeMin = (byte)value.Minute;
                _timeSec = (byte)value.Second;
            }
        }

        // 33
        private byte _isFarenheit;

        public TemperatureUnit TemperatureUnit
        {
            get => _isFarenheit == 0 ? TemperatureUnit.Celsius : TemperatureUnit.Farenheit;
            set => _isFarenheit = (byte)(value==TemperatureUnit.Celsius? 0 : 1);
        }

        // 34  bit 0: alarm on/off, bits 1-2: 10 (?), bits 3-7: flash frequency in seconds
        private byte _ledConf;

        public bool AlarmEnabled
        {
            get => _ledConf>>7 == 1;
            set => _ledConf = value ? (byte)(_ledConf |(1<<7)) : (byte)(_ledConf & ~(1<<7)); //set highest bit
        }

        public TimeSpan FlashInterval
        {
            get => TimeSpan.FromSeconds(_ledConf & 0x7F);
            set
            {
                if (value < TimeSpan.FromSeconds(1) || value > TimeSpan.FromSeconds(31))
                    throw new ArgumentOutOfRangeException("interval must be [1-31] seconds");

                _ledConf &= 1<<7;//clear all but highest bit
                _ledConf |= (byte)value.TotalSeconds;
            }
        }

        // 35-50 
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
        string _name; // config name. actually just 16 bytes: 35-50 

        public string Name
        {
            get => _name;
            set => _name = value.Substring(0, ValidateMinMax(0, 16, value.Length));
        }

        // 51    
        private byte _startCfg; // 0x02 = start logging immediately; 0x01 = start logging manually 

        public bool AutoStart
        {
            get => _startCfg == 2;
            set => _startCfg = value ? (byte)2 : (byte)1;
        }
        
        // 52-53 
        private readonly short _padding52;

        // 54-55 
        private ushort _humidityLowThreshold;

        public short HumidityLowThreshold
        {
            get => _humidityLowThreshold.Decode();
            set => _humidityLowThreshold = ValidateMinMax<short>(0, 100, value).Encode();
        }

        // 56-57 
        private readonly short _padding56;

        // 58-59 
        private ushort _humidityHighThreshold;

        public short HumidityHighThreshold
        {
            get => _humidityHighThreshold.Decode();
            set => _humidityHighThreshold = ValidateMinMax<short>(0, 100, value).Encode();
        }

        // 60-63
        private short _configEnd; // == config_begin 
    }
}