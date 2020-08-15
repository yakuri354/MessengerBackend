using System;
using Npgsql.Logging;
using Serilog;
using Serilog.Events;

namespace MessengerBackend.Utils
{
    internal class SerilogLoggingProvider : INpgsqlLoggingProvider
    {
        private readonly ILogger _logger;
        internal SerilogLoggingProvider(ILogger logger) => _logger = logger;

        public NpgsqlLogger CreateLogger(string name) => new NpgsqlSerilogLogger(name, _logger);
    }

    internal class NpgsqlSerilogLogger : NpgsqlLogger
    {
        private readonly ILogger _logger;

        internal NpgsqlSerilogLogger(string name, ILogger logger)
        {
            NpgsqlLogManager.IsParameterLoggingEnabled = Serilog.Log.IsEnabled(LogEventLevel.Debug);
            _logger = logger.ForContext<NpgsqlSerilogLogger>().ForContext("NpgsqlName", name);
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception? exception = null)
        {
            if (level != NpgsqlLogLevel.Debug) // TODO FIXME
            {
                _logger.Write(ToSerilogLevel(level), exception, msg);
            }
        }

        public override bool IsEnabled(NpgsqlLogLevel level) => _logger.IsEnabled(ToSerilogLevel(level));

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