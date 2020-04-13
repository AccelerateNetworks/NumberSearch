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
        public IEnumerable<ProductOrder> ProductOrders { get; set; }
        public Order Order { get; set; }

        enum CartKey
        {
            PhoneNumbers,
            Products,
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
            var productOrders = session.Get<List<ProductOrder>>(CartKey.ProductOrders.ToString());

            return new Cart
            {
                PhoneNumbers = numbers == null ? new List<PhoneNumber>() : numbers,
                Products = products == null ? new List<Product>() : products,
                ProductOrders = productOrders == null ? new List<ProductOrder>() : productOrders,
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
            try
            {
                session.Set<List<PhoneNumber>>(CartKey.PhoneNumbers.ToString(), PhoneNumbers?.ToList());
                session.Set<List<Product>>(CartKey.Products.ToString(), Products?.ToList());
                session.Set<List<ProductOrder>>(CartKey.ProductOrders.ToString(), ProductOrders?.ToList());

                return true;
            }
            catch
            {
                // We don't expect this to fail.
                return false;
            }
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
            var phoneNumbers = PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
            var productOrders = ProductOrders.ToDictionary(x => !string.IsNullOrEmpty(x.DialedNumber) ? x.DialedNumber : x.ProductId.ToString(), x => x);

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
        /// Adda Product to the Cart.
        /// </summary>
        /// <param name="product"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool AddProduct(Product product, ProductOrder productOrder)
        {
            var products = Products?.ToDictionary(x => x?.ProductId.ToString(), x => x);
            var productOrders = ProductOrders?.ToDictionary(x => !string.IsNullOrEmpty(x?.DialedNumber) ? x?.DialedNumber : x?.ProductId.ToString(), x => x);

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
        /// Remove a phone number from the Cart.
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="productOrder"></param>
        /// <returns></returns>
        public bool RemovePhoneNumber(PhoneNumber phoneNumber, ProductOrder productOrder)
        {
            phoneNumber ??= new PhoneNumber();
            productOrder ??= new ProductOrder();

            var phoneNumbers = PhoneNumbers.ToDictionary(x => x.DialedNumber, x => x);
            var productOrders = ProductOrders.ToDictionary(x => !string.IsNullOrEmpty(x.DialedNumber) ? x.DialedNumber : x.ProductId.ToString(), x => x);

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

            var products = Products.ToDictionary(x => x.ProductId.ToString(), x => x);
            var productOrders = ProductOrders.ToDictionary(x => !string.IsNullOrEmpty(x.DialedNumber) ? x.DialedNumber : x.ProductId.ToString(), x => x);

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
    }
}
