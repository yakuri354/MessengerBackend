using MessengerBackend.Errors;
using PhoneNumbers;

namespace MessengerBackend.Utils
{
    public class PhoneNumberHelper
    {
        private readonly PhoneNumberUtil _phoneNumberUtil = PhoneNumberUtil.GetInstance();

        public string ParseNumber(string rawNumber)
        {
            try
            {
                var number = _phoneNumberUtil.Parse(rawNumber, null);
                if (!_phoneNumberUtil.IsValidNumber(number))
                {
                    throw new NumberParseException(
                        ErrorType.NOT_A_NUMBER, "Validation failed");
                }

                return _phoneNumberUtil.Format(number, PhoneNumberFormat.E164);
            }
            catch (NumberParseException ex)
            {
                throw new InvalidNumberException(ex);
            }
        }
    }
}