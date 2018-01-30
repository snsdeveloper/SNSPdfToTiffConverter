using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFToTiffConverter.Services
{
    /// <summary>
    /// simple logger, logs to the console and to a text file with the date/time and log message
    /// </summary>
    public class TextFileLogger
    {
        string filePath;
        public TextFileLogger(string filePath)
        {
            this.filePath = filePath;
        }
        public void LogIt(string message)
        {

            var messageToLog = string.Format("{0}: {1}", DateTime.Now.ToString(),
                message);
            Console.WriteLine(messageToLog);

            try
            {
                System.IO.File.AppendAllText(filePath, messageToLog + Environment.NewLine);

            }
            catch //don't care if the logger fails
            {

            }


        }
    }
}
