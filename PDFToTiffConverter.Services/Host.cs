using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PDFToTiffConverter.Services
{
    public static class Host
    {
        #region fields
        static TextFileLogger logger;
        static Dictionary<string, string> NewFiles = new Dictionary<string, string>();
        static System.Timers.Timer newFileReadyTimer;
        #endregion

        #region Stop Service
        public static void Stop()
        {
            newFileReadyTimer.Stop();
        }
        #endregion

        #region Start Service
        public static void Start()
        {
            logger = new TextFileLogger("PDFToTiffConverterError.log");
            ValidateSettings(logger);
            //every second, check to see if new PDF files are ready to convert (not in use by another process)
            newFileReadyTimer = new System.Timers.Timer();
            newFileReadyTimer.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            newFileReadyTimer.Elapsed += NewFileReadyTimer_Elapsed;
            newFileReadyTimer.Start();
            
            var foldersToWatch = System.Configuration.ConfigurationManager.AppSettings["foldersToWatch"].Split(";".ToCharArray(),StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach(var folder in foldersToWatch)
            {
                QueueUpExistingFiles(folder);
                FileSystemWatcher watcher = new FileSystemWatcher();
                watcher.Path = folder;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.Filter = "*.pdf";
                watcher.Changed += NewPdfFile_Event;
                watcher.EnableRaisingEvents = true;
            }
        }

        private static void ValidateSettings(TextFileLogger logger)
        {
            var gsPath = System.Configuration.ConfigurationManager.AppSettings["ghostScriptLocation"];

            if (string.IsNullOrEmpty(gsPath) || !System.IO.File.Exists(gsPath))
            {
                logger.LogIt("Invalid ghost script location");
                throw new ApplicationException("Ghost Script Path is not set properly. This is the path to the ghost script executable to convert pdf to tiff");
            }
            var outputDir = System.Configuration.ConfigurationManager.AppSettings["tiffOutputDirectory"];
            if (string.IsNullOrEmpty(outputDir) || !System.IO.Directory.Exists(outputDir))
            {
                logger.LogIt("Invalid tiffOutputDirectory");
                throw new ApplicationException("tiffOutputDirectory path is not set properly. This is the path to put the TIFF files that get converted from PDF");
            }

        }

        private static void QueueUpExistingFiles(string folder)
        {
            //we might have had new pdf files come in while the application was not running so load up any existing
            //files in the folder so they can be processed
            lock (NewFiles)
            {
                foreach(var f in System.IO.Directory.GetFiles(folder))
                {
                    NewFiles.Add(f, f);
                }
            }
        }
        #endregion

        #region Process New Files
        private static void NewFileReadyTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (NewFiles)
            {
                var processed = new List<string>();
                foreach(var file in NewFiles.Keys)
                {
                    if (FileUtils.IsFileReady(file))
                    {
                        ProcessFile(file);
                        processed.Add(file);
                    }
                }
                foreach(var p in processed)
                {
                    NewFiles.Remove(p);
                }
            }
        }
       

        private static void ProcessFile(string file)
        {
            Console.WriteLine("Processing " + file);
            try
            {
                var needsRotation = FileUtils.NeedsRotation(file);
                var outputDir = System.Configuration.ConfigurationManager.AppSettings["tiffOutputDirectory"];
                var fileName = System.IO.Path.GetFileNameWithoutExtension(file);
                fileName += ".tiff";
                var outputFile = System.IO.Path.Combine(outputDir, fileName);
                Converter.Convert(file, outputFile, needsRotation);
            }
            catch(Exception ex)
            {
                logger.LogIt($"Error Processing File {file}. {ex.Message} {ex.StackTrace}");
            }

        }
         



        /// <summary>
        /// the event can fire multiple times
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void NewPdfFile_Event(object sender, FileSystemEventArgs e)
        {
            //add it to a list, we have a timer running that will check this new files dictionary
            // to see if they are ready to be processed, this newpdfFile_event fires multiple times so
            // we are trying to deal with knowing when the file has fully finished writing to the folder
            lock (NewFiles)
            {
                if (!NewFiles.ContainsKey(e.FullPath)) NewFiles.Add(e.FullPath, e.FullPath);
            }
            
        }
      
        #endregion
    }
}
