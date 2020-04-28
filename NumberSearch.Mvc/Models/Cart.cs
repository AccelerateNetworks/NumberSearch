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
        public IEnumerable<PortedPhoneNumber> PortedPhoneNumbers { get; set; }
        public Order Order { get; set; }

        enum CartKey
        {
            PhoneNumbers,
            Products,
            Services,
            ProductOrders,
            PortedPhoneNumbers,
            Order
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
            var portedPhoneNumbers = session.Get<List<PortedPhoneNumber>>(CartKey.PortedPhoneNumbers.ToString());
            var order = session.Get<Order>(CartKey.Order.ToString());

            return new Cart
            {
                PhoneNumbers = numbers ?? new List<PhoneNumber>(),
                Products = products ?? new List<Product>(),
                Services = service ?? new List<Service>(),
                ProductOrders = productOrders ?? new List<ProductOrder>(),
                PortedPhoneNumbers = portedPhoneNumbers ?? new List<PortedPhoneNumber>(),
                Order = order ?? new Order()
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
            session.Set<List<PortedPhoneNumber>>(CartKey.PortedPhoneNumbers.ToString(), PortedPhoneNumbers?.ToList());
            session.Set<Order>(CartKey.Order.ToString(), Order);

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
            if (portedPhoneNumber?.PortedDialedNumber?.Length == 10 && portedPhoneNumber.PortedDialedNumber == productOrder?.PortedDialedNumber)
            {
                portedPhoneNumbers[portedPhoneNumber.PortedDialedNumber] = portedPhoneNumber;
                PortedPhoneNumbers = portedPhoneNumbers.Values.ToArray();

                productOrders[productOrder.PortedDialedNumber] = productOrder;
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

            if (portedPhoneNumber?.PortedDialedNumber?.Length == 10 && portedPhoneNumber.PortedDialedNumber == productOrder?.PortedDialedNumber)
            {
                var checkRemovePortedPhoneNumber = portedPhoneNumbers.Remove(portedPhoneNumber.PortedDialedNumber);
                var checkRemoveProductOrder = productOrders.Remove(productOrder.PortedDialedNumber);

                if (checkRemovePortedPhoneNumber && checkRemoveProductOrder)
                {
                    PortedPhoneNumbers = portedPhoneNumbers.Values.ToArray();
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
                var checkRemoveProductorder = productOrders.Remove(productOrder.ServiceId.ToString());

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
            return ProductOrders.ToDictionary(x => !string.IsNullOrEmpty(x.DialedNumber) ? x.DialedNumber : !string.IsNullOrEmpty(x.PortedDialedNumber) ? x.PortedDialedNumber : x.ProductId == System.Guid.Empty ? x.ServiceId.ToString() : x.ProductId.ToString(), x => x);
        }

        public Dictionary<string, PhoneNumber> PhoneNumbersToDictionary()
        {
            return PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
        }

        public Dictionary<string, PortedPhoneNumber> PortedPhoneNumbersToDictionary()
        {
            return PortedPhoneNumbers.ToDictionary(x => x.PortedDialedNumber, x => x);
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
