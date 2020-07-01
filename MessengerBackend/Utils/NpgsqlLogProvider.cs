using System;
using Npgsql.Logging;
using Serilog;
using Serilog.Events;

namespace MessengerBackend.Utils
{
    internal class SerilogLoggingProvider : INpgsqlLoggingProvider
    {
        public NpgsqlLogger CreateLogger(string name)
        {
            return new SerilogLogger(name);
        }
    }

    internal class SerilogLogger : NpgsqlLogger
    {
        private readonly ILogger _logger;

        internal SerilogLogger(string name)
        {
            NpgsqlLogManager.IsParameterLoggingEnabled = Serilog.Log.IsEnabled(LogEventLevel.Debug);
            _logger = Serilog.Log.ForContext("NpgsqlName", name);
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception exception = null)
        {
            _logger.Write(ToSerilogLevel(level), exception, "");
        }

        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            return _logger.IsEnabled(ToSerilogLevel(level));
        }

        private static LogEventLevel ToSerilogLevel(NpgsqlLogLevel level)
        {
            return level switch
            {
                NpgsqlLogLevel.Debug => LogEventLevel.Debug,
                NpgsqlLogLevel.Error => LogEventLevel.Error,
                NpgsqlLogLevel.Fatal => LogEventLevel.Fatal,
                NpgsqlLogLevel.Info => LogEventLevel.Information,
                NpgsqlLogLevel.Trace => LogEventLevel.Verbose,
                NpgsqlLogLevel.Warn => LogEventLevel.Warning,
                _ => throw new ArgumentOutOfRangeException("Level out of range: " + level)
            };
        }
    }
}