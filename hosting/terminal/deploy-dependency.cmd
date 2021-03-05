SET Edition=Debug
SET DestinationDirectory=.\bin\%Edition%\net5.0\plugins

@ECHO 复制第三方类库...

SET Packages=%USERPROFILE%\.nuget\packages

copy /Y %Packages%\pinyinconvertercore\1.0.2\lib\netstandard2.0\*.dll                          %DestinationDirectory%\automao\common\

copy /Y %Packages%\mysql.data\8.0.22\lib\net5.0\*.dll                                          %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.configuration.configurationmanager\5.0.0\lib\netstandard2.0\*.dll    %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.security.permissions\5.0.0\lib\net5.0\*.dll                          %DestinationDirectory%\zongsoft\data\drivers\mysql\

copy /Y %Packages%\stackexchange.redis\2.2.4\lib\net5.0\*.dll                                  %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\microsoft.bcl.asyncinterfaces\1.1.1\lib\netstandard2.1\*.dll                %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\pipelines.sockets.unofficial\2.2.0\lib\net5.0\*.dll                         %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\system.io.pipelines\5.0.0\lib\netstandard2.0\*.dll                          %DestinationDirectory%\zongsoft\externals\redis\

copy /Y %Packages%\cronos\0.7.0\lib\netstandard2.0\*.dll                                       %DestinationDirectory%\zongsoft\scheduling\cron\
