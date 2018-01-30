#PDF To Tiff Converter
This project is licensed under the terms of the AGPL (GNU Affero General Public License)

This is a C# .NET 4.6.2 application

You can run the application as a console host or windows service. It will monitor folders for new pdf files and convert them to TIFF using ghostscript

This application depends on ghostscript to do the actual conversion to TIFF, you can download and install ghostscript from https://www.ghostscript.com/download/gsdnld.html

AppSettings to configure in the app.config file:

* foldersToWatch - semicolon separate list of directory paths to watch for new pdf files
* tiffOutputDirectory - directory you want the TIFF files placed after the pdf is  converted to tiff
* ghostScriptLocation - the directory where ghostscript is located on your machine

To run this application in visual studio, open the PDFToTiffConverter solution and set the PDFToTiffConverter.ConsoleHost as the startup application

If the PDF file is landscape, after it converts to TIFF it will be rotated 90 degress clockwise