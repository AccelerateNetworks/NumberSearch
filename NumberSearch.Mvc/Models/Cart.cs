using Microsoft.AspNetCore.Http;

using NumberSearch.DataAccess;

using System.Collections.Generic;
using System.Linq;

namespace NumberSearch.Mvc
{
    public class Cart
    {
        public IEnumerable<PhoneNumber> PhoneNumbers { get; set; }
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Service> Services { get; set; }
        public IEnumerable<ProductOrder> ProductOrders { get; set; }
        public Order Order { get; set; }

        enum CartKey
        {
            PhoneNumbers,
            Products,
            Services,
            ProductOrders
        }

        /// <summary>
        /// Get the current Cart from the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static Cart GetFromSession(ISession session)
        {
            var numbers = session.Get<List<PhoneNumber>>(CartKey.PhoneNumbers.ToString());
            var products = session.Get<List<Product>>(CartKey.Products.ToString());
            var service = session.Get<List<Service>>(CartKey.Services.ToString());
            var productOrders = session.Get<List<ProductOrder>>(CartKey.ProductOrders.ToString());

            return new Cart
            {
                PhoneNumbers = numbers ?? new List<PhoneNumber>(),
                Products = products ?? new List<Product>(),
                Services = service ?? new List<Service>(),
                ProductOrders = productOrders ?? new List<ProductOrder>(),
                Order = new Order()
            };
        }

        /// <summary>
        /// Save the cart to the session.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public bool SetToSession(ISession session)
        {
            session.Set<List<PhoneNumber>>(CartKey.PhoneNumbers.ToString(), PhoneNumbers?.ToList());
            session.Set<List<Product>>(CartKey.Products.ToString(), Products?.ToList());
            session.Set<List<Service>>(CartKey.Services.ToString(), Services?.ToList());
            session.Set<List<ProductOrder>>(CartKey.ProductOrders.ToString(), ProductOrders?.ToList());

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
            var phoneNumbers = this.PhoneNumbersToDictionary();
            var productOrders = this.ProductOrdersToDictionary();

            // If it's a valid phone number make sure the keys match.
            if (phoneNumber?.DialedNumber?.Length == 10 && phoneNumber.DialedNumber == productOrder?.DialedNumber)
            {
                phoneNumbers[phoneNumber.DialedNumber] = phoneNumber;
                PhoneNumbers = phoneNumbers.Values.ToArray();

                productOrders[productOrder.DialedNumber] = productOrder;
                ProductOrders = productOrders.Values.ToArray();

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
                Products = products.Values.ToArray();

                productOrders[productOrder.ProductId.ToString()] = productOrder;
                ProductOrders = productOrders.Values.ToArray();

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
                Services = services.Values.ToArray();

                productOrders[productOrder.ServiceId.ToString()] = productOrder;
                ProductOrders = productOrders.Values.ToArray();

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
                    PhoneNumbers = phoneNumbers.Values.ToArray();
                    ProductOrders = productOrders.Values.ToArray();

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
                    Products = products.Values.ToArray();
                    ProductOrders = productOrders.Values.ToArray();

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
                var checkRemoveProductorder = productOrders.Remove(productOrder.ProductId.ToString());

                if (checkRemoveService && checkRemoveProductorder)
                {
                    Services = services.Values.ToArray();
                    ProductOrders = productOrders.Values.ToArray();

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
            return ProductOrders.ToDictionary(x => !string.IsNullOrEmpty(x.DialedNumber) ? x.DialedNumber : x.ProductId == System.Guid.Empty ? x.ServiceId.ToString() : x.ProductId.ToString(), x => x);
        }

        public Dictionary<string, PhoneNumber> PhoneNumbersToDictionary()
        {
            return PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
        }

        public Dictionary<string, Product> ProductsToDictionary()
        {
            return Products.ToDictionary(x => x.ProductId.ToString(), x => x);
        }

        public Dictionary<string, Service> ServicesToDictionary()
        {
            return Services.ToDictionary(x => x.ServiceId.ToString(), x => x);
        }
    }
}
