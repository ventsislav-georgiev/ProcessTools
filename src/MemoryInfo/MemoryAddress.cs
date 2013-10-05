using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using WinAPI;

namespace MemoryInfo
{
    public enum ValueType
    {
        ArrayOfBytes,
        Short,
        Integer,
        Long,
        Float,
        Double,
        String
    }

    public class MemoryAddress : INotifyPropertyChanged
    {
        #region Properties

        public Memory Memory { get; set; }

        public Structs.MemoryBasicInformation MemoryRegion { get; set; }

        public ulong Offset { get; set; }

        public string OffsetString
        {
            get
            {
                return Offset.ToString("x");
            }
        }

        public uint Length { get; set; }

        public bool IsUnicode { get; set; }

        private string _value;

        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = ReadValueFromProcessMemory();
                OnPropertyChanged("Value");
            }
        }

        public string ValueSetter
        {
            set
            {
                _value = value;
                SetValue(value);
                OnPropertyChanged("Value");
            }
        }

        public string MemoryType { get; private set; }

        public ValueType Type { get; set; }

        #endregion Properties

        #region Constructors

        public MemoryAddress(Memory memory, Structs.MemoryBasicInformation memoryRegion, ulong offset, ValueType type, uint length, bool isUnicode)
        {
            Memory = memory;
            MemoryRegion = memoryRegion;
            Offset = offset;
            Type = type;
            Length = length;
            IsUnicode = isUnicode;
            MemoryType = memoryRegion.Protect.ToString();
            _value = ReadValueFromProcessMemory();
        }

        #endregion Constructors

        #region ValueTypes

        private byte[] ArrayOfBytes
        {
            get
            {
                return ((byte[])GetValue());
            }
        }

        private short Short
        {
            get
            {
                return ((short)GetValue());
            }
        }

        private int Integer
        {
            get
            {
                return ((int)GetValue());
            }
        }

        private long Long
        {
            get
            {
                return ((long)GetValue());
            }
        }

        private float Float
        {
            get
            {
                return ((float)GetValue());
            }
        }

        private double Double
        {
            get
            {
                return ((double)GetValue());
            }
        }

        private string String
        {
            get
            {
                return ((string)GetValue());
            }
        }

        #endregion ValueTypes

        #region Get, Set

        public string ReadValueFromProcessMemory()
        {
            var value = string.Empty;
            switch (Type)
            {
                case ValueType.ArrayOfBytes:
                    char[] c = new char[ArrayOfBytes.Length * 2];
                    int b;
                    for (int i = 0; i < ArrayOfBytes.Length; i++)
                    {
                        b = ArrayOfBytes[i] >> 4;
                        c[i * 2] = (char)(55 + b + (((b - 10) >> 31) & -7));
                        b = ArrayOfBytes[i] & 0xF;
                        c[i * 2 + 1] = (char)(55 + b + (((b - 10) >> 31) & -7));
                    }
                    value = new string(c);
                    break;

                case ValueType.Short:
                    value = Short.ToString("x");
                    break;

                case ValueType.Integer:
                    value = Integer.ToString();
                    break;

                case ValueType.Long:
                    value = Long.ToString();
                    break;

                case ValueType.Float:
                    value = Float.ToString();
                    break;

                case ValueType.Double:
                    value = Double.ToString();
                    break;

                case ValueType.String:
                    value = String;
                    break;
            }
            return value;
        }

        private void SetValue(string value)
        {
            byte[] data = null;

            switch (Type)
            {
                case ValueType.ArrayOfBytes:
                    int NumberChars = value.Length / 2;
                    byte[] bytes = new byte[NumberChars];
                    using (var sr = new StringReader(value))
                    {
                        for (int i = 0; i < NumberChars; i++)
                            bytes[i] =
                            Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
                    }
                    data = bytes;
                    break;

                case ValueType.Short:
                    data = BitConverter.GetBytes(short.Parse(value));
                    break;

                case ValueType.Integer:
                    data = BitConverter.GetBytes(int.Parse(value));
                    break;

                case ValueType.Long:
                    data = BitConverter.GetBytes(long.Parse(value));
                    break;

                case ValueType.Float:
                    data = BitConverter.GetBytes(float.Parse(value));
                    break;

                case ValueType.Double:
                    data = BitConverter.GetBytes(double.Parse(value));
                    break;

                case ValueType.String:
                    var buffer = new List<byte>();
                    if (IsUnicode)
                        buffer.AddRange(Encoding.Unicode.GetBytes(value));
                    else
                        buffer.AddRange(Encoding.Default.GetBytes(value));

                    buffer.AddRange(BitConverter.GetBytes('\0'));
                    data = buffer.ToArray();
                    break;
            }

            Length = (uint)data.Length;
            var isReadOnlyMemory = MemoryRegion.Protect == Enums.Protection.PAGE_READONLY;
            if (isReadOnlyMemory)
                Memory.WriteToProtectedMemory(MemoryRegion, this.Offset, data, Length);
            else
                Memory.Write(this.Offset, data, Length);
        }

        private object GetValue()
        {
            object value = null;
            var data = Memory.Read(this.Offset, this.Length);

            switch (Type)
            {
                case ValueType.ArrayOfBytes:
                    value = data;
                    break;

                case ValueType.Short:
                    value = BitConverter.ToInt16(data, 0);
                    break;

                case ValueType.Integer:
                    value = BitConverter.ToInt32(data, 0);
                    break;

                case ValueType.Long:
                    value = BitConverter.ToInt64(data, 0);
                    break;

                case ValueType.Float:
                    value = BitConverter.ToSingle(data, 0);
                    break;

                case ValueType.Double:
                    value = BitConverter.ToDouble(data, 0);
                    break;

                case ValueType.String:
                    string stringValue = null;
                    if (IsUnicode)
                        stringValue = Encoding.Unicode.GetString(data);
                    else
                        stringValue = Encoding.Default.GetString(data);

                    var nullZeroIndex = stringValue.IndexOf('\0');
                    if (nullZeroIndex != -1)
                        stringValue = stringValue.Remove(nullZeroIndex);
                    value = stringValue;
                    break;
            }
            return value;
        }

        #endregion Get, Set

        #region PropertyChangedNotification

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string value)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(value));
            }
        }

        #endregion PropertyChangedNotification
    }
}