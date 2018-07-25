# vNext.Comparer
[![Build Status](https://travis-ci.org/PDmatrix/vNext.Comparer.svg?branch=master)](https://travis-ci.org/PDmatrix/vNext.Comparer)
[![Codacy Badge](https://api.codacy.com/project/badge/Grade/1479838fe39348bf85a11e05310cdd57)](https://www.codacy.com/app/PDmatrix/vNext.Comparer?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=PDmatrix/vNext.Comparer&amp;utm_campaign=Badge_Grade)

This utility will help you automate the work with the MS SQL database.

This is a console utility, so you will need to pass arguments through the console.

Commands to work with utility:
  
  ---
1) CompareLocal - Comparison of local scripts with scripts in database.
  
  Required arguments:
  ```
COMMAND="CompareLocal" - Command to work with,
CONNECTIONSTRING - The connection string,
DIR - The directory where the scripts are stored.
  ```
  Optional arguments:
  ```
WINMERGE - Flag to open comparison results in the WinMerge
  ```
  Usage:
  ```
vNext.Comparer COMMAND="CompareLocal" CONNECTIONSTRING="connectionstring" dir="directory" WINMERGE
  ```
  
  ---
2) CompareDB - Comparison of scripts in the database with another database
  
  Required arguments:
  ```
COMMAND="CompareDb" - Command to work with,
LEFTCONNECTIONSTRING - The connection string of the left database
RIGHTCONNECTIONSTRING - The connection string of the right database.
  ```
  Optional arguments:
  ```
QUERY - Query, to get a list of objects. The query must return the names of the objects in one column,
WINMERGE - Flag to open comparison results in the WinMerge.
  ```
  Usage:
  ```
vNext.Comparer COMMAND="CompareDb" LEFTCONNECTIONSTRING="connectionstring1" RIGHTCONNECTIONSTRING="connectionstring2" QUERY="SELECT DISTINCT SCHEMA_NAME(SCHEMA_ID) + '.' + name FROM sys.procedures  WHERE type = 'P'" WINMERGE
  ```
  
  ---
3) UpdateDb - Update scripts in the database from local files.
  
  Required arguments:
  ```
COMMAND="UpdateDb" - Command to work with,
CONNECTIONSTRING - The connection string,
DIR - The directory where the scripts are stored. Also, you can specify individual files that have an extension ".sql".
  ```
  Usage:
  ```
vNext.Comparer COMMAND="UpdateDb" CONNECTIONSTRING="connectionstring" DIR="directory"
  ```
  
  ---
