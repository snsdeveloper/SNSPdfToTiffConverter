using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace PDFToTiffConverter.Services
{
    public class FileUtils
    {
        public static bool IsFileReady(string path)
        {
            try
            {

                //If we can't open the file, it's still being used by another process
                using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    
                    return true;
                }

            }
            catch (IOException)
            {
                return false;
            }
        }
        public static bool NeedsRotation(string file)
        {
            //we need to rotate any pdf files that are landscape
            PdfReader pdfReader = new PdfReader(file);
            var size = pdfReader.GetPageSize(1);
            var height = size.Height / 72;
            var width = size.Width / 72;
            if (width == 11 || width == 14)
            {
                return true;
            }
            pdfReader.Close();
            return false;
        }
        public static int GetPageCount(string path)
        {
            //get the page count of a tiff
            var uri = new System.Uri(path);
            TiffBitmapDecoder decoder = new TiffBitmapDecoder(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
            var count = decoder.Frames.Count;
            return count;

        }
         

    }
}
