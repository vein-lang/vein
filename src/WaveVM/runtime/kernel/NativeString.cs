namespace wave.runtime.kernel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    //[PublicAPI]
    [SecurityCritical]
    public unsafe struct NativeString
    {
        public void* @ref;

        /// <summary>
        /// Get Length of this string
        /// </summary>
        /// <returns></returns>
        [SecurityCritical]
        public int GetLen() => Marshal.ReadInt32((IntPtr)@ref);

        /// <summary>
        /// Get Encoding Page
        /// </summary>
        /// <returns></returns>
        [SecurityCritical]
        public Encoding GetEncoding() => Encoding.GetEncoding(Marshal.ReadInt32((IntPtr)@ref + 4));

        [SecurityCritical]
        public override int GetHashCode() => Marshal.ReadInt32((IntPtr)@ref + 8);

        /// <summary>
        /// Get managed buffer
        /// </summary>
        [SecurityCritical]
        public byte[] GetBuffer() => ReadUtf8String((void*)@ref).ToArray();

        /// <summary>
        /// get size of this structure
        /// </summary>
        /// <returns></returns>
        [SecurityCritical]
        public static int SizeOf(NativeString* p) => p->GetLen() + 12;

        /// <summary>
        /// Get UnmanagedBuffer
        /// </summary>
        /// <returns></returns>
        [SecurityCritical]
        public byte* GetUnmanagedBuffer()
        {
            fixed (byte* p = GetBuffer()) return p;
        }

        /// <summary>get managed string </summary>
        /// <param name="p">point of native string </param>
        /// <param name="str">managed string </param>
        /// <param name="free">free memory after unwraping string? </param>
        /// <exception cref="AccessViolationException">point has disposed</exception>
        [SecurityCritical]
        public static void Unwrap(in NativeString p, out string str, bool free, bool suppressFail = false)
        {
            if (p.@ref == null)
            {
                if (!suppressFail) throw new AccessViolationException();
                str = null;
                return;
            }
            var buffer = p.GetBuffer();
            var enc = p.GetEncoding();
            str = enc.GetString(buffer, 0, p.GetLen());
            if (free) p.Dispose();
        }

        [SecurityCritical]
        public static NativeString Wrap(string str, Encoding enc = null)
        {
            if (enc is null) enc = Encoding.UTF8;
            return new NativeString
            {
                @ref = WriteUtf8String(enc.GetBytes(str), enc.CodePage, GetHashCode(str))
            };
        }

        #region private

        [SecurityCritical]
        private static IEnumerable<byte> ReadUtf8String(void* point)
        {
            var address = new IntPtr(point);
            var length = Marshal.ReadInt32(address);
            var startStr = address + 12;
            var result = new List<byte>();
            for (var i = 0; i < length; i++)
                result.Add(Marshal.ReadByte(startStr + i));
            return result.ToArray();
        }

        [SecurityCritical]
        private static void* WriteUtf8String(IReadOnlyList<byte> bytes, int encodingPage, int hashCode)
        {
            var len = bytes.Count;
            var adr = Marshal.AllocHGlobal(len + 12);
            Marshal.WriteInt32(adr, len);
            Marshal.WriteInt32(adr + 4, encodingPage);
            Marshal.WriteInt32(adr + 8, hashCode);

            for (var i = 0; i < len; i++)
                Marshal.WriteByte(adr + 12 + i, bytes[i]);
            return (void*)adr;
        }

        // only x64
        [SecurityCritical]
        public static int GetHashCode(string str)
        {
            fixed (char* chPtr1 = str)
            {
                var num1 = 0x1505;
                var num2 = num1;
                var chPtr2 = chPtr1;
                int num3;
                while ((num3 = *chPtr2) != 0)
                {
                    num1 = (num1 << 5) + num1 ^ num3;
                    var num4 = (int)chPtr2[1];
                    if (num4 != 0)
                    {
                        num2 = (num2 << 5) + num2 ^ num4;
                        chPtr2 += 2;
                    }
                    else break;
                }
                return num1 + num2 * 0x5D588B65;
            }
        }

        [SecurityCritical]
        public byte[] GetRaw()
        {
            var len = GetLen() + 12;
            var arr = new List<byte>();
            for (var i = 0; i < len; i++)
                arr.Add(Marshal.ReadByte((IntPtr)@ref + 12 + i));
            return arr.ToArray();
        }

        #endregion private

        #region IDisposable

        public void Dispose()
        {
            Marshal.FreeHGlobal((IntPtr)@ref);
            @ref = null;
        }

        #endregion IDisposable
    }
}