using BaileysCSharp.Core.Helper;
using Google.Protobuf;
using Proto;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Unicode;

namespace BaileysCSharp.Core.Logging
{
    public class DefaultLogger : ILogger, IDisposable
    {
        public DefaultLogger()
        {

        }

        public void Dispose() { }

        // PATCH KOLORA: app WPF não tem consola — Console.ForegroundColor/Write lançam
        // IOException e os logs da lib perdem-se. Quando Forward != null, os logs vão
        // para o sink (Serilog) e a consola é ignorada.
        public static Action<string>? Forward;
        private static bool SemConsola => Forward != null;

        private static void ComCor(ConsoleColor cor, Action corpo)
        {
            if (SemConsola) { corpo(); return; }
            lock (locker)
            {
                Console.ForegroundColor = cor;
                try { corpo(); }
                finally { Console.ResetColor(); }
            }
        }

        private static object locker = new object();
        public LogLevel Level { get; set; }


        public void Error(string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };
                ComCor(ConsoleColor.Red, () => Write(logEntry));
            }
        }
        public void Error(object? obj, string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                ComCor(ConsoleColor.Red, () => Write(logEntry));
            }
        }

        public void Error(Exception ex, string message)
        {
            if (Level <= LogLevel.Error)
            {
                var logEntry = new
                {
                    level = LogLevel.Error,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    error = ex.Message,
                    message
                };

                ComCor(ConsoleColor.Red, () => Write(logEntry));
            }
        }

        public void Warn(object? obj, string message)
        {
            if (Level <= LogLevel.Warn)
            {
                var logEntry = new
                {
                    level = LogLevel.Warn,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };
                ComCor(ConsoleColor.Yellow, () => Write(logEntry));
            }
        }

        public void Warn(string message)
        {
            if (Level <= LogLevel.Warn)
            {
                var logEntry = new
                {
                    level = LogLevel.Warn,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                ComCor(ConsoleColor.Yellow, () => Write(logEntry));
            }
        }

        public void Info(string message)
        {
            if (Level <= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Info(object? obj, string message)
        {
            if (Level <= LogLevel.Info)
            {
                var logEntry = new
                {
                    level = LogLevel.Info,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }
        public void Debug(string message)
        {
            if (Level <= LogLevel.Debug)
            {
                var logEntry = new
                {
                    level = LogLevel.Debug,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Debug(object? obj, string message)
        {
            if (Level <= LogLevel.Debug)
            {
                var logEntry = new
                {
                    level = LogLevel.Debug,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Trace(string message)
        {
            if (Level <= LogLevel.Trace)
            {
                var logEntry = new
                {
                    level = LogLevel.Trace,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }

        public void Trace(object? obj, string message)
        {
            if (Level <= LogLevel.Trace)
            {
                var logEntry = new
                {
                    level = LogLevel.Trace,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }


        private void Write(object logEntry)
        {
            var settings = new JsonSerializerOptions()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters = { new Base64Converter(), new ByteStringConverter(), new ProtoConverterFactory() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var json = JsonSerializer.Serialize(logEntry, settings);
            System.Diagnostics.Debug.WriteLine(json);
            if (SemConsola)
            {
                try { Forward?.Invoke(json); } catch { }
                return; // sem consola em WPF: não chama Console.Write (lança IOException)
            }
            Console.Write($"{json}\n");
        }

        public void Raw(object obj, string message)
        {
            if (Level >= LogLevel.Raw)
            {
                var logEntry = new
                {
                    level = LogLevel.Raw,
                    time = DateTime.Now,
                    hostname = Dns.GetHostName(),
                    traceobj = obj,
                    message
                };

                lock (locker)
                {
                    Write(logEntry);
                }
            }
        }
    }
}