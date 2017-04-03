SQL Server Management Studio Add-in for Denali
----------------------------------------------

Aim
---
The aim of this project is to help you get a SSMS Addin running quickly.


Background
----------
I've tried to document each step of creating the Addin. 

Creating the Add-in
	http://tsqltidy.blogspot.com/2011/08/how-to-write-sql-server-management_17.html


Before you can get up and running you need to do a couple of things;

1) If using installator, installator should do it otherwise:
Copy Ssms2012Extneder.addin to C:\ProgramData\Microsoft\MSEnvShared\AddIns
You may need to create the folders.

2) Edit Ssms2012Extender.addin - Change the <Assembly></Assembly> to be there your binary is. 

Build your project, Debug/Run it.

Mark

http://tsqltidy.blogspot.com
markpm@hotmail.co.uk


To enable logging add registry keys:

"HKEY_CURRENT_USER\\Software\\Ssms2012Extender", "LoggingEnabled" (0 - disabled, 1 - enabled) (if LoggingPath not provided, logger will be disabled anyway) (DWORD)

"HKEY_CURRENT_USER\\Software\\Ssms2012Extender", "LoggingPath"  - make sure you have premission to write files on the selected path  (STRING)

the log file will be Ssms2012ExtenderLog.log





