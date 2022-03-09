using Microsoft.AspNetCore.Http;

using NumberSearch.DataAccess;

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace NumberSearch.Mvc
{
    public class Cart
    {
        public IEnumerable<PhoneNumber>? PhoneNumbers { get; set; }
        public IEnumerable<Product>? Products { get; set; }
        public IEnumerable<Service>? Services { get; set; }
        public IEnumerable<ProductOrder>? ProductOrders { get; set; }
        public IEnumerable<PortedPhoneNumber>? PortedPhoneNumbers { get; set; }
        public IEnumerable<VerifiedPhoneNumber>? VerifiedPhoneNumbers { get; set; }
        public IEnumerable<PurchasedPhoneNumber>? PurchasedPhoneNumbers { get; set; }
        public IEnumerable<Coupon>? Coupons { get; set; }
        public Order? Order { get; set; }

        enum CartKey
        {
            PhoneNumbers,
            Products,
            Services,
            ProductOrders,
            PortedPhoneNumbers,
            VerifiedPhoneNumber,
            PurchasedPhoneNumber,
            Coupon,
            Order
        }

        /// <summary>
        /// Get the current Cart from the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static Cart GetFromSession(ISession session)
        {
            //var numbers = session.Get<List<PhoneNumber>>(CartKey.PhoneNumbers.ToString());
            //var products = session.Get<List<Product>>(CartKey.Products.ToString());
            //var service = session.Get<List<Service>>(CartKey.Services.ToString());
            //var productOrders = session.Get<List<ProductOrder>>(CartKey.ProductOrders.ToString());
            //var portedPhoneNumbers = session.Get<List<PortedPhoneNumber>>(CartKey.PortedPhoneNumbers.ToString());
            //var verfiedPhoneNumbers = session.Get<List<VerifiedPhoneNumber>>(CartKey.VerifiedPhoneNumber.ToString());
            //var purchasedPhoneNumbers = session.Get<List<PurchasedPhoneNumber>>(CartKey.PurchasedPhoneNumber.ToString());
            //var order = session.Get<Order>(CartKey.Order.ToString());

            var cart = session.Get<Cart>("Cart");

            return new Cart
            {
                PhoneNumbers = cart?.PhoneNumbers ?? new List<PhoneNumber>(),
                Products = cart?.Products ?? new List<Product>(),
                Services = cart?.Services ?? new List<Service>(),
                ProductOrders = cart?.ProductOrders ?? new List<ProductOrder>(),
                PortedPhoneNumbers = cart?.PortedPhoneNumbers ?? new List<PortedPhoneNumber>(),
                VerifiedPhoneNumbers = cart?.VerifiedPhoneNumbers ?? new List<VerifiedPhoneNumber>(),
                PurchasedPhoneNumbers = cart?.PurchasedPhoneNumbers ?? new List<PurchasedPhoneNumber>(),
                Coupons = cart?.Coupons ?? new List<Coupon>(),
                Order = cart?.Order ?? new Order()
            };
        }

        /// <summary>
        /// Save the cart to the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public bool SetToSession(ISession session)
        {
            session.Set<Cart>("Cart", this);
            //session.Set<List<PhoneNumber>>(CartKey.PhoneNumbers.ToString(), PhoneNumbers?.ToList());
            //session.Set<List<Product>>(CartKey.Products.ToString(), Products?.ToList());
            //session.Set<List<Service>>(CartKey.Services.ToString(), Services?.ToList());
            //session.Set<List<ProductOrder>>(CartKey.ProductOrders.ToString(), ProductOrders?.ToList());
            //session.Set<List<PortedPhoneNumber>>(CartKey.PortedPhoneNumbers.ToString(), PortedPhoneNumbers?.ToList());
            //session.Set<List<VerifiedPhoneNumber>>(CartKey.VerifiedPhoneNumber.ToString(), VerifiedPhoneNumbers?.ToList());
            //session.Set<List<PurchasedPhoneNumber>>(CartKey.PurchasedPhoneNumber.ToString(), PurchasedPhoneNumbers?.ToList());
            //session.Set<Order>(CartKey.Order.ToString(), Order);

            return true;
        }

        /// <summary>
        /// Add a PhoneNumber to the Cart.
        /// </summary>
        /// <param name="newPhoneNumber"></param>
        /// <returns></returns>
        public bool AddPhoneNumber(PhoneNumber phoneNumber, ProductOrder productOrder)
        {
            phoneNumber ??= new PhoneNumber();
            productOrder ??= new ProductOrder();

            // We're using dictionaries here to prevent duplicates.
            var phoneNumbers = PhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (phoneNumber?.DialedNumber?.Length == 10 && phoneNumber.DialedNumber == productOrder?.DialedNumber)
            {
                phoneNumbers[phoneNumber.DialedNumber] = phoneNumber;
                PhoneNumbers = phoneNumbers.Values;

                productOrders[productOrder.DialedNumber] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a PhoneNumber to the Cart.
        /// </summary>
        /// <param name="newPhoneNumber"></param>
        /// <returns></returns>
        public bool AddPortedPhoneNumber(PortedPhoneNumber portedPhoneNumber, ProductOrder productOrder)
        {
            portedPhoneNumber ??= new PortedPhoneNumber();
            productOrder ??= new ProductOrder();

            // We're using dictionaries here to prevent duplicates.
            var portedPhoneNumbers = this.PortedPhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (portedPhoneNumber?.PortedPhoneNumberId is not null && portedPhoneNumber.PortedPhoneNumberId == productOrder?.PortedPhoneNumberId)
            {
                portedPhoneNumbers[portedPhoneNumber.PortedPhoneNumberId.ToString()] = portedPhoneNumber;
                PortedPhoneNumbers = portedPhoneNumbers.Values;

                productOrders[productOrder.PortedPhoneNumberId?.ToString() ?? string.Empty] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a verified Phone Number to the cart.
        /// </summary>
        /// <param name="verifiedPhoneNumber"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool AddVerifiedPhoneNumber(VerifiedPhoneNumber verifiedPhoneNumber, ProductOrder productOrder)
        {
            verifiedPhoneNumber ??= new VerifiedPhoneNumber();
            productOrder ??= new ProductOrder();

            // We're using dictionaries here to prevent duplicates.
            var verifiedPhoneNumbers = this.VerifiedPhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (verifiedPhoneNumber?.VerifiedPhoneNumberId is not null && verifiedPhoneNumber.VerifiedPhoneNumberId == productOrder?.VerifiedPhoneNumberId)
            {
                verifiedPhoneNumbers[verifiedPhoneNumber.VerifiedPhoneNumberId.ToString()] = verifiedPhoneNumber;
                VerifiedPhoneNumbers = verifiedPhoneNumbers.Values;

                productOrders[productOrder.VerifiedPhoneNumberId?.ToString() ?? string.Empty] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a Product to the Cart.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool AddProduct(Product product, ProductOrder productOrder)
        {
            var products = ProductsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (product is not null && productOrder is not null && !string.IsNullOrWhiteSpace(product?.Name) && product?.ProductId == productOrder?.ProductId)
            {
                products[product!.ProductId.ToString()] = product;
                Products = products.Values;

                productOrders[productOrder!.ProductId.ToString()] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a Service to the Cart.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool AddService(Service service, ProductOrder productOrder)
        {
            var services = ServicesToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (service is not null && productOrder is not null && !string.IsNullOrWhiteSpace(service?.Name) && service?.ServiceId == productOrder?.ServiceId)
            {
                services[service!.ServiceId.ToString()] = service;
                Services = services.Values;

                productOrders[productOrder!.ServiceId.ToString()] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Add a Coupon to the Cart.
        /// </summary>
        /// <param name="coupon"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool AddCoupon(Coupon coupon, ProductOrder productOrder)
        {
            var coupons = CouponsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (coupon is not null && productOrder is not null && !string.IsNullOrWhiteSpace(coupon?.Name) && productOrder.CouponId is not null && coupon?.CouponId == productOrder?.CouponId)
            {
                coupons[coupon!.CouponId.ToString()] = coupon;
                Coupons = coupons.Values;

                productOrders[productOrder!.CouponId.ToString() ?? string.Empty] = productOrder;
                ProductOrders = productOrders.Values;

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a phone number from the Cart.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemovePhoneNumber(PhoneNumber phoneNumber, ProductOrder productOrder)
        {
            phoneNumber ??= new PhoneNumber();
            productOrder ??= new ProductOrder();

            var phoneNumbers = PhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (phoneNumber?.DialedNumber?.Length == 10 && phoneNumber.DialedNumber == productOrder?.DialedNumber)
            {
                var checkRemovePhoneNumber = phoneNumbers.Remove(phoneNumber.DialedNumber);
                var checkRemoveProductOrder = productOrders.Remove(productOrder.DialedNumber);

                if (checkRemovePhoneNumber && checkRemoveProductOrder)
                {
                    PhoneNumbers = phoneNumbers.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a phone number from the Cart.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemovePortedPhoneNumber(PortedPhoneNumber portedPhoneNumber, ProductOrder productOrder)
        {
            portedPhoneNumber ??= new PortedPhoneNumber();
            productOrder ??= new ProductOrder();

            var portedPhoneNumbers = PortedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (portedPhoneNumber?.PortedPhoneNumberId is not null && portedPhoneNumber.PortedPhoneNumberId == productOrder?.PortedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = portedPhoneNumbers.Remove(portedPhoneNumber.PortedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.PortedPhoneNumberId?.ToString() ?? string.Empty);

                if (checkRemovePortedPhoneNumber && checkRemoveProductOrder)
                {
                    PortedPhoneNumbers = portedPhoneNumbers.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a verified number form the cart.
        /// </summary>
        /// <param name="verifiedPhoneNumber"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemoveVerifiedPhoneNumber(VerifiedPhoneNumber verifiedPhoneNumber, ProductOrder productOrder)
        {
            verifiedPhoneNumber ??= new VerifiedPhoneNumber();
            productOrder ??= new ProductOrder();

            var verifedPhoneNumbers = VerifiedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (verifiedPhoneNumber?.VerifiedPhoneNumberId is not null && verifiedPhoneNumber.VerifiedPhoneNumberId == productOrder?.VerifiedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = verifedPhoneNumbers.Remove(verifiedPhoneNumber.VerifiedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.VerifiedPhoneNumberId?.ToString() ?? string.Empty);

                if (checkRemovePortedPhoneNumber && checkRemoveProductOrder)
                {
                    VerifiedPhoneNumbers = verifedPhoneNumbers.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Remove a product from the Cart.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemoveProduct(Product product, ProductOrder productOrder)
        {
            product ??= new Product();
            productOrder ??= new ProductOrder();

            var products = ProductsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (product.ProductId == productOrder?.ProductId)
            {
                var checkRemoveProduct = products.Remove(product.ProductId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.ProductId.ToString());

                if (checkRemoveProduct && checkRemoveProductorder)
                {
                    Products = products.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a Service from the Cart.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemoveService(Service service, ProductOrder productOrder)
        {
            service ??= new Service();
            productOrder ??= new ProductOrder();

            var services = ServicesToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (service.ServiceId == productOrder?.ServiceId)
            {
                var checkRemoveService = services.Remove(service.ServiceId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.ServiceId.ToString());

                if (checkRemoveService && checkRemoveProductorder)
                {
                    Services = services.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Remove a Coupon from the Cart.
        /// </summary>
        /// <param name="coupon"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemoveCoupon(Coupon coupon, ProductOrder productOrder)
        {
            coupon ??= new Coupon();
            productOrder ??= new ProductOrder();

            var coupons = CouponsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (coupon.CouponId == productOrder?.CouponId)
            {
                var checkRemoveService = coupons.Remove(coupon.CouponId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.CouponId?.ToString() ?? string.Empty);

                if (checkRemoveService && checkRemoveProductorder)
                {
                    Coupons = coupons.Values;
                    ProductOrders = productOrders.Values;

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public Dictionary<string, ProductOrder> ProductOrdersToDictionary()
        {
            // This was too difficult to debug, but very fun to write!

            //return ProductOrders.ToDictionary(x => !string.IsNullOrWhiteSpace(x.DialedNumber)
            //? x.DialedNumber : x.PortedPhoneNumberId.HasValue
            //? x.PortedPhoneNumberId.ToString() : x.VerifiedPhoneNumberId.HasValue
            //? x.VerifiedPhoneNumberId.ToString() : x.ProductId != System.Guid.Empty
            //? x.ProductId.ToString() : x.ServiceId != System.Guid.Empty
            //? x.ServiceId.ToString() : x.CouponId.ToString(), x => x);

            if (ProductOrders is not null && ProductOrders.Any())
            {
                var dict = new Dictionary<string, ProductOrder>();

                foreach (var item in ProductOrders)
                {
                    var foreignId = string.Empty;

                    if (!string.IsNullOrWhiteSpace(item.DialedNumber))
                    {
                        foreignId = item.DialedNumber;
                    }
                    else if (item.PortedPhoneNumberId.HasValue)
                    {
                        foreignId = item.PortedPhoneNumberId.ToString();
                    }
                    else if (item.VerifiedPhoneNumberId.HasValue)
                    {
                        foreignId = item.VerifiedPhoneNumberId.ToString();
                    }
                    else if (item.ProductId != System.Guid.Empty)
                    {
                        foreignId = item.ProductId.ToString();
                    }
                    else if (item.ServiceId != System.Guid.Empty)
                    {
                        foreignId = item.ServiceId.ToString();
                    }
                    else if (item.CouponId.HasValue)
                    {
                        foreignId = item.CouponId.ToString();
                    }

                    if (!string.IsNullOrWhiteSpace(foreignId))
                    {
                        dict.Add(foreignId, item);
                    }
                }

                return dict;
            }
            else
            {
                return new Dictionary<string, ProductOrder>();
            }
        }

        public Dictionary<string, PhoneNumber> PhoneNumbersToDictionary()
        {
            return PhoneNumbers is not null ? PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x) : new();
        }

        public Dictionary<string, PortedPhoneNumber> PortedPhoneNumbersToDictionary()
        {
            return PortedPhoneNumbers is not null ? PortedPhoneNumbers.ToDictionary(x => x.PortedPhoneNumberId.ToString(), x => x) : new();
        }

        public Dictionary<string, VerifiedPhoneNumber> VerifiedPhoneNumbersToDictionary()
        {
            return VerifiedPhoneNumbers is not null ? VerifiedPhoneNumbers.ToDictionary(x => x.VerifiedPhoneNumberId.ToString(), x => x) : new();
        }

        public Dictionary<string, PurchasedPhoneNumber> PurchasedPhoneNumbersToDictionary()
        {
            return PurchasedPhoneNumbers is not null ? PurchasedPhoneNumbers.ToDictionary(x => x.DialedNumber, x => x) : new();
        }

        public Dictionary<string, Product> ProductsToDictionary()
        {
            return Products is not null ? Products.ToDictionary(x => x.ProductId.ToString(), x => x) : new();
        }

        public Dictionary<string, Service> ServicesToDictionary()
        {
            return Services is not null ? Services.ToDictionary(x => x.ServiceId.ToString(), x => x) : new();
        }

        public Dictionary<string, Coupon> CouponsToDictionary()
        {
            return Coupons is not null ? Coupons.ToDictionary(x => x.CouponId.ToString(), x => x) : new();
        }
    }
}
