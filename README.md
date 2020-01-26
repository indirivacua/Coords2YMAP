# Coords2YMAP

### About 
It's a little program that allow you to convert the coords from .IDE and .OPL files to YTYP/YMAP (XML)

### Usage
Put in some folder the .OPLs, .IDEs and .WDRs files (yeah, models are needed too for reverse with bruteforce [Jenkins hash](https://en.wikipedia.org/wiki/Jenkins_hash_function#one_at_a_time) names in *.OPLs for some cases) and use the convert buttons

*.IDE -> .YTYP.xml*

*.OPL -> .YMAP.xml*

### Requirements
	.NET Framework 4.5.2
	OpenIV (for export the .OPL files)
	Meta Toolkit (for convert the .XML to YTYP/YMAP)

### Thanks to
	_CP_ 		- Idea and tests
	NTAuthority 	- Base from his .IDE and .OPL to JSON
	dexyfex		- Code for read .YPT files

### Changelog
- 09/21/2016 - Initial Release

- 26/01/2020 - Updated and re-writed mostly all the code just 4 fun and practice. Fixed probably all bugs it had, but who cares? Nobody going to use this tool.
