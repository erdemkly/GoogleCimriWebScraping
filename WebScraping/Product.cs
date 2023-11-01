namespace WebScraping
{

    public class Product
    {
        public readonly string SearchingText;
        public readonly string ProductName;
        public readonly string ImageSrc;

        public Product()
        {
            
        }
        public Product(string productName, string imageSrc, string searchingText)
        {
            ImageSrc = imageSrc;
            SearchingText = searchingText;
            ProductName = productName;
        }
    }
}
