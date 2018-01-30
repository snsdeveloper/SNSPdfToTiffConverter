using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
/// <summary>
/// This project is licensed under the terms of the AGPL (GNU Affero General Public License)
/// </summary>
namespace PDFToTiffConverter.Services
{
    public static class Converter
    {
        /// <summary>
        /// this uses ghostscript so ghostscript needs to be installed on the machine running middle man
        /// http://www.ghostscript.com/download/gsdnld.html
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        public static void Convert(string input, string output, bool rotate)
        {
            string batchFilePath = BuildTempBatchFileName();
            //
            string gsPath = GetGhostScriptPath();
            //this will run a postscript command that takes the pdf input file and outputs a tiff version to the output path

            string batchCommand = BuildGhostScriptCommand(gsPath, input);
            //send it through ghost script first then we'll be adjusting the size
            //so teh first pass through ghostscript, save with a different file name
            var tempConvertOutput = output + "temp";
            batchCommand = batchCommand.Replace("@OutputFile", tempConvertOutput);

            System.IO.File.WriteAllText(batchFilePath, batchCommand);
            //start the batch file and then wait for completion
            Process pr2 = new Process();
            pr2.StartInfo.FileName = batchFilePath;
            pr2.StartInfo.UseShellExecute = false;
            //redirects are necessary to avoid errors running it this way
            pr2.StartInfo.RedirectStandardError = true;
            pr2.StartInfo.RedirectStandardInput = true;
            pr2.StartInfo.RedirectStandardOutput = true;
            pr2.Start();

            pr2.WaitForExit();
            //check for errors
            string error = pr2.StandardError.ReadToEnd();
            pr2.WaitForExit();
            DeleteFileIgnoreError(batchFilePath);
            if (!string.IsNullOrEmpty(error))
            {
                throw new ApplicationException(error);
            }
            AdjustImageSize(tempConvertOutput, output, rotate);
            ArchiveFile(input);

        }

        private static void DeleteFileIgnoreError(string batchFilePath)
        {
            try
            {
                System.IO.File.Delete(batchFilePath);
            }
            catch
            {

            }
        }

        private static string BuildGhostScriptCommand(string gsPath, string inputFile)
        {
            var batchCommand= "\"" + gsPath + "\"   -sDEVICE=tiffg4     -dBATCH -r600 -q -dAdjustWidth=0 -dAlignToPixels=0 -dGridFitTT=0 -dTextAlphaBits=1 -dGraphicsAlphaBits=1  -dNOPAUSE -sOutputFile=\"@OutputFile\" \"@InputFile\" -c quit";
            return batchCommand.Replace("@InputFile", inputFile);
        }

        private static string GetGhostScriptPath()
        {
            var gsPath = System.Configuration.ConfigurationManager.AppSettings["ghostScriptLocation"];
            if (!System.IO.File.Exists(gsPath))
            {
                throw new ApplicationException("Ghost Script executable not found, check the ghostScriptLocation in the app.config and make sure it's the path and file name to the ghostscript exe");
            }

            return gsPath;
        }

        private static string BuildTempBatchFileName()
        {
            var batchFile = System.IO.Path.GetTempFileName();
            batchFile = batchFile.Replace(".tmp", ".bat");
            return batchFile;
        }

        private static void ArchiveFile(string input)
        {
            var directory = System.IO.Path.GetDirectoryName(input);
             
            var archive = System.IO.Path.Combine(directory, "ARCHIVE");
            if (!System.IO.Directory.Exists(archive)) System.IO.Directory.CreateDirectory(archive);
            System.IO.File.Move(input, System.IO.Path.Combine(archive, System.IO.Path.GetFileName(input)));

        }

        private static void AdjustImageSize(string inputFilePath, string outputFilePath, bool rotate)
        {
            var pages = new List<byte[]>();


            ImageCodecInfo encoder = GetEncoderInfo("image/tiff");
            var pageCount = FileUtils.GetPageCount(inputFilePath);
            //extract pages
            using (var img = System.Drawing.Image.FromFile(inputFilePath))
            {

                for (int i = 0; i < pageCount; i++)
                {
                    img.SelectActiveFrame(FrameDimension.Page, i);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        img.Save(ms, encoder, null);
                        pages.Add(ms.ToArray());
                    }
                }
            }
            if (rotate)
            {
                Rotate(pages, encoder);

            }
            BuildMultipageTiff(pages, encoder, outputFilePath);
            DeleteTmpFile(inputFilePath);
        }

        private static void DeleteTmpFile(string inputFilePath)
        {
            try
            {
                System.IO.File.Delete(inputFilePath);
            }
            catch (Exception ex)
            {
                //try again in a few seconds, may not be released by the tiff conversion yet
                System.Threading.Thread.Sleep(2000);
                try
                {
                    System.IO.File.Delete(inputFilePath);
                }
                catch { }
            }
        }

        private static void Rotate(List<byte[]> pages, ImageCodecInfo encoder)
        {
            var rotateFlipType = RotateFlipType.Rotate90FlipNone;
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                using (MemoryStream stream = new MemoryStream())
                using (MemoryStream ms = new MemoryStream(pages[pageIndex]))
                using (var img = System.Drawing.Image.FromStream(ms))
                {
                    img.RotateFlip(rotateFlipType);
                    img.Save(stream, encoder, null);
                    pages[pageIndex] = stream.ToArray();
                }
            }
        }

        private static ImageCodecInfo GetEncoderInfo(string mimeType)
        {
            var encoders = ImageCodecInfo.GetImageEncoders();

            for (var i = 0; i < encoders.Length; i++)
            {
                if (encoders[i].MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase))
                    return encoders[i];
            }

            return null;
        }
        private static void BuildMultipageTiff(List<byte[]> pages, ImageCodecInfo encoder, string fileName)
        {
            using (MemoryStream ms = new MemoryStream(pages[0]))
            using (System.Drawing.Image img = System.Drawing.Image.FromStream(ms))
            {
                using (EncoderParameters encoderParams = new EncoderParameters(2))
                using (EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionCCITT4))
                {
                    encoderParams.Param[0] = encoderParam;

                    using (EncoderParameter encoderParam2 = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.MultiFrame))
                    {
                        encoderParams.Param[1] = encoderParam2;

                        img.Save(fileName, encoder, encoderParams);
                    }

                    using (EncoderParameter encoderParam2 = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.FrameDimensionPage))
                    {
                        encoderParams.Param[1] = encoderParam2;

                        for (int i = 1; i < pages.Count; i++)
                        {
                            using (MemoryStream stream = new MemoryStream(pages[i]))
                            using (System.Drawing.Image img2 = System.Drawing.Image.FromStream(stream))
                            {
                                img.SaveAdd(img2, encoderParams);
                            }
                        }
                    }
                }

                using (EncoderParameters encoderParams = new EncoderParameters(1))
                using (EncoderParameter encoderParam = new EncoderParameter(System.Drawing.Imaging.Encoder.SaveFlag, (long)EncoderValue.Flush))
                {
                    encoderParams.Param[0] = encoderParam;
                    img.SaveAdd(encoderParams);
                }
            }
        }


    }
}
