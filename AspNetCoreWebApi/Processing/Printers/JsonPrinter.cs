using System;
using System.IO;
using System.Text;

namespace AspNetCoreWebApi.Processing.Printers
{
    public static class StreamWriterExtensions
    {
        public static void WriteDoubleQuote(this Stream stream)
        {
            stream.WriteByte(34);
        }

        public static void WriteA(this Stream stream)
        {
            stream.WriteByte(64);
        }

        public static void WriteOpenRoundBracket(this Stream stream)
        {
            stream.WriteByte(40);
        }

        public static void WriteCloseRoundBracket(this Stream stream)
        {
            stream.WriteByte(41);
        }

        public static void StartObject(this Stream stream)
        {
            stream.WriteByte(123);
        }

        public static void EndObject(this Stream stream)
        {
            stream.WriteByte(125);
        }

        public static void StartArray(this Stream stream)
        {
            stream.WriteByte(91);
        }

        public static void EndArray(this Stream stream)
        {
            stream.WriteByte(93);
        }

        public static void Write(this Stream stream, string str)
        {
            unsafe
            {
                int count = Encoding.UTF8.GetByteCount(str);
                byte* buffer = stackalloc byte[count];
                Span<byte> span = new Span<byte>(buffer, count);
                Encoding.UTF8.GetBytes(str.AsSpan(), span);
                stream.Write(span);
            }
        }

        public static void Write(this Stream stream, int i)
        {
            if (i == 0)
            {
                stream.WriteByte(48);
                return;
            }

            if (i < 0)
            {
                stream.WriteByte(45);
                WriteImpl(stream, -i);
            }
            else
            {
                WriteImpl(stream, i);
            }
        }

        private static void WriteImpl(Stream stream, int i)
        {
            if (i == 0)
            {
                return;
            }

            WriteImpl(stream, i / 10);
            stream.WriteByte((byte)(48 + i % 10));
        }

        public static void PropertyNameWithColon(this Stream sw, string name)
        {
            sw.WriteByte(34);
            sw.Write(name);
            sw.WriteByte(34);
            sw.WriteByte(58);
        }

        public static void Property(this Stream sw, string name, int value)
        {
            sw.WriteByte(34);
            sw.Write(name);
            sw.WriteByte(34);
            sw.WriteByte(58);
            sw.Write(value);
        }

        public static void Property(this Stream sw, string name, string value)
        {
            sw.WriteByte(34);
            sw.Write(name);
            sw.WriteByte(34);
            sw.WriteByte(58);
            sw.WriteByte(34);
            sw.Write(value);
            sw.WriteByte(34);
        }

        public static void Comma(this Stream sw)
        {
            sw.WriteByte(44);
        }
    }
}