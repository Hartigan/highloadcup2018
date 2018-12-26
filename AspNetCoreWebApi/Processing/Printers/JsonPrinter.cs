using System;
using System.IO;

namespace AspNetCoreWebApi.Processing.Printers
{
    public static class StreamWriterExtensions
    {
        public static void PropertyNameWithColon(this StreamWriter sw, string name)
        {
            sw.Write('\"');
            sw.Write(name);
            sw.Write('\"');
            sw.Write(':');
        }

        public static void Property(this StreamWriter sw, string name, int value)
        {
            sw.Write('\"');
            sw.Write(name);
            sw.Write('\"');
            sw.Write(':');
            sw.Write(value);
        }

        public static void Property(this StreamWriter sw, string name, long value)
        {
            sw.Write('\"');
            sw.Write(name);
            sw.Write('\"');
            sw.Write(':');
            sw.Write(value);
        }

        public static void Property(this StreamWriter sw, string name, string value)
        {
            sw.Write('\"');
            sw.Write(name);
            sw.Write('\"');
            sw.Write(':');
            sw.Write('\"');
            sw.Write(value);
            sw.Write('\"');
        }

        public static void Comma(this StreamWriter sw)
        {
            sw.Write(',');
        }
    }

    public class Quotes : IDisposable
    {
        private StreamWriter _sw;

        public Quotes(StreamWriter sw)
        {
            _sw = sw;
            _sw.Write('\"');
        }

        public void Dispose()
        {
            _sw.Write('\"');
        }
    }

    public class JsObject : IDisposable
    {
        private StreamWriter _sw;

        public JsObject(StreamWriter sw)
        {
            _sw = sw;
            _sw.Write('{');
        }

        public void Dispose()
        {
            _sw.Write('}');
        }
    }

    public class JsArray : IDisposable
    {
        private StreamWriter _sw;

        public JsArray(StreamWriter sw)
        {
            _sw = sw;
            _sw.Write('[');
        }

        public void Dispose()
        {
            _sw.Write(']');
        }
    }
}