using System;
using System.Text;

namespace MemoryInfo
{
    /// <summary>
    ///   Contains methods to interact with the memory at the provided address.
    /// </summary>
    public class MemoryWalker
    {
        /// <summary>
        ///   Contains an instance of a memory object to use
        /// </summary>
        protected Memory _hMemory = null;

        /// <summary>
        ///   Base address to start the calculation with.
        /// </summary>
        protected ulong _iAddress = 0;

        /// <summary>
        ///   Creates a new walker object with the provided memory handle.
        /// </summary>
        /// <param name="hMemory">Contains the memory object to use.</param>
        /// <param name="iAddress">Contains the address to set.</param>
        public MemoryWalker(Memory hMemory, ulong iAddress = 0)
        {
            _hMemory = hMemory;
            _iAddress = iAddress;
        }

        /// <summary>
        ///   Create a new walker object pointed at the specified address.
        /// </summary>
        /// <param name="iAddress">Contains the address to set.</param>
        public virtual MemoryWalker Create(uint iAddress)
        {
            return new MemoryWalker(_hMemory, iAddress);
        }

        /// <summary>
        ///   Read one byte from memory and return it as a boolean.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public bool GetBoolean(ulong iAddress)
        {
            return Convert.ToBoolean(GetByte(iAddress));
        }

        /// <summary>
        ///   Read one byte from memory and return it as a boolean.
        /// </summary>
        public bool GetBoolean()
        {
            return GetBoolean(_iAddress);
        }

        /// <summary>
        ///   Read one byte from memory and return it as a single byte.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public byte GetByte(ulong iAddress)
        {
            byte[] zBuffer = GetBytes(iAddress, 1);
            return zBuffer[0];
        }

        /// <summary>
        ///   Read one byte from memory and return it as a single byte.
        /// </summary>
        public byte GetByte()
        {
            return GetByte(_iAddress);
        }

        /// <summary>
        ///   Read all the specified bytes and returns it as a byte array.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        /// <param name="iSize">Contains the number of bytes to read.</param>
        public virtual byte[] GetBytes(ulong iAddress, uint iSize)
        {
            byte[] bRead = _hMemory.Read(iAddress, iSize);
            if (bRead == null) return new byte[iSize];
            return bRead;
        }

        /// <summary>
        ///   Read all the specified bytes and returns it as a byte array.
        /// </summary>
        /// <param name="iSize">Contains the number of bytes to read.</param>
        public byte[] GetBytes(uint iSize)
        {
            return GetBytes(_iAddress, iSize);
        }

        /// <summary>
        ///   Read one byte from memory and return it as a 32-bits character.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public char GetChar(ulong iAddress)
        {
            return (char)GetByte(iAddress);
        }

