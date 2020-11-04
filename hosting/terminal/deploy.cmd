@echo off

setlocal EnableDelayedExpansion

SET SourcePathBase=D:\Zongsoft\Framework\
SET DestinationPath=.\bin\Debug\netcoreapp3.1\plugins\

SET BinaryDirectory=bin\Debug\netcoreapp3.1

SET Project_1_Name=Zongsoft.Data
SET Project_1_Destination=zongsoft\data\

SET Project_2_Name=Zongsoft.Security
SET Project_2_Destination=zongsoft\security\

SET Project_3_Name=Zongsoft.Commands
SET Project_3_Destination=zongsoft\commands\

SET Project_4_Name=Zongsoft.Scheduling
SET Project_4_Destination=zongsoft\scheduling\

SET Project_5_Name=Zongsoft.Scheduling.Cron
SET Project_5_Path=Zongsoft.Scheduling
SET Project_5_Directory=cron
SET Project_5_Destination=zongsoft\scheduling\cron\

SET Project_6_Name=Zongsoft.Externals.Aliyun
SET Project_6_Path=externals\aliyun
SET Project_6_Destination=zongsoft\%Project_6_Path%\

SET Project_7_Name=Zongsoft.Externals.Redis
SET Project_7_Path=externals\redis
SET Project_7_Destination=zongsoft\%Project_7_Path%\

SET Project_8_Name=Zongsoft.Data.MsSql
SET Project_8_Path=Zongsoft.Data
SET Project_8_Directory=drivers\mssql
SET Project_8_Destination=zongsoft\data\%Project_8_Directory%\

SET Project_9_Name=Zongsoft.Data.MySql
SET Project_9_Path=Zongsoft.Data
SET Project_9_Directory=drivers\mysql
SET Project_9_Destination=zongsoft\data\%Project_9_Directory%\

REM 拷贝主插件(main.plugin)文件
xcopy %SourcePathBase%Zongsoft.Plugins\Main.plugin %DestinationPath% /d /y
REM 拷贝终端插件(terminal.plugin)文件
xcopy .\plugins\Terminal.plugin %DestinationPath% /d /y

for /L %%i in (1,1,9) do (
	if not defined Project_%%i_Directory SET Project_%%i_Directory=src

	if not defined Project_%%i_Path (
		SET FileName=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\!Project_%%i_Name!
		SET LibraryPath=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\%BinaryDirectory%\!Project_%%i_Name!
	) else (
		SET FileName=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\!Project_%%i_Name!
		SET LibraryPath=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\%BinaryDirectory%\!Project_%%i_Name!
	)

	SET OptionFile=!FileName!.option
	SET PluginFile=!FileName!.plugin
	SET MappingFile=!FileName!.mapping
	SET OptionProductionFile=!FileName!.production.option
	SET OptionDevelopmentFile=!FileName!.development.option

	if exist !OptionFile! (
		xcopy !OptionFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!OptionFile!' file is not exists.
	)

	if exist !OptionProductionFile! (
		xcopy !OptionProductionFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!OptionProductionFile!' file is not exists.
	)

	if exist !OptionDevelopmentFile! (
		xcopy !OptionDevelopmentFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!OptionDevelopmentFile!' file is not exists.
	)

	if exist !PluginFile! (
		xcopy !PluginFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!PluginFile!' file is not exists.
	)

	if exist !MappingFile! (
		xcopy !MappingFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!MappingFile!' file is not exists.
	)

	if exist !LibraryPath!.dll (
		xcopy !LibraryPath!.dll %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!LibraryPath!.dll' file is not exists.
	)
	if exist !LibraryPath!.pdb (
		xcopy !LibraryPath!.pdb %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!LibraryPath!.pdb' file is not exists.
	)

	@echo;
)

:EXIT
