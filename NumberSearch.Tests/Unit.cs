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

            // Coupons don't carry a meaningful quantity, so a Partner line qualifies whatever its quantity.
            var partnerNoQuantity = new NumberSearch.DataAccess.ProductOrder { CouponId = NumberSearch.DataAccess.InternetBundle.PartnerCouponId, Quantity = 0 };
            Assert.Equal(15, NumberSearch.DataAccess.InternetBundle.Discount([fiber, partnerNoQuantity]));

            Assert.True(NumberSearch.DataAccess.InternetBundle.IsPartnerOnly([fiber, partner]));
            Assert.False(NumberSearch.DataAccess.InternetBundle.IsPartnerOnly([fiber, lines, partner]));
        }

        [Theory]
        [InlineData("1.0G/1.0G", 1000)]
        [InlineData("300.0M/300.0M", 300)]
        [InlineData("2.0G/2.0G", 2000)]
        [InlineData("10G", 10000)]
        [InlineData("1000M", 1000)]
        [InlineData("50.0M/50.0M", 50)]
        [InlineData("1 Gbps", 1000)]
        [InlineData("", 0)]
        [InlineData("fast", 0)]
        public void ServiceAddressParseMbpsTest(string maxSpeed, int expected)
        {
            Assert.Equal(expected, NumberSearch.DataAccess.ServiceAddress.ParseMbps(maxSpeed));
        }

        [Theory]
        [InlineData("512", "512")]
        [InlineData("0512", "512")]
        [InlineData("512 1/2", "512")]
        [InlineData("1250A", "1250")]
        [InlineData("N123", "")]
        [InlineData("", "")]
        public void ServiceAddressHouseKeyTest(string houseNumber, string expected)
        {
            Assert.Equal(expected, NumberSearch.DataAccess.ServiceAddress.ToHouseKey(houseNumber));
        }

        [Fact]
        public void InternetBundleCanSellAtTest()
        {
            var gig = new NumberSearch.DataAccess.ServiceAddress { Product = "WFI", Status = "Sellable", MaxSpeed = "1.0G/1.0G" };
            var slow = new NumberSearch.DataAccess.ServiceAddress { Product = "WFI", Status = "Sellable", MaxSpeed = "300.0M/300.0M" };
            var slower = new NumberSearch.DataAccess.ServiceAddress { Product = "WFI", Status = "Sellable", MaxSpeed = "100.0M/100.0M" };
            var unreadable = new NumberSearch.DataAccess.ServiceAddress { Product = "WFI", Status = "Sellable", MaxSpeed = "" };
            var confirm = new NumberSearch.DataAccess.ServiceAddress { Product = "WFI", Status = "Confirm", MaxSpeed = "1.0G/1.0G" };
            var eia = new NumberSearch.DataAccess.ServiceAddress { Product = "EIA", Status = "Quote", MaxSpeed = "1.0G/1.0G" };
            var t300 = NumberSearch.DataAccess.InternetBundle.FiberInternet300ServiceId;
            var t1g = NumberSearch.DataAccess.InternetBundle.FiberInternet1GServiceId;

            Assert.True(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, gig));
            Assert.True(NumberSearch.DataAccess.InternetBundle.CanSellAt(t1g, gig));
            Assert.True(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, slow));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(t1g, slow));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, slower));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, unreadable));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, confirm));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(t300, eia));
            Assert.False(NumberSearch.DataAccess.InternetBundle.CanSellAt(NumberSearch.DataAccess.InternetBundle.StandardLinesServiceId, gig));
        }

        [Fact]
        public void InternetBundleFiberNotesTest()
        {
            Assert.Equal("3 year term. Service address: 71 Pine Cliff Dr, Naches, Washington 98937. Fiber.", NumberSearch.DataAccess.InternetBundle.FiberNotes(3, "71 Pine Cliff Dr, Naches, Washington 98937", "Fiber."));
            Assert.Equal("Fiber.", NumberSearch.DataAccess.InternetBundle.FiberNotes(0, "", "Fiber."));
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
