﻿using Microsoft.AspNetCore.Http;

using NumberSearch.DataAccess;
using NumberSearch.DataAccess.Models;

using System.Collections.Generic;
using System.Linq;

using ZLinq;

namespace NumberSearch.Mvc.Models
{
    public class Cart
    {
        public List<PhoneNumber> PhoneNumbers { get; set; } = [];
        public List<Product> Products { get; set; } = [];
        public List<Service> Services { get; set; } = [];
        public List<ProductOrder> ProductOrders { get; set; } = [];
        public List<PortedPhoneNumber> PortedPhoneNumbers { get; set; } = [];
        public List<VerifiedPhoneNumber> VerifiedPhoneNumbers { get; set; } = [];
        public List<PurchasedPhoneNumber> PurchasedPhoneNumbers { get; set; } = [];
        public List<Coupon> Coupons { get; set; } = [];
        public ProductItem Shipment { get; set; } = new();
        public Order Order { get; set; } = new();

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
            var cart = session.Get<Cart>("Cart");

            return new Cart
            {
                PhoneNumbers = cart?.PhoneNumbers ?? [],
                Products = cart?.Products ?? [],
                Services = cart?.Services ?? [],
                ProductOrders = cart?.ProductOrders ?? [],
                PortedPhoneNumbers = cart?.PortedPhoneNumbers ?? [],
                VerifiedPhoneNumbers = cart?.VerifiedPhoneNumbers ?? [],
                PurchasedPhoneNumbers = cart?.PurchasedPhoneNumbers ?? [],
                Coupons = cart?.Coupons ?? [],
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
            session.Set("Cart", this);
            return true;
        }

