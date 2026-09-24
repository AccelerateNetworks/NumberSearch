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
        public void InternetBundleDiscountTest()
        {
            var fiber = new NumberSearch.DataAccess.ProductOrder { ServiceId = NumberSearch.DataAccess.InternetBundle.FiberInternet1GServiceId, Quantity = 1 };
            var twoFiber = new NumberSearch.DataAccess.ProductOrder { ServiceId = NumberSearch.DataAccess.InternetBundle.FiberInternet300ServiceId, Quantity = 2 };
            var lines = new NumberSearch.DataAccess.ProductOrder { ServiceId = NumberSearch.DataAccess.InternetBundle.StandardLinesServiceId, Quantity = 3 };
            var seats = new NumberSearch.DataAccess.ProductOrder { ServiceId = NumberSearch.DataAccess.InternetBundle.ConcurrentSeatsServiceId, Quantity = 1 };
            var noLines = new NumberSearch.DataAccess.ProductOrder { ServiceId = NumberSearch.DataAccess.InternetBundle.StandardLinesServiceId, Quantity = 0 };
            var partner = new NumberSearch.DataAccess.ProductOrder { CouponId = NumberSearch.DataAccess.InternetBundle.PartnerCouponId, Quantity = 1 };
            var otherCoupon = new NumberSearch.DataAccess.ProductOrder { CouponId = Guid.NewGuid(), Quantity = 1 };

            // Fiber on its own, or with a coupon that isn't Partner, pays full price.
            Assert.Equal(0, NumberSearch.DataAccess.InternetBundle.Discount([fiber]));
            Assert.Equal(0, NumberSearch.DataAccess.InternetBundle.Discount([fiber, otherCoupon, noLines]));
            // Phone service or the Partner coupon take $15 off each fiber connection, never more.
            Assert.Equal(15, NumberSearch.DataAccess.InternetBundle.Discount([fiber, lines]));
            Assert.Equal(15, NumberSearch.DataAccess.InternetBundle.Discount([fiber, lines, seats, partner]));
            Assert.Equal(15, NumberSearch.DataAccess.InternetBundle.Discount([fiber, partner]));
            Assert.Equal(45, NumberSearch.DataAccess.InternetBundle.Discount([fiber, twoFiber, seats]));
            // Phone service without fiber has nothing to discount.
            Assert.Equal(0, NumberSearch.DataAccess.InternetBundle.Discount([lines, partner]));

            Assert.True(NumberSearch.DataAccess.InternetBundle.IsPartnerOnly([fiber, partner]));
            Assert.False(NumberSearch.DataAccess.InternetBundle.IsPartnerOnly([fiber, lines, partner]));
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
