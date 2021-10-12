﻿using System;
using System.Threading.Tasks;
using YWB.SiteTranslator.Helpers;

namespace YWB.SiteTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.Unicode;
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine("Sites Translator by Yellow Web ver 1.0b");
            Console.WriteLine("If you like this software, please, donate!");
            DonationHelper.Info();
            //await Task.Delay(5000);
            Console.WriteLine();

            Console.WriteLine("What do you want to do?");
            Console.WriteLine("1. Extract website's text to csv");
            Console.WriteLine("2. Replace website's text from csv");
            var action = YesNoSelector.GetMenuAnswer(2);
            Console.Write("Enter your offer's name (as how it is written in the html):");
            var offerName = Console.ReadLine();
            switch (action)
            {
                case 1:
                {
                    var ex = new HtmlProcessor();
                    var txt = await ex.ExtractTextAsync(offerName);
                    var csv = new CSVProcessor();
                    csv.Write(txt);
                    Console.WriteLine("Text extracted to \"translation.csv\" file in the program's directory.");
                    break;
                }
                case 2:
                {
                    Console.Write("Enter translated offer's name (or Enter if the same):");
                    var newOfferName = Console.ReadLine();
                    if (string.IsNullOrEmpty(newOfferName)) newOfferName = offerName;
                    var csv = new CSVProcessor();
                    var txt = csv.Read();
                    var ex = new HtmlProcessor();
                    await ex.TranslateAsync(offerName, newOfferName, txt);
                    Console.WriteLine("Tranlation saved to \"indext.html\" file in the website's directory.");
                    break;
                }
            }



            Console.WriteLine("All done. Press any key to exit... and don't forget to donate!");
            DonationHelper.Info();
            Console.ReadKey();
        }
    }
}