        /// <summary>
        ///   Read one byte from memory and return it as a 32-bits character.
        /// </summary>
        public char GetChar()
        {
            return GetChar(_iAddress);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a 64-bits double.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public double GetDouble(ulong iAddress)
        {
            return BitConverter.ToDouble(GetBytes(iAddress, 8), 0);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a 64-bits double.
        /// </summary>
        public double GetDouble()
        {
            return GetDouble(_iAddress);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as a float.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public float GetFloat(ulong iAddress)
        {
            return BitConverter.ToSingle(GetBytes(iAddress, 4), 0);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as a float.
        /// </summary>
        public float GetFloat()
        {
            return GetFloat(_iAddress);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as an integer.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public int GetInteger(ulong iAddress)
        {
            return BitConverter.ToInt32(GetBytes(iAddress, 4), 0);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as an integer.
        /// </summary>
        public int GetInteger()
        {
            return GetInteger(_iAddress);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a long.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public long GetLong(ulong iAddress)
        {
            return BitConverter.ToInt64(GetBytes(iAddress, 8), 0);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a long.
        /// </summary>
        public long GetLong()
        {
            return GetLong(_iAddress);
        }

        /// <summary>
        ///   Read two bytes from memory and return it as a short.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public short GetShort(ulong iAddress)
        {
            return BitConverter.ToInt16(GetBytes(iAddress, 2), 0);
        }

        /// <summary>
        ///   Read two bytes from memory and return it as a short.
        /// </summary>
        public short GetShort()
        {
            return GetShort(_iAddress);
        }

        /// <summary>
        ///   Read all the specified string length, when using Unicode the amount of bytes is multiplied.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        /// <param name="iSize">Contains the number of bytes to read.</param>
        /// <param name="bUnicode">Indicates whether or not the string is unicode.</param>
        public string GetString(ulong iAddress, uint iSize, bool bUnicode = false)
        {
            byte[] zBuffer = GetBytes(iAddress, (bUnicode) ? iSize * 2 : iSize);
            string zString = (bUnicode) ? Encoding.Unicode.GetString(zBuffer) : Encoding.ASCII.GetString(zBuffer);
            if (zString.IndexOf('\0') != -1) zString = zString.Remove(zString.IndexOf('\0'));
            return zString;
        }

        /// <summary>
        ///   Read all the specified string length, when using Unicode the amount of bytes is multiplied.
        /// </summary>
        /// <param name="iSize">Contains the number of bytes to read.</param>
        /// <param name="bUnicode">Indicates whether or not the string is unicode.</param>
        public string GetString(uint iSize, bool bUnicode = false)
        {
            return GetString(_iAddress, iSize, bUnicode);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as an integer.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public uint GetUnsignedInteger(ulong iAddress)
        {
            return BitConverter.ToUInt32(GetBytes(iAddress, 4), 0);
        }

        /// <summary>
        ///   Read four bytes from memory and return it as an integer.
        /// </summary>
        public uint GetUnsignedInteger()
        {
            return GetUnsignedInteger(_iAddress);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a long.
        /// </summary>
        /// <param name="iAddress">Contains the address to read.</param>
        public ulong GetUnsignedLong(ulong iAddress)
        {
            return BitConverter.ToUInt64(GetBytes(iAddress, 8), 0);
        }

        /// <summary>
        ///   Read eight bytes from memory and return it as a long.
        /// </summary>
        public ulong GetUnsignedLong()
        {
            return GetUnsignedLong(_iAddress);
        }

        /// <summary>
        ///   Write one byte to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="bBuffer">Contains the byte(s) to be written.</param>
        public bool SetByte(ulong iAddress, byte bBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(bBuffer), 1);
        }

        /// <summary>
        ///   Write one byte to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="bBuffer">Contains the byte(s) to be written.</param>
        public bool SetByte(byte bBuffer)
        {
            return SetByte(_iAddress, bBuffer);
        }

        /// <summary>
        ///   Write one byte to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="cBuffer">Contains the byte(s) to be written.</param>
        public bool SetChar(ulong iAddress, char cBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(cBuffer), 1);
        }

        /// <summary>
        ///   Write one byte to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="">Contains the byte(s) to be written.</param>
        public bool SetChar(char cBuffer)
        {
            return SetChar(_iAddress, cBuffer);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="dBuffer">Contains the byte(s) to be written.</param>
        public bool SetDouble(ulong iAddress, double dBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(dBuffer), 8);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="dBuffer">Contains the byte(s) to be written.</param>
        public bool SetDouble(double dBuffer)
        {
            return SetDouble(_iAddress, dBuffer);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="fBuffer">Contains the byte(s) to be written.</param>
        public bool SetFloat(ulong iAddress, float fBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(fBuffer), 4);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="fBuffer">Contains the byte(s) to be written.</param>
        public bool SetFloat(float fBuffer)
        {
            return SetFloat(_iAddress, fBuffer);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="iBuffer">Contains the byte(s) to be written.</param>
        public bool SetInteger(ulong iAddress, int iBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(iBuffer), 4);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="">Contains the byte(s) to be written.</param>
        public bool SetInteger(int iBuffer)
        {
            return SetInteger(_iAddress, iBuffer);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="dBuffer">Contains the byte(s) to be written.</param>
        public bool SetLong(ulong iAddress, double dBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(dBuffer), 1);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="dBuffer">Contains the byte(s) to be written.</param>
        public bool SetLong(double dBuffer)
        {
            return SetLong(_iAddress, dBuffer);
        }

        /// <summary>
        ///   Write two bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="sBuffer">Contains the byte(s) to be written.</param>
        public bool SetShort(ulong iAddress, short sBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(sBuffer), 2);
        }

        /// <summary>
        ///   Write two bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="dBuffer">Contains the byte(s) to be written.</param>
        public bool SetShort(short sBuffer)
        {
            return SetShort(_iAddress, sBuffer);
        }

        /// <summary>
        ///   Write all provided string memory and return whether or not it was successful, when using Unicode the amount of bytes used it multiplied.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="zString">Contains the byte(s) to be written.</param>
        /// <param name="iSize">Contains the number of bytes to write.</param>
        /// <param name="bUnicode">Indicates whether or not the string is unicode.</param>
        public bool SetString(ulong iAddress, string zString, uint iSize, bool bUnicode = false)
        {
            byte[] zTemporary = (bUnicode) ? Encoding.Unicode.GetBytes(zString) : Encoding.ASCII.GetBytes(zString);
            uint iLength = ((bUnicode) ? iSize * 2 : iSize); // zString.Length * 2 : zString.Length );
            var zBuffer = new byte[iLength + ((bUnicode) ? 2 : 1)];
            zTemporary.CopyTo(zBuffer, 0);
            return SetBytes(iAddress, zBuffer, iLength);
        }

        /// <summary>
        ///   Write all provided string memory and return whether or not it was successful, when using Unicode the amount of bytes used it multiplied.
        /// </summary>
        /// <param name="zString">Contains the byte(s) to be written.</param>
        /// <param name="iSize">Contains the number of bytes to write.</param>
        /// <param name="bUnicode">Indicates whether or not the string is unicode.</param>
        public bool SetString(string zString, uint iSize, bool bUnicode = false)
        {
            return SetString(_iAddress, zString, iSize, bUnicode);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="iBuffer">Contains the byte(s) to be written.</param>
        public bool SetUnsignedInteger(ulong iAddress, uint iBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(iBuffer), 4);
        }

        /// <summary>
        ///   Write four bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iBuffer">Contains the byte(s) to be written.</param>
        public bool SetUnsignedInteger(uint iBuffer)
        {
            return SetUnsignedInteger(_iAddress, iBuffer);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="lBuffer">Contains the byte(s) to be written.</param>
        public bool SetUnsignedLong(ulong iAddress, long lBuffer)
        {
            return SetBytes(iAddress, BitConverter.GetBytes(lBuffer), 8);
        }

        /// <summary>
        ///   Write eight bytes to memory and return whether or not it was successful.
        /// </summary>
        /// <param name="lBuffer">Contains the byte(s) to be written.</param>
        public bool SetUnsignedLong(long lBuffer)
        {
            return SetUnsignedLong(_iAddress, lBuffer);
        }

        /// <summary>
        ///   Write all the specified bytes and return whether or not it was successful.
        /// </summary>
        /// <param name="iAddress">Contains the address to write.</param>
        /// <param name="zBuffer">Contains the byte(s) to be written.</param>
        /// <param name="iSize">Contains the number of bytes to write.</param>
        public bool SetBytes(ulong iAddress, byte[] zBuffer, uint iSize)
        {
            return _hMemory.Write(iAddress, zBuffer, iSize) == iSize ? true : false;
        }

        /// <summary>
        ///   Write all the specified bytes and return whether or not it was successful.
        /// </summary>
        /// <param name="zBuffer">Contains the byte(s) to be written.</param>
        /// <param name="iSize">Contains the number of bytes to write.</param>
        public bool SetBytes(byte[] zBuffer, uint iSize)
        {
            return SetBytes(_iAddress, zBuffer, iSize);
        }
    }
}