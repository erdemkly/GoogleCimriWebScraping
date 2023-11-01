using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
namespace WebScraping
{
    public class CimriScraping : ScrapingBase
    {

        public CimriScraping()
        {
            var cimriChromeOptions = new ChromeOptions();
            cimriChromeOptions.EnableMobileEmulation("iPhone XR");
            cimriChromeOptions.AddArgument("--user-agent=Mozilla/5.0 (iPhone; CPU iPhone OS 13_2 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.0.1 Mobile/15E148 Safari/604.1");

            Driver = new ChromeDriver(cimriChromeOptions);
        }
        public override int WorksheetPosition { get => 2; }
        public override string Name { get => "Cimri"; }
        public override List<ProductSeller> GetProductsBySearch(string searchingText, int count, out Product product, string backupSearchingText = "", string source = "")
        {
            if (string.IsNullOrWhiteSpace(backupSearchingText))
            {
                product = new Product("BULUNAMADI!", "", searchingText);
                return new List<ProductSeller>();
            }
            searchingText = backupSearchingText;
            backupSearchingText = "";
            Driver.Navigate().GoToUrl($"https://www.cimri.com/arama?q={searchingText}");
            var waitElement = new WebDriverWait(Driver, TimeSpan.FromSeconds(55));

            try
            {
                var policy = waitElement.Until((d) => d.FindElement(By.CssSelector("#onetrust-accept-btn-handler")));

                policy?.Click();
            }
            catch (WebDriverTimeoutException e) { }

            // try
            // {
            //     var openSearchBoxElement = waitElement.Until((d) => d.FindElement(By.ClassName("SearchBox_searchBoxContainer__qAcB5")));
            //     openSearchBoxElement?.Click();
            // }
            // catch (WebDriverTimeoutException e)
            // {
            //     Console.WriteLine("Beklenmeyen hata!");
            //     
            //     if (!string.IsNullOrWhiteSpace(backupSearchingText))
            //     {
            //         Console.WriteLine($"~{backupSearchingText}~ ile tekrar deneniyor.");
            //         product = new Product();
            //         return GetProductsBySearch(backupSearchingText, count, out product);
            //     }
            //     product = new Product();
            //     return new List<ProductSeller>();
            // }
            //
            //
            // var searchBoxElement = Driver.FindElement(By.Id("search-input"));
            // searchBoxElement.SendKeys($"{searchingText}");
            // searchBoxElement.SendKeys(Keys.Enter);




            try
            {
                var cimriProductElement = waitElement.Until((d) =>
                {
                    var parent = d.FindElement(By.Id("products-list-selected"));
                    return parent.FindElement(By.XPath("//*[@data-id=\"cimri-product\"]/a"));
                });
                if (cimriProductElement is {Displayed: true})
                {
                    Driver.Navigate().GoToUrl(cimriProductElement.GetAttribute("href"));
                }
            }
            catch (WebDriverTimeoutException e)
            {
                Console.WriteLine($"<{Name}>{searchingText} Ürünü bulunamadı.");
                if (!string.IsNullOrWhiteSpace(backupSearchingText))
                {
                    Console.WriteLine($"~{backupSearchingText}~ ile tekrar deneniyor.");
                    product = new Product();
                    return GetProductsBySearch(backupSearchingText, count, out product, "", source);
                }
                product = new Product();
                return new List<ProductSeller>();
            }


            //var imageElement = Driver.FindElement(By.XPath("//*[@id=\"product-detail-full-img-0\"]"));
            //var imageSrc = imageElement.GetAttribute("src");
            var productNameElement = Driver.FindElement(By.XPath("//*[@id=\"products-list-selected\"]"));
            var produrctName = productNameElement.Text;

            var result = new List<ProductSeller>();
            try
            {
                var cimriOffers = waitElement.Until((d) => d.FindElement(By.Id("offers-wrapper")));

                var offerParentChildren = cimriOffers.FindElements(By.XPath(".//a"));

                var allOffers = offerParentChildren.ToList();
                product = new Product(produrctName, "", searchingText);
                var productSellers = new List<ProductSeller>();
                foreach (var offerElement in allOffers)
                {
                    var sellerName = offerElement.FindElement(By.XPath($".//div[2]/div[2]/div[1]/div/img")).GetAttribute("alt");
                    var priceText = offerElement.GetAttribute("data-price");

                    if (int.TryParse(priceText, out _))
                    {
                        priceText += ".00";
                    }
                    productSellers.Add(new ProductSeller(sellerName, $"₺{priceText}", product, source, sellerLink: ""));
                }

                var orderedByPrice = productSellers.OrderBy(x => x.Price).ToList();
                for (var i = 0; i < Math.Min(orderedByPrice.Count, count); i++)
                {
                    result.Add(orderedByPrice[i]);
                }


            }
            catch (WebDriverTimeoutException e)
            {
                Console.WriteLine($"{searchingText} Ürünü için teklif bulunamadı.");
                if (string.IsNullOrWhiteSpace(backupSearchingText))
                {
                    product = new Product();
                    return result;
                }
                else
                {
                    product = new Product();
                    return GetProductsBySearch(backupSearchingText, count, out product, "", source);
                }
            }

            return result;
        }
    }
}
