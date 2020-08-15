using System;
using System.Collections.Generic;
using System.Text.Json;
using PhoneNumbers;

namespace MessengerBackend.Errors
{
    //TODO Organize codes
    public abstract class ApiErrorException : Exception
    {
        protected string? Details;
        public abstract int Code { get; }
        public abstract string Summary { get; }
        public abstract int HttpStatusCode { get; }
        public virtual Dictionary<string, string> HttpHeaders => new Dictionary<string, string>();
    }

    public class InvalidNumberException : ApiErrorException
    {
        private readonly ErrorType _type;

        public InvalidNumberException(NumberParseException ex)
        {
            _type = ex.ErrorType;
            Details = ex.Message;
        }

        public InvalidNumberException(string details) => Details = details;

        public override int Code => 1100;
        public override int HttpStatusCode => 400;
        public override string Summary => "Invalid Number";

        public override string Message => _type switch
        {
            ErrorType.TOO_LONG => "Too long",
            ErrorType.NOT_A_NUMBER => "Is not a phone number",
            ErrorType.TOO_SHORT_NSN => "Too short NSN",
            ErrorType.TOO_SHORT_AFTER_IDD => "Too short after IDD",
            ErrorType.INVALID_COUNTRY_CODE => "Invalid country code",
            _ => "Unknown Error"
        } + "; " + Details;
    }

    public class JsonParseException : ApiErrorException
    {
        public JsonParseException(JsonException ex) => Message = ex.Message;
        public JsonParseException(Newtonsoft.Json.JsonException ex) => Message = ex.Message;
        public JsonParseException(string message) => Message = message;
        public override int Code => 1000;
        public override int HttpStatusCode => 400;
        public override string Summary => "Json parse error";

        public override string Message { get; }
    }

    public class TooManyRequestsException : ApiErrorException
    {
        public override int Code => 1200;
        public override int HttpStatusCode => 429;
        public override string Summary => "Too Many Requests";
    }

    public class WrongTokenException : ApiErrorException
    {
        private readonly string _actualType;

        private readonly string _requiredType;

        public WrongTokenException(string requiredType, string actualType)
        {
            _requiredType = requiredType;
            _actualType = actualType;
        }

        public override int Code => 3101;
        public override int HttpStatusCode => 403;
        public override string Summary => "Wrong token type";

        public override string Message
        {
            get
            {
                if (_requiredType != null && _actualType != null)
                {
                    return "Type '" + _requiredType + "' was expected, instead got '" + _actualType + "'";
                }

                if (_requiredType != null && _actualType == null)
                {
                    return "Type '" + _requiredType + "' was expected";
                }

                if (_requiredType == null && _actualType != null)
                {
                    return "Type '" + _actualType + "' unexpected";
                }

                return "";
            }
        }
    }

#nullable disable
    public class TokenVerificationFailedException : ApiErrorException
    {
        public TokenVerificationFailedException(string message) => Message = message;

        public TokenVerificationFailedException()
        {
        }

        public override int Code => 3102;
        public override int HttpStatusCode => 403;
        public override string Summary => "Token verification failed";

        public override string Message { get; }
    }
#nullable restore
}