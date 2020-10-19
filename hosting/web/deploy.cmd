@echo off

setlocal EnableDelayedExpansion

SET SourcePathBase=D:\Zongsoft\Framework\
SET BinaryDirectory=bin\Debug\netcoreapp3.1

SET Project_1_Name=Zongsoft.Data
SET Project_1_Destination=zongsoft\data\

SET Project_2_Name=Zongsoft.Security
SET Project_2_Destination=zongsoft\security\

SET Project_3_Name=Zongsoft.Commands
SET Project_3_Destination=zongsoft\commands\

SET Project_4_Name=Zongsoft.Externals.Aliyun
SET Project_4_Path=externals\aliyun
SET Project_4_Destination=zongsoft\%Project_4_Path%\

SET Project_5_Name=Zongsoft.Externals.Redis
SET Project_5_Path=externals\redis
SET Project_5_Destination=zongsoft\%Project_5_Path%\

SET Project_6_Name=Zongsoft.Data.MsSql
SET Project_6_Path=Zongsoft.Data
SET Project_6_Directory=drivers\mssql
SET Project_6_Destination=zongsoft\data\%Project_6_Directory%\

SET Project_7_Name=Zongsoft.Data.MySql
SET Project_7_Path=Zongsoft.Data
SET Project_7_Directory=drivers\mysql
SET Project_7_Destination=zongsoft\data\%Project_7_Directory%\

SET Project_8_Name=Zongsoft.Security.Web
SET Project_8_Path=Zongsoft.Security
SET Project_8_Directory=src\api
SET Project_8_Destination=zongsoft\security\

REM 拷贝主插件(main.plugin)文件
xcopy %SourcePathBase%Zongsoft.Plugins\Main.plugin .\plugins\ /d /y

for /L %%i in (1,1,8) do (
	if not defined Project_%%i_Directory SET Project_%%i_Directory=src

	if not defined Project_%%i_Path (
		SET OptionFile=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\!Project_%%i_Name!.option
		SET PluginFile=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\!Project_%%i_Name!.plugin
		SET MappingFile=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\!Project_%%i_Name!.mapping
		SET LibraryPath=%SourcePathBase%!Project_%%i_Name!\!Project_%%i_Directory!\%BinaryDirectory%\!Project_%%i_Name!
	) else (
		SET OptionFile=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\!Project_%%i_Name!.option
		SET PluginFile=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\!Project_%%i_Name!.plugin
		SET MappingFile=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\!Project_%%i_Name!.mapping
		SET LibraryPath=%SourcePathBase%!Project_%%i_Path!\!Project_%%i_Directory!\%BinaryDirectory%\!Project_%%i_Name!
	)

	if exist !OptionFile! (
		xcopy !OptionFile! .\plugins\!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!OptionFile!' file is not exists.
	)

	if exist !PluginFile! (
		xcopy !PluginFile! .\plugins\!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!PluginFile!' file is not exists.
	)

	if exist !MappingFile! (
		xcopy !MappingFile! .\plugins\!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!MappingFile!' file is not exists.
	)

	if exist !LibraryPath!.dll (
		xcopy !LibraryPath!.dll .\plugins\!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!LibraryPath!.dll' file is not exists.
	)
	if exist !LibraryPath!.pdb (
		xcopy !LibraryPath!.pdb .\plugins\!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!LibraryPath!.pdb' file is not exists.
	)

	@echo;
)

:EXIT
