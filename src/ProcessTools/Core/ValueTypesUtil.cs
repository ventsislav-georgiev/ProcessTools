using System;
using System.Text;

namespace ProcessTools.Core
{
    public static class ValueTypesUtil
    {
        public static void GetBytesFromStringValueByType(string valueInString, string valueTypeInString, bool isUnicode, out byte[] valueInBytes, out MemoryInfo.ValueType valueType)
        {
            valueInBytes = null;
            valueType = MemoryInfo.ValueType.ArrayOfBytes;

            switch (valueTypeInString)
            {
                case "ArrayOfBytes":
                    {
                        valueType = MemoryInfo.ValueType.ArrayOfBytes;
                        var bytes = valueInString.Split(' ');
                        valueInBytes = new byte[bytes.Length];
                        for (int byteIndex = 0; byteIndex < bytes.Length; byteIndex++)
                        {
                            var value = byte.Parse(bytes[byteIndex]);
                            valueInBytes[byteIndex] = value;
                        }
                        break;
                    }
                case "Short":
                    {
                        valueType = MemoryInfo.ValueType.Short;
                        var value = short.Parse(valueInString);
                        valueInBytes = BitConverter.GetBytes(value);
                        break;
                    }
                case "Integer":
                    {
                        valueType = MemoryInfo.ValueType.Integer;
                        var value = int.Parse(valueInString);
                        valueInBytes = BitConverter.GetBytes(value);
                        break;
                    }
                case "Long":
                    {
                        valueType = MemoryInfo.ValueType.Long;
                        var value = long.Parse(valueInString);
                        valueInBytes = BitConverter.GetBytes(value);
                        break;
                    }
                case "Float":
                    {
                        valueType = MemoryInfo.ValueType.Float;
                        var value = float.Parse(valueInString);
                        valueInBytes = BitConverter.GetBytes(value);
                        break;
                    }
                case "Double":
                    {
                        valueType = MemoryInfo.ValueType.Double;
                        var value = double.Parse(valueInString);
                        valueInBytes = BitConverter.GetBytes(value);
                        break;
                    }
                case "String":
                    {
                        valueType = MemoryInfo.ValueType.String;
                        if (isUnicode)
                            valueInBytes = Encoding.Unicode.GetBytes(valueInString);
                        else
                            valueInBytes = Encoding.Default.GetBytes(valueInString);
                        break;
                    }
            }
        }
    }
}