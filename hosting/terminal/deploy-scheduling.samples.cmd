@echo off

setlocal EnableDelayedExpansion

SET SourcePathBase=D:\Zongsoft\Framework\
SET DestinationPath=.\bin\Debug\netcoreapp3.1\plugins\

SET BinaryDirectory=bin\Debug\netcoreapp3.1

SET Project_1_Name=Zongsoft.Scheduling.Samples
SET Project_1_Path=Zongsoft.Scheduling
SET Project_1_Directory=samples
SET Project_1_Destination=zongsoft\scheduling\samples\

for /L %%i in (1,1,1) do (
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
		xcopy !OptionFile! %DestinationPath%!Project_%%i_Destination! /d /y /q /c
	) else (
		@echo The '!OptionFile!' file is not exists.
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
