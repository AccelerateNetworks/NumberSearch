using NumberSearch.Mvc.Controllers;

using Xunit;

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

        [Fact]
        public void PortPinTest()
        {
            var pin = CartController.GetPortingPIN();
            Assert.True(pin > 0);
            Assert.True(pin > 100000);
        }

        [Theory]
        [InlineData("Pine Cliff", "pinecliff")]
        [InlineData("1st", "1st")]
        [InlineData("Bothell-Everett ", "bothelleverett")]
        [InlineData("O'Neil", "oneil")]
        [InlineData("", "")]
        public void ServiceAddressStreetKeyTest(string streetName, string expected)
        {
            Assert.Equal(expected, NumberSearch.DataAccess.ServiceAddress.ToStreetKey(streetName));
        }

        [Fact]
        public void ServiceAddressDistanceTest()
        {
            // 0.0001 degrees of latitude is about 11 meters.
            var meters = NumberSearch.DataAccess.ServiceAddress.DistanceMeters(46.8993, -121.0128, 46.8994, -121.0128);
            Assert.InRange(meters, 10.5, 11.5);
            Assert.Equal(0, NumberSearch.DataAccess.ServiceAddress.DistanceMeters(46.8993, -121.0128, 46.8993, -121.0128));
        }
    }
}
