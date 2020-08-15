using System;
using System.Collections.Generic;
using System.Reflection;
using Serilog;

namespace MessengerBackend.RealTime
{
    public class VerificationBuilder
    {
        private readonly List<Func<object?, string?>> _argumentPredicates = new List<Func<object?, string?>>();
        private readonly ILogger _logger;
        private readonly InboundMessage _message;

        public VerificationBuilder(InboundMessage message, ILogger logger)
        {
            _message = message;
            _logger = logger;
        }

        public VerificationBuilder Method(MethodInfo methodInfo)
        {
            foreach (var param in methodInfo.GetParameters())
            {
                ReflectionArgument(param.ParameterType, null, !param.IsOptional);
            }

            return this;
        }

        public VerificationBuilder Argument<T>(Func<T, string?>? customRequirement = null,
            bool required = true)
        {
            var args = _argumentPredicates.Count + 1;
            if (required)
            {
                _argumentPredicates.Add(arg =>
                {
                    if (arg == null)
                    {
                        return $"Argument {args} must be specified";
                    }

                    if (!(arg is T))
                    {
                        return $"Argument {args} must be of type {typeof(T).Name}";
                    }

                    var result = customRequirement?.Invoke((T) arg);
                    return result != null ? $"Argument {args} is invalid: {result}" : null;
                });
            }
            else
            {
                _argumentPredicates.Add(arg =>
                {
                    if (arg == null)
                    {
                        return null;
                    }

                    if (!(arg is T))
                    {
                        return $"Argument {args} must be of type {typeof(T).Name}";
                    }

                    var result = customRequirement?.Invoke((T) arg);
                    return result != null ? $"Argument {args} is invalid: {result}" : null;
                });
            }

            return this;
        }

        private void ReflectionArgument(Type argumentType, Func<object?, string?>? customRequirement = null,
            bool required = true)
        {
            var args = _argumentPredicates.Count + 1;
            if (required)
            {
                _argumentPredicates.Add(arg =>
                {
                    if (arg == null)
                    {
                        return $"Argument {args} must be specified";
                    }

                    if (!(arg.GetType() == argumentType))
                    {
                        return $"Argument {args} must be of type {argumentType.Name}";
                    }

                    var result = customRequirement?.Invoke(arg);
                    return result != null ? $"Argument {args} is invalid: {result}" : null;
                });
            }
            else
            {
                _argumentPredicates.Add(arg =>
                {
                    if (arg == null)
                    {
                        return null;
                    }

                    if (!(arg.GetType() == argumentType))
                    {
                        return $"Argument {args} must be of type {argumentType.Name}";
                    }

                    var result = customRequirement?.Invoke(arg);
                    return result != null ? $"Argument {args} is invalid: {result}" : null;
                });
            }
        }

        public string? Build()
        {
            if (_message.Params == null)
            {
                var err = $"Message with type {_message.Type.ToString()} "
                          + (_message.Type == InboundMessageType.Method ? $"and method {_message.Method}" : "")
                          + $"has {_argumentPredicates.Count} parameters, got only {_message.Params?.Count ?? 0}";
                _logger.Information(err);
                return err;
            }

            for (var i = 0; i < _argumentPredicates.Count; i++)
            {
                object? arg;
                try
                {
                    arg = _message.Params[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    arg = null;
                }

                var result = _argumentPredicates[i](arg);
                if (result != null)
                {
                    return result;
                }
            }

            _logger.Verbose("Verification succeeded for message ID {MessageID}", _message.ID);
            return null;
        }
    }
}