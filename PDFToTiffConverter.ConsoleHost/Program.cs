using PDFToTiffConverter.Services;
using System;
/// <summary>
/// This project is licensed under the terms of the AGPL (GNU Affero General Public License)
/// </summary>
namespace PDFToTiffConverter.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            Host.Start();
            Console.WriteLine("Started");
            Console.ReadKey();
            Host.Stop();
        }
    }
}
