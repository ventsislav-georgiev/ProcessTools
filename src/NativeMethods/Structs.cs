using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WinAPI
{
    public static class Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UnicodeString
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TokenUser
        {
            public readonly SidAndAttributes User;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SidAndAttributes
        {
            public readonly IntPtr Sid;
            private readonly int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GenericMapping
        {
            private readonly int GenericRead;
            private readonly int GenericWrite;
            private readonly int GenericExecute;
            private readonly int GenericAll;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowInfo
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public Enums.WindowStyles dwStyle;
            public Enums.WindowStylesEx dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WindowInfo(Boolean? filler)
                : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WindowInfo)));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MemoryBasicInformation
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public Enums.Protection AllocationProtect;
            public IntPtr RegionSize;
            public Enums.MemoryState State;
            public Enums.Protection Protect;
            public Enums.MemoryType Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectBasicInformation
        {
            private readonly int Attributes;
            private readonly int GrantedAccess;
            private readonly int HandleCount;
            private readonly int PointerCount;
            private readonly int PagedPoolUsage;
            private readonly int NonPagedPoolUsage;
            private readonly int Reserved1;
            private readonly int Reserved2;
            private readonly int Reserved3;
            private readonly int NameInformationLength;
            private readonly int TypeInformationLength;
            private readonly int SecurityDescriptorLength;
            private readonly FILETIME CreateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectNameInformation
        {
            // Information Class 1
            private readonly UnicodeString Name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ObjectTypeInformation
        {
            // Information Class 2
            public UnicodeString Name;

            public int ObjectCount;
            public int HandleCount;
            public int Reserved1;
            public int Reserved2;
            public int Reserved3;
            public int Reserved4;
            public int PeakObjectCount;
            public int PeakHandleCount;
            public int Reserved5;
            public int Reserved6;
            public int Reserved7;
            public int Reserved8;
            public int InvalidAttributes;
            public GenericMapping GenericMapping;
            public int ValidAccess;
            public byte Unknown;
            public byte MaintainHandleDatabase;
            public int PoolType;
            public int PagedPoolUsage;
            public int NonPagedPoolUsage;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SystemHandleInformation
        {
            // Information Class 16
            public int ProcessID;

            public byte ObjectTypeNumber;
            public byte Flags; // 0x01 = PROTECT_FROM_CLOSE, 0x02 = INHERIT
            public IntPtr Handle;
            public int Object_Pointer;
            public UInt32 GrantedAccess;
        }

        /// <summary>
        /// Wrapper around the Winapi POINT type.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// The X Coordinate.
            /// </summary>
            public int X;

            /// <summary>
            /// The Y Coordinate.
            /// </summary>
            public int Y;

            /// <summary>
            /// Creates a new POINT.
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            /// <summary>
            /// Implicit cast.
            /// </summary>
            /// <returns></returns>
            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            /// <summary>
            /// Implicit cast.
            /// </summary>
            /// <returns></returns>
            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }

        /// <summary>
        /// Wrapper around the Winapi RECT type.
        /// </summary>
        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            /// <summary>
            /// LEFT
            /// </summary>
            public int Left;

            /// <summary>
            /// TOP
            /// </summary>
            public int Top;

            /// <summary>
            /// RIGHT
            /// </summary>
            public int Right;

            /// <summary>
            /// BOTTOM
            /// </summary>
            public int Bottom;

            /// <summary>
            /// Creates a new RECT.
            /// </summary>
            public RECT(int left_, int top_, int right_, int bottom_)
            {
                Left = left_;
                Top = top_;
                Right = right_;
                Bottom = bottom_;
            }

            /// <summary>
            /// HEIGHT
            /// </summary>
            public int Height { get { return Bottom - Top; } }

            /// <summary>
            /// WIDTH
            /// </summary>
            public int Width { get { return Right - Left; } }

            /// <summary>
            /// SIZE
            /// </summary>
            public Size Size { get { return new Size(Width, Height); } }

            /// <summary>
            /// LOCATION
            /// </summary>
            public Point Location { get { return new Point(Left, Top); } }

            // Handy method for converting to a System.Drawing.Rectangle
            /// <summary>
            /// Convert RECT to a Rectangle.
            /// </summary>
            public Rectangle ToRectangle()
            { return Rectangle.FromLTRB(Left, Top, Right, Bottom); }

            /// <summary>
            /// Convert Rectangle to a RECT
            /// </summary>
            public static RECT FromRectangle(Rectangle rectangle)
            {
                return new RECT(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            public override int GetHashCode()
            {
                return Left ^ ((Top << 13) | (Top >> 0x13))
                  ^ ((Width << 0x1a) | (Width >> 6))
                  ^ ((Height << 7) | (Height >> 0x19));
            }

            #region Operator overloads

            /// <summary>
            /// Implicit Cast.
            /// </summary>
            public static implicit operator Rectangle(RECT rect)
            {
                return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            /// <summary>
            /// Implicit Cast.
            /// </summary>
            public static implicit operator RECT(Rectangle rect)
            {
                return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }

            #endregion Operator overloads
        }
    }
}