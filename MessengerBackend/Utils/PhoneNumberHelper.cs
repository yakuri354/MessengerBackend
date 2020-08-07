using MessengerBackend.Errors;
using PhoneNumbers;

namespace MessengerBackend.Utils
{
    public class PhoneNumberHelper
    {
        // public static string Normalize(string number) =>
        //     number.Replace("(", "")
        //         .Replace(")", "")
        //         .Replace("-", "")
        //         .Replace(" ", "");
        internal readonly PhoneNumberUtil PhoneNumberUtil = PhoneNumberUtil.GetInstance();

        public string ParseNumber(string rawNumber)
        {
            try
            {
                var number = PhoneNumberUtil.Parse(rawNumber, null);
                if (!PhoneNumberUtil.IsValidNumber(number))
                    throw new NumberParseException(
                        ErrorType.NOT_A_NUMBER, "Validation failed");
                return PhoneNumberUtil.Format(number, PhoneNumberFormat.E164);
            }
            catch (NumberParseException ex)
            {
                throw new InvalidNumberException(ex);
            }
        }
    }
}