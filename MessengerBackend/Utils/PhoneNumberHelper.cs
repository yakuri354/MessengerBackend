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
        internal readonly PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();

        public string ParseNumber(string rawNumber)
        {
            try
            {
                var number = phoneNumberUtil.Parse(rawNumber, null);
                if (!phoneNumberUtil.IsValidNumber(number))
                    throw new NumberParseException(
                        ErrorType.NOT_A_NUMBER, "Validation failed");
                return phoneNumberUtil.Format(number, PhoneNumberFormat.E164);
            }
            catch (NumberParseException ex)
            {
                throw new InvalidNumberException(ex);
            }
        }
    }
}