using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
namespace WebScraping
{


    public class GoogleShoppingScraping : ScrapingBase
    {
        public override int WorksheetPosition { get => 1; }
        public override string Name { get => "Google Shopping"; }
        public override List<ProductSeller> GetProductsBySearch(string searchingText, int count, out Product product, string backupSearchingText = "", string source = "")
        {
            Driver.Navigate().GoToUrl($"https://www.google.com/search?tbm=shop&q={searchingText}");

            var waitProduct = new WebDriverWait(Driver, TimeSpan.FromSeconds(3));

            try
            {
                var productElement = waitProduct.Until((d) =>
                {
                    var firstProduct = d.FindElements(By.XPath("//*[@id=\"rso\"]/div/div[2]/div/div[1]/div[1]/div[2]/span/a"));
                    if (firstProduct.Any() && firstProduct.First().Displayed)
                    {
                        return firstProduct.First();
                    }
                    else
                    {
                        return null;
                    }
                });
                productElement?.Click();

            }
            catch (WebDriverTimeoutException e)
            {
                Console.WriteLine($"{searchingText} Ürünü bulunamadı.");
                if (!string.IsNullOrWhiteSpace(backupSearchingText))
                {
                    Console.WriteLine($"~{backupSearchingText}~ ile tekrar deneniyor.");
                    return GetProductsBySearch(backupSearchingText, count, out product, "", source);
                }

                product = new Product("", "", searchingText);
                return new List<ProductSeller>();

            }
            catch (StaleElementReferenceException e)
            {
                Console.WriteLine($"{searchingText} Ürünü bulunamadı.");
                if (!string.IsNullOrWhiteSpace(backupSearchingText))
                {
                    Console.WriteLine($"~{backupSearchingText}~ ile tekrar deneniyor.");
                    return GetProductsBySearch(backupSearchingText, count, out product, "", source);
                }

                product = new Product("", "", searchingText);
                return new List<ProductSeller>();
            }




            var waitShowDetails = new WebDriverWait(Driver, TimeSpan.FromSeconds(3));
            try
            {
                var showDetailsElement = waitShowDetails.Until((d) =>
                {
                    var showDetails = d.FindElement(By.LinkText(@"Ürün ayrıntılarını göster"));
                    if (showDetails.Displayed)
                    {
                        return showDetails;
                    }

                    var elements = d.FindElements(By.ClassName("xCpuod"));
                    if (elements.Any() && elements.First().Displayed)
                    {
                        return elements.First();
                    }
                    return null;

                });
                showDetailsElement?.Click();
            }
            catch (WebDriverTimeoutException e)
            {
                product = new Product("", "", searchingText);
                return new List<ProductSeller>();
            }
            catch (ElementClickInterceptedException e)
            {
                product = new Product("", "", searchingText);
                return new List<ProductSeller>();
            }

            var imageSrc = "";
            try
            {
                var imageElement = Driver.FindElement(By.CssSelector("#sg-product__pdp-container > div > div:nth-child(2) > div.HRKRR > div.YrJ7P.i95lR > div > div > div.wTvWSc > div > img"));
                imageSrc = imageElement.GetAttribute("src");
            }
            catch (Exception e)
            {
                // ignored
            }


            var productNameElement = Driver.FindElement(By.XPath("//*[@id=\"sg-product__pdp-container\"]/div/div[2]/div[1]/div/span"));
            product = new Product(productNameElement.Text, imageSrc, searchingText);

            var table = Driver.FindElement(By.ClassName("dOwBOc"));
            var sellersElement = table.FindElements(By.ClassName("sh-osd__offer-row"));

            List<ProductSeller> sellers = new List<ProductSeller>();
            var priceElementWait = new WebDriverWait(Driver, TimeSpan.FromSeconds(1));
            foreach (var sellerElement in sellersElement) //*[@id="sh-osd__online-sellers-cont"]/tr[1]/td[1]/div[1]/a
            {
                var nameElement = sellerElement.FindElement(By.XPath(".//td/div[1]"));
                try
                {
                    var priceElement = priceElementWait.Until(_ =>
                    {
                        var priceElement = sellerElement.FindElements(By.XPath(".//td[4]/div[1]"));
                        if (priceElement.Any() && priceElement.First().Displayed)
                        {
                            return priceElement.First();
                        }
                        priceElement = sellerElement.FindElements(By.XPath(".//td[4]/div/div[1]"));
                        if (priceElement.Any() && priceElement.First().Displayed)
                        {
                            Console.WriteLine(priceElement.First().Text);
                            return priceElement.First();
                        }
                        return null;
                    });

                    var sellerLinkElement = priceElementWait.Until(_ =>
                    {
                        var sellerLinkElements = sellerElement.FindElements(By.XPath(".//td[1]/div[1]/a"));
                        if (sellerLinkElements.Any() && sellerLinkElements.First().Displayed)
                        {
                            return sellerLinkElements.First();
                        }
                        return null;
                    });

                    var sellerLink = sellerLinkElement == null ? "" : sellerLinkElement.GetAttribute("href");

                    var seller = new ProductSeller(nameElement.Text, priceElement == null ? "" : priceElement.Text, product, source, sellerLink);
                    sellers.Add(seller);
                }
                catch (WebDriverTimeoutException e)
                {
                    Console.WriteLine($"Hata: {e.Message}");
                }


            }

            var orderedByPrice = sellers.OrderBy(x => x.Price).ToList();

            var result = new List<ProductSeller>();
            for (var i = 0; i < Math.Min(orderedByPrice.Count, count); i++)
            {
                result.Add(orderedByPrice[i]);
            }


            return result;
        }
        public GoogleShoppingScraping()
        {
            Driver = new ChromeDriver();
        }
    }
}
