
using NumberSearch.DataAccess;

using System.Collections.Generic;
using System.Linq;

namespace NumberSearch.Ops
{
    public class Cart
    {
        public IEnumerable<PhoneNumber> PhoneNumbers { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Service> Services { get; set; }
        public IEnumerable<ProductOrder> ProductOrders { get; set; }
        public IEnumerable<PortedPhoneNumber> PortedPhoneNumbers { get; set; }
        public IEnumerable<VerifiedPhoneNumber> VerifiedPhoneNumbers { get; set; }
        public IEnumerable<PurchasedPhoneNumber> PurchasedPhoneNumbers { get; set; }
        public IEnumerable<Coupon> Coupons { get; set; }

        public Order Order { get; set; }

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
        /// Add a PhoneNumber to the Cart.
        /// </summary>
        /// <param name="newPhoneNumber"></param>
        /// <returns></returns>
        public bool AddPhoneNumber(PhoneNumber phoneNumber, ProductOrder productOrder)
        {
            phoneNumber ??= new PhoneNumber();
            productOrder ??= new ProductOrder();

            // We're using dictionaries here to prevent duplicates.
            var phoneNumbers = this.PhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

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

                productOrders[productOrder.PortedPhoneNumberId.ToString()] = productOrder;
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

                productOrders[productOrder.VerifiedPhoneNumberId.ToString()] = productOrder;
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
            var products = this.ProductsToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (!string.IsNullOrWhiteSpace(product?.Name) && product?.ProductId == productOrder?.ProductId)
            {
                products[product.ProductId.ToString()] = product;
                Products = products.Values;

                productOrders[productOrder.ProductId.ToString()] = productOrder;
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
            var services = this.ServicesToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (!string.IsNullOrWhiteSpace(service?.Name) && service?.ServiceId == productOrder?.ServiceId)
            {
                services[service.ServiceId.ToString()] = service;
                Services = services.Values;

                productOrders[productOrder.ServiceId.ToString()] = productOrder;
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
            var coupons = this.CouponsToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (!string.IsNullOrWhiteSpace(coupon?.Name) && coupon?.CouponId == productOrder?.CouponId)
            {
                coupons[coupon.CouponId.ToString()] = coupon;
                Coupons = coupons.Values;

                productOrders[productOrder.CouponId.ToString()] = productOrder;
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

            var phoneNumbers = this.PhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

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

            var portedPhoneNumbers = this.PortedPhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (portedPhoneNumber?.PortedPhoneNumberId is not null && portedPhoneNumber.PortedPhoneNumberId == productOrder?.PortedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = portedPhoneNumbers.Remove(portedPhoneNumber.PortedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.PortedPhoneNumberId.ToString());

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

            var verifedPhoneNumbers = this.VerifiedPhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (verifiedPhoneNumber?.VerifiedPhoneNumberId is not null && verifiedPhoneNumber.VerifiedPhoneNumberId == productOrder?.VerifiedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = verifedPhoneNumbers.Remove(verifiedPhoneNumber.VerifiedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.VerifiedPhoneNumberId.ToString());

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

            var products = this.ProductsToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

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

            var services = this.ServicesToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

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

            var coupons = this.CouponsToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            if (coupon.CouponId == productOrder?.CouponId)
            {
                var checkRemoveService = coupons.Remove(coupon.CouponId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.CouponId.ToString());

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
            return PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
        }

        public Dictionary<string, PortedPhoneNumber> PortedPhoneNumbersToDictionary()
        {
            return PortedPhoneNumbers.ToDictionary(x => x.PortedPhoneNumberId.ToString(), x => x);
        }

        public Dictionary<string, VerifiedPhoneNumber> VerifiedPhoneNumbersToDictionary()
        {
            return VerifiedPhoneNumbers.ToDictionary(x => x.VerifiedPhoneNumberId.ToString(), x => x);
        }

        public Dictionary<string, PurchasedPhoneNumber> PurchasedPhoneNumbersToDictionary()
        {
            return PurchasedPhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
        }

        public Dictionary<string, Product> ProductsToDictionary()
        {
            return Products.ToDictionary(x => x.ProductId.ToString(), x => x);
        }

        public Dictionary<string, Service> ServicesToDictionary()
        {
            return Services.ToDictionary(x => x.ServiceId.ToString(), x => x);
        }

        public Dictionary<string, Coupon> CouponsToDictionary()
        {
            return Coupons.ToDictionary(x => x.CouponId.ToString(), x => x);
        }
    }
}