        /// <summary>
        /// Add a PhoneNumber to the Cart.
        /// </summary>
        /// <param name="newPhoneNumber"></param>
        /// <returns></returns>
        public bool AddPhoneNumber(ref PhoneNumber phoneNumber, ref ProductOrder productOrder)
        {
            // We're using dictionaries here to prevent duplicates.
            var phoneNumbers = PhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (phoneNumber?.DialedNumber?.Length is 10 && phoneNumber.DialedNumber == productOrder?.DialedNumber)
            {
                phoneNumbers[phoneNumber.DialedNumber] = phoneNumber;
                PhoneNumbers = [.. phoneNumbers.Values];

                productOrders[productOrder.DialedNumber] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool AddPortedPhoneNumber(ref PortedPhoneNumber portedPhoneNumber, ref ProductOrder productOrder)
        {
            // We're using dictionaries here to prevent duplicates.
            var portedPhoneNumbers = PortedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (portedPhoneNumber?.PortedPhoneNumberId is not null && portedPhoneNumber.PortedPhoneNumberId == productOrder?.PortedPhoneNumberId)
            {
                portedPhoneNumbers[portedPhoneNumber.PortedPhoneNumberId.ToString()] = portedPhoneNumber;
                PortedPhoneNumbers = [.. portedPhoneNumbers.Values];

                productOrders[productOrder.PortedPhoneNumberId?.ToString() ?? string.Empty] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool AddVerifiedPhoneNumber(ref VerifiedPhoneNumber verifiedPhoneNumber,ref ProductOrder productOrder)
        {
            // We're using dictionaries here to prevent duplicates.
            var verifiedPhoneNumbers = VerifiedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (verifiedPhoneNumber?.VerifiedPhoneNumberId is not null && verifiedPhoneNumber.VerifiedPhoneNumberId == productOrder?.VerifiedPhoneNumberId)
            {
                verifiedPhoneNumbers[verifiedPhoneNumber.VerifiedPhoneNumberId.ToString()] = verifiedPhoneNumber;
                VerifiedPhoneNumbers = [.. verifiedPhoneNumbers.Values];

                productOrders[productOrder.VerifiedPhoneNumberId?.ToString() ?? string.Empty] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool AddProduct(ref Product product, ref ProductOrder productOrder)
        {
            var products = ProductsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (product is not null && productOrder is not null && !string.IsNullOrWhiteSpace(product?.Name) && product?.ProductId == productOrder?.ProductId)
            {
                products[product!.ProductId.ToString()] = product;
                Products = [.. products.Values];

                productOrders[productOrder!.ProductId.ToString()] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool AddService(ref Service service, ref ProductOrder productOrder)
        {
            var services = ServicesToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (service is not null && productOrder is not null && !string.IsNullOrWhiteSpace(service?.Name) && service?.ServiceId == productOrder?.ServiceId)
            {
                services[service!.ServiceId.ToString()] = service;
                Services = [.. services.Values];

                productOrders[productOrder!.ServiceId.ToString()] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool AddCoupon(ref Coupon coupon, ref ProductOrder productOrder)
        {
            var coupons = CouponsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (coupon is not null && productOrder is not null && !string.IsNullOrWhiteSpace(coupon?.Name) && productOrder.CouponId is not null && coupon?.CouponId == productOrder?.CouponId)
            {
                coupons[coupon!.CouponId.ToString()] = coupon;
                Coupons = [.. coupons.Values];

                productOrders[productOrder!.CouponId.ToString() ?? string.Empty] = productOrder;
                ProductOrders = [.. productOrders.Values];

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
        public bool RemovePhoneNumber(ref PhoneNumber phoneNumber, ref ProductOrder productOrder)
        {
            var phoneNumbers = PhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (phoneNumber?.DialedNumber?.Length == 10 && phoneNumber.DialedNumber == productOrder?.DialedNumber)
            {
                var checkRemovePhoneNumber = phoneNumbers.Remove(phoneNumber.DialedNumber);
                var checkRemoveProductOrder = productOrders.Remove(productOrder.DialedNumber);

                if (checkRemovePhoneNumber && checkRemoveProductOrder)
                {
                    PhoneNumbers = [.. phoneNumbers.Values];
                    ProductOrders = [.. productOrders.Values];

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
        public bool RemovePortedPhoneNumber(ref PortedPhoneNumber portedPhoneNumber, ref ProductOrder productOrder)
        {
            var portedPhoneNumbers = PortedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (portedPhoneNumber?.PortedPhoneNumberId is not null && portedPhoneNumber.PortedPhoneNumberId == productOrder?.PortedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = portedPhoneNumbers.Remove(portedPhoneNumber.PortedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.PortedPhoneNumberId?.ToString() ?? string.Empty);

                if (checkRemovePortedPhoneNumber && checkRemoveProductOrder)
                {
                    PortedPhoneNumbers = [.. portedPhoneNumbers.Values];
                    ProductOrders = [.. productOrders.Values];

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
        public bool RemoveVerifiedPhoneNumber(ref VerifiedPhoneNumber verifiedPhoneNumber, ref ProductOrder productOrder)
        {
            var verifedPhoneNumbers = VerifiedPhoneNumbersToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (verifiedPhoneNumber?.VerifiedPhoneNumberId is not null && verifiedPhoneNumber.VerifiedPhoneNumberId == productOrder?.VerifiedPhoneNumberId)
            {
                var checkRemovePortedPhoneNumber = verifedPhoneNumbers.Remove(verifiedPhoneNumber.VerifiedPhoneNumberId.ToString());
                var checkRemoveProductOrder = productOrders.Remove(productOrder.VerifiedPhoneNumberId?.ToString() ?? string.Empty);

                if (checkRemovePortedPhoneNumber && checkRemoveProductOrder)
                {
                    VerifiedPhoneNumbers = [.. verifedPhoneNumbers.Values];
                    ProductOrders = [.. productOrders.Values];

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
        public bool RemoveProduct(ref Product product, ref ProductOrder productOrder)
        {
            var products = ProductsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (product.ProductId == productOrder?.ProductId)
            {
                var checkRemoveProduct = products.Remove(product.ProductId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.ProductId.ToString());

                if (checkRemoveProduct && checkRemoveProductorder)
                {
                    Products = [.. products.Values];
                    ProductOrders = [.. productOrders.Values];

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
        public bool RemoveService(ref Service service, ref ProductOrder productOrder)
        {
            var services = ServicesToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (service.ServiceId == productOrder?.ServiceId)
            {
                var checkRemoveService = services.Remove(service.ServiceId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.ServiceId.ToString());

                if (checkRemoveService && checkRemoveProductorder)
                {
                    Services = [.. services.Values];
                    ProductOrders = [.. productOrders.Values];

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
        public bool RemoveCoupon(ref Coupon coupon, ref ProductOrder productOrder)
        {
            var coupons = CouponsToDictionary();
            var productOrders = ProductOrdersToDictionary();

            if (coupon.CouponId == productOrder?.CouponId)
            {
                var checkRemoveService = coupons.Remove(coupon.CouponId.ToString());
                var checkRemoveProductorder = productOrders.Remove(productOrder.CouponId?.ToString() ?? string.Empty);

                if (checkRemoveService && checkRemoveProductorder)
                {
                    Coupons = [.. coupons.Values];
                    ProductOrders = [.. productOrders.Values];

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

            if (ProductOrders is not null && ProductOrders.Count != 0)
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
                return [];
            }
        }

        public Dictionary<string, PhoneNumber> PhoneNumbersToDictionary()
        {
            return PhoneNumbers is not null ? PhoneNumbers.AsValueEnumerable().ToDictionary(x => x.DialedNumber, x => x) : [];
        }

        public Dictionary<string, PortedPhoneNumber> PortedPhoneNumbersToDictionary()
        {
            return PortedPhoneNumbers is not null ? PortedPhoneNumbers.AsValueEnumerable().ToDictionary(x => x.PortedPhoneNumberId.ToString(), x => x) : [];
        }

        public Dictionary<string, VerifiedPhoneNumber> VerifiedPhoneNumbersToDictionary()
        {
            return VerifiedPhoneNumbers is not null ? VerifiedPhoneNumbers.AsValueEnumerable().ToDictionary(x => x.VerifiedPhoneNumberId.ToString(), x => x) : [];
        }

        public Dictionary<string, PurchasedPhoneNumber> PurchasedPhoneNumbersToDictionary()
        {
            return PurchasedPhoneNumbers is not null ? PurchasedPhoneNumbers.AsValueEnumerable().ToDictionary(x => x.DialedNumber, x => x) : [];
        }

        public Dictionary<string, Product> ProductsToDictionary()
        {
            return Products is not null ? Products.AsValueEnumerable().ToDictionary(x => x.ProductId.ToString(), x => x) : [];
        }

        public Dictionary<string, Service> ServicesToDictionary()
        {
            return Services is not null ? Services.AsValueEnumerable().ToDictionary(x => x.ServiceId.ToString(), x => x) : [];
        }

        public Dictionary<string, Coupon> CouponsToDictionary()
        {
            return Coupons is not null ? Coupons.AsValueEnumerable().ToDictionary(x => x.CouponId.ToString(), x => x) : [];
        }
    }
}
