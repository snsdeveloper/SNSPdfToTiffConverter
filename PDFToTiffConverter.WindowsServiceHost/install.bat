@echo off
@echo Installing Service...
%systemroot%\Microsoft.NET\Framework\v4.0.30319\installutil "yourpathTo\PDFToTiffConverter.WindowsServiceHost.exe"
@echo Finished Installing

net start PDFToTiffConverter
pause