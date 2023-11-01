using System.Collections.Generic;
using System.Drawing;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
namespace WebScraping
{
    public abstract class ScrapingBase
    {
        public abstract int WorksheetPosition { get; }
        public abstract string Name { get; }
        public IWebDriver Driver;

        ~ScrapingBase()
        {
            // Driver.Close();
        }

        public abstract List<ProductSeller> GetProductsBySearch(string searchingText, int count, out Product product, string backupSearchingText = "", string source = "");
    }
}
