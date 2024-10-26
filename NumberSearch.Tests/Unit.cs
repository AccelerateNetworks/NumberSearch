using System;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

namespace NumberSearch.Tests
{
    public class Unit(ITestOutputHelper output)
    {
        [Fact]
        public void LetterToKeypadDigitTest()
        {
            var allAscii = Enumerable.Range('\x1', 127).ToArray();
            var validChars = new char[] { '0', '*', '2', '3', '4', '5', '6', '7', '8', '9' };
            foreach (var letter in allAscii)
            {
                var result = PhoneNumbersNA.PhoneNumber.LetterToKeypadDigit(Convert.ToChar(letter));
                Assert.Contains(result, validChars);
                output.WriteLine($"{letter}, {result}");
            }
        }
    }
}
