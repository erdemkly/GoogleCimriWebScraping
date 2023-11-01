// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using OpenQA.Selenium.Chrome;
namespace WebScraping
{
    class SearchingText
    {
        public string searchingText;
        public string backupText;
        public int doneCount;
        public int rowCount;

        public SearchingText(string searchingText, string backupText, int rowCount)
        {
            this.searchingText = searchingText;
            this.backupText = backupText;
            this.rowCount = rowCount;

        }
    }
    static class Program
    {
        static readonly string ResultPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}/result.xlsx";
        static readonly string InputPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}/liste.xlsx";
        private const int SellerPriceCount = 2;
        private static List<SearchingText> _searchingTextList;
        private static int FetchedProductCount;
        private static int ParallelTaskCount;
        private static async Task Main()
        {
            Console.WriteLine($"Created by ~Erdem Kalay~");
            await StartScraping();
        }

        private static float GetPercent()
        {
            float sum = _searchingTextList.Sum(x => x.doneCount);
            return (sum)/_searchingTextList.Count;
        }

        private static DateTime startTime;
        private static async Task StartScraping()
        {
            while (!File.Exists(InputPath))
            {
                Console.Clear();
                Console.WriteLine("liste.xlsx Dosyasını dosya konumuna yerleştirin.");
                Console.WriteLine("Bekleniyor...");
                await Task.Delay(500);
            }
            int input;
            do
            {
                Console.WriteLine("Aynı Anda Kaç Tane Arama Yapılsın (MIN:1 MAX:15)");
            } while (!int.TryParse(Console.ReadLine(), out input));
            ParallelTaskCount = Math.Clamp(input, 1, 15);


            CreateExcelFile(1);

            startTime = DateTime.Now;


            // do
            // {
            //     Console.WriteLine("En Ucuz Kaç Fiyat Gelsin? (MIN:1 MAX:5)");
            // } while (!int.TryParse(Console.ReadLine(), out input));
            // SellerPriceCount = Math.Clamp(input, 1, 5);

            AdjustSearchingTexts();

            var productCount = _searchingTextList.Count;


            Console.WriteLine($"{productCount} Adet ürün bulundu.");

            var sellersProductPair = new Dictionary<Product, List<ProductSeller>>();
            FetchedProductCount = 0;
            List<Task> taskList = new List<Task>();


            var googleQueue = new Queue<SearchingText>();
            foreach (var valueTuple in _searchingTextList)
            {
                googleQueue.Enqueue(valueTuple);
            }

            for (int c = 0; c < ParallelTaskCount; c++)
            {
                var task = Task.Run(async () =>
                {
                    var scraping = new GoogleShoppingScraping();
                    while (googleQueue.TryDequeue(out var search))
                    {
                        var searchText = search.searchingText;
                        var backupSearchText = search.backupText;
                        Console.WriteLine($"<{scraping.Name}> <{searchText}> fiyat getiriliyor.");
                        List<ProductSeller> result = scraping.GetProductsBySearch(searchText, SellerPriceCount, out var p, backupSearchText, scraping.Name);
                        var foundPair = sellersProductPair.Any(x => x.Key.SearchingText == p.SearchingText);
                        if (foundPair)
                        {
                            continue;
                        }
                        sellersProductPair.Add(p, result);
                        ExcelParamsQueue.Enqueue(new ExcelProductParams((p, result), scraping.WorksheetPosition));
                        FetchedProductCount++;

                        _searchingTextList.First(x => x == search).doneCount++;

                        while (_workbookBusy)
                        {
                            await Task.Delay(100);
                        }
                        WriteExcelProduct();
                        Console.WriteLine($"%{GetPercent()*100} Tamamlandı...");
                    }
                    scraping.Driver.Quit();

                });
                taskList.Add(task);



            }

            await Task.WhenAll(taskList);


            Console.WriteLine($"Ürün fiyatları çekildi.");


            Console.WriteLine($"{DateTime.Now.Subtract(startTime)} Sürede bitti.");
            Console.WriteLine("Kapatmak için herhangi bir tuşa basın...");
            Console.ReadKey();
        }


        private static void AdjustSearchingTexts()
        {
            _searchingTextList = new List<SearchingText>();
            using var workbook = new XLWorkbook(InputPath);
            var worksheet = workbook.Worksheet(1);

            var skipFirstRow = false;
            var rowIndex = 2;
            foreach (var row in worksheet.RowsUsed())
            {
                if (!skipFirstRow)
                {
                    skipFirstRow = true;
                    continue;
                }
                _searchingTextList.Add(new SearchingText(row.Cell(1).GetValue<string>(), row.Cell(2).GetValue<string>(), rowIndex));
                rowIndex++;
            }

        }

        private static void CreateExcelFile(int worksheetCount)
        {
            using var workbook = new XLWorkbook();

            for (int i = 0; i < worksheetCount; i++)
            {
                var worksheet = workbook.Worksheets.Add($"Fiyat Araştırması. {i + 1}");

                //worksheet.Cell(1, 1).Value = "Ürün Resmi";
                //worksheet.Column(1).Width = 20;

                worksheet.Cell(1, 1).Value = "Ürün Barkod/Adı";
                worksheet.Column(2).Width = 15;

                worksheet.Cell(1, 2).Value = "Ürün Adı";
                worksheet.Column(3).Width = 60;
            }


            workbook.SaveAs(ResultPath);



        }

        private static readonly Dictionary<string, int> SourceList = new Dictionary<string, int>()
        {
            {
                "Min", 6
            }
        };

        private static bool TryAddNewSource(string source, out int column, int worksheetPos)
        {
            using var workbook = new XLWorkbook(ResultPath);
            var worksheet = workbook.Worksheet(worksheetPos);

            column = -1;
            if (string.IsNullOrWhiteSpace(source)) return false;
            if (SourceList.ContainsKey(source))
            {
                column = SourceList[source];
                return true;
            }
            column = SourceList.Max(x => x.Value) + 1;
            worksheet.Cell(1, column).Value = source;
            SourceList.Add(source, column);
            workbook.Save();
            return true;

        }


        private struct ExcelProductParams
        {
            public readonly (Product, List<ProductSeller>) Pair;
            public readonly int WorksheetPos;
            public ExcelProductParams((Product, List<ProductSeller>) pair, int worksheetPos)
            {
                this.Pair = pair;
                this.WorksheetPos = worksheetPos;
            }
        }
        private static readonly Queue<ExcelProductParams> ExcelParamsQueue = new Queue<ExcelProductParams>();
        private static bool _workbookBusy;
        private static async void WriteExcelProduct()
        {
            while (ExcelParamsQueue.Count <= 0 || _workbookBusy)
            {
                await Task.Delay(100);
            }
            _workbookBusy = true;
            var args = ExcelParamsQueue.Dequeue();
            using var workbook = new XLWorkbook(ResultPath);
            IXLWorksheet worksheet;
            
            try
            {
                worksheet = workbook.Worksheet(args.WorksheetPos);
            }
            catch (ArgumentException e)
            {
                Console.WriteLine(e);
                return;
            }

            if (args.Pair.Item2.Count == 0)
            {
                _workbookBusy = false;
                return;
            }

            //worksheet.Cell(_rows[worksheetPos - 1], 1).FormulaA1 = ($"=IMAGE(\"{pair.Item1.ImageSrc}\")");

            var row = _searchingTextList.First(x => x.backupText == args.Pair.Item1.SearchingText || x.searchingText == args.Pair.Item1.SearchingText).rowCount;
            worksheet.Cell(row, 1).Value = ($"{args.Pair.Item1.SearchingText}");
            worksheet.Cell(row, 2).Value = ($"{args.Pair.Item1.ProductName}");

            for (var i = 0; i < args.Pair.Item2.Count; i++)
            {
                var seller = args.Pair.Item2[i];
                if (!TryAddNewSource(seller.Source, out var column, args.WorksheetPos))
                {
                    Console.WriteLine($"YAZILAMADI {seller.Source}");
                    continue;
                }
                worksheet.Cell(row, 7 + i).FormulaA1 = $"=HYPERLINK(\"{seller.SellerLink}\",\"{seller.PriceText}\")";

                // worksheet.Cell(row, 7 + i).Value = $"{seller.PriceText}";
                worksheet.Column(7).Width = 10;
            }


            workbook.Save();
            _workbookBusy = false;
        }
    }
}
