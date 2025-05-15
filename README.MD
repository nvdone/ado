# Alter Database Objects
A command-line utility allowing to modify multiple Microsoft SQL Server database views, functions, stored procedures, triggers with the help of regular expressions.\
\
This application might be of help if you need to move or copy a set of databases, linked via cross-database and/or cross-server queries within numerous views and stored procedures:
```
... INNER JOIN anotherserver.anotherdatabase.dbo.table...
... FROM OPENQUERY(anotherserver, 'anotherquery')
```
\
**Usage**: ado.exe server database_list.txt rule_list.txt [-user:username] [-pass:password] [-commit]\
\
**server** - name of the SQL Server\
\
**database_list** - a text file with a list of names of databases to process, one database name per line\
\
**rule_list** - a text file with rules to apply to database objects:\
\
*ObjectNameRegExp*[TAB]*ObjectDefinitionRegExp*[TAB]*ObjectDefinitionReplacement*[CR][LF]\
*ObjectNameRegExp* - if a database object name matches this regular expression, respective rule will be applied to this object\
*ObjectDefinitionRegExp* - database object T-SQL definition will be matched by this regular expression; all matches will be replaced with *ObjectDefinitionReplacement*\
\
**-user:**, **-pass:** - optional parameters, credentials to be used for server connection; if parameters are omitted, Windows Authentication will be used for connection\
\
**-commit** - optional parameter indicating that changes are to be committed to database. By default, application only outputs planned changes to standard output, this flag instructs it to apply changes to database.
