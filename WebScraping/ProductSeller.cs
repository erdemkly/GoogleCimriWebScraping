using System;
using System.Text.RegularExpressions;
namespace WebScraping
{
    public struct ProductSeller
    {
        public readonly string Source;
        public readonly string SellerNameText;
        public readonly string PriceText;
        public readonly float Price;
        public readonly Product Product;
        public readonly string SellerLink;
        public ProductSeller(string sellerNameText, string priceText, Product product, string source, string sellerLink)
        {
            SellerNameText = sellerNameText;
            PriceText = priceText;
            Product = product;
            Source = source;
            SellerLink = sellerLink;
            var regex = new Regex(@"\d{1,5}([.,]\d{1,5})");
            Price = float.Parse(regex.Match(priceText).Value);
        }
    }
}
