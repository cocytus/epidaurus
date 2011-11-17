Hi!

COPYRIGHT: This project is Copyright (C) Bjørn Moe, bem@farlig.org
LICENSE: GPL v2 http://www.gnu.org/licenses/gpl-2.0.html 
No warranty, software is distributed AS-IS.

Overview:

This is beta quality software, there is no easy setup of anything. There is massive room for improvements in the code. Some work is done on a clean-up refactoring, more to come.

System consists of 2 main parts:
 - The Rescanner, when configured, should be run via Windows Task Scheduler or other other means regularly. I run it every 15 minutes. It updates the movie listing.
 - The web is everything else.


INSTALLATION:

Database:
Only tested on SQL SERVER 2008 R2. Create an empty database,  Run the EpidaurusDb.edmx.aql script. Change the USE [epidaurus]; line to fit whatever you name your database.
Create an initial admin user. Run this in SQL Server manager:

	INSERT INTO Users (Username,Password, LastLogin,Name,IsAdmin)
	VALUES ('YourUserName','F44A9AF592051C6845514F138BE88E65F117689',GETDATE(),'Your Name',1)

Change YourUserName and Your Name to whatever you want. The password hash is for password "blahBLAH". Note that as of now we do not seed the password hash.

WEB:

Create an application within IIS, and point it to wherever you have Epidaurus installed. https is probably a good idea, as is a separate .NET 4.0 application pool.
Open up Web.config and set the DB Connection string, and your google API key.

When database and web have been deployed you should be able to log in using the username you used in the INSERT statement above with the 'blahBLAH' password.

RESCANNER:
This is the thing that actually adds your movies. Open up App.config and fix the google API key and connection string.

Now, open up SQL Server Manager, open up the Epidaurus DB and right click the StorageLocations table, choose Edit Top 200 Rows.
This table is where you enter the locations of your movies. Add one or more rows, where the columns are:
	Id: Don't enter anything here, it's a serial.
	Name: Human-readable name of the storage location. Ie. "ServerOne Movies share" or "Local movies".
	Type: Should be "Folder", the only type currently supported.
	Data1: Path to the folder
	Data2: Leave Empty, but not NULL
	Rebase: Rebase is not currently used on the web, but the intention is to rewrite the folder path (from Data1 column) to some other path accessible to users of the web.
	Active: True/1 .. If you set this to False/0 the location will not be scanned.

After the row(s) have been successfully stored, you may run rescaner. Watch the logfile (log.txt by default) for info on errors an whatnot. Logging can be configured in Nlog.config in both Rescanner and Web folder.

After the rescanner run you should see your new movies in the web.


OTHER RANDOM INFORMATION:

GUI:
On the movie listing:
Click the (Endre) text next to the IMDB ID to change the IMDB of a local movie if the scanner got it wrong.
Click the @ symbol in "Lokal lokasjon <Title> @ <Storage location name>" to ignore that storage location for that movie. Used when the rescanner got it wrong.

There is no localization. Web text Norwegian only for now.

