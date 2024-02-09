 _____ _     ____                  ___ ___ 
|  ___| |   / ___|  ___ __ _ _ __ |_ _|_ _|
| |_  | |   \___ \ / __/ _` | '_ \ | | | | 
|  _| | |___ ___) | (_| (_| | | | || | | | 
|_|   |_____|____/ \___\__,_|_| |_|___|___|
        ..built out of hacks, to fix yours.

FLScan2 by M1C

What is FLScan2?

This is a reimplemented open source version of FLScan by Accushot. FLScan2 is a program that will scan your Freelancer files looking for errors, saving you the time and headaches that this usually entails. You can direct the program to scan certain areas of your project or to simply scan the whole lot. An error report will be created which details the problems.

Important Information

FLScan2 requires version 3.5 of the .NET Framework to function correctly. This is a set of runtime files that allow applications built using the new .NET framework to run correctly. These files come as standard with new versions of Windows and are also automatically installed by Windows Update. If you do not have these files however you can download them here: http://www.microsoft.com/download/en/details.aspx?id=21

How do I use it?

The program is very simple to use. Run FLScan2 and select which logoutput you want to see, you can ignore specific log by putting keywords into the "exclude" textbox.
At the top of the window you should see a field named "Freelancer Directory"; FLScan2 searches the registry to find out where Freelancer is installed to, but if this path is incorrect then change it now.

How do I use it without the GUI?
See FLScanII.exe --help

When you have selected the desired options click "Scan" to begin. FLScan2 will first parse all the major Freelancer files gathering data about the mod, and then it will begin analysing the areas.

Differences between FLScan2 and the original FLScan:
    *) Unlike the original FLScan, this version supports both bini and text ini files.
	   The Freelancer SDK is not required.
    *) THN repair, Pathbuilding and auto mod scan are not supported.

Credits
    - A few NPC checks, initial hacks and encouragement by cannon
    - Everything else by M1C
    - Some readme content shamelessly borrowed by the original FLScan readme by Accushot.
