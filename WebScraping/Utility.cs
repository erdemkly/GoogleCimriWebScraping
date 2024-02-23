using System;
namespace WebScraping
{
    public static class Utility
    {
        public static int CalculateLevenshteinDistance(this string a, string b)
        {
            var sum = 0;
            var aWordList = a.ToLower().Split(" ");
            var bWordList = b.ToLower().Split(" ");
            
            for (var i = 0; i < aWordList.Length; i++)
            {
                for (var j = i; j < bWordList.Length; j++)
                {
                   sum += CalculateLevenshteinDistanceWord(aWordList[i], bWordList[j]);
                }
            }
            return sum;
        }
        public static int CalculateLevenshteinDistanceWord(this string a, string b)
        {
            int[,] distance = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
            {
                for (int j = 0; j <= b.Length; j++)
                {
                    if (i == 0)
                        distance[i, j] = j;
                    else if (j == 0)
                        distance[i, j] = i;
                    else
                        distance[i, j] = Math.Min(
                            Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
                            distance[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1)
                        );
                }
            }

            return distance[a.Length, b.Length];
        }
    }
}
