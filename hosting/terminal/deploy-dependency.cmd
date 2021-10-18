SET Edition=Debug
SET DestinationDirectory=.\bin\%Edition%\net5.0\plugins

@ECHO 复制第三方类库...

SET Packages=%USERPROFILE%\.nuget\packages

copy /Y %Packages%\mqttnet\3.0.16\lib\net5.0\*.dll                                             %DestinationDirectory%\zongsoft\messaging\mqtt\
copy /Y %Packages%\mqttnet.extensions.managedclient\3.0.16\lib\net5.0\*.dll                    %DestinationDirectory%\zongsoft\messaging\mqtt\

copy /Y %Packages%\stackexchange.redis\2.2.50\lib\net5.0\*.dll                                 %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\pipelines.sockets.unofficial\2.2.0\lib\net5.0\*.dll                         %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\system.diagnostics.performancecounter\5.0.0\lib\netstandard2.0\*.dll        %DestinationDirectory%\zongsoft\externals\redis\

copy /Y %Packages%\mysql.data\8.0.26\lib\net5.0\*.dll                                          %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\bouncycastle.netcore\1.8.5\lib\netstandard2.0\*.dll                         %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\google.protobuf\3.14.0\lib\netstandard2.0\*.dll                             %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\k4os.compression.lz4\1.1.11\lib\netstandard2.0\*.dll                        %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\k4os.compression.lz4.streams\1.1.11\lib\netstandard2.0\*.dll                %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\k4os.hash.xxhash\1.0.6\lib\netstandard2.0\*.dll                             %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.buffers\4.5.1\lib\netstandard2.0\*.dll                               %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.configuration.configurationmanager\5.0.0\lib\netstandard2.0\*.dll    %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.security.permissions\5.0.0\lib\net5.0\*.dll                          %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.text.encoding.codepages\5.0.0\lib\netstandard2.0\*.dll               %DestinationDirectory%\zongsoft\data\drivers\mysql\

copy /Y %Packages%\microsoft.win32.registry\5.0.0\lib\netstandard2.0\*.dll                     %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.data.sqlclient\4.8.3\lib\netstandard2.0\*.dll                        %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.buffers\4.5.1\lib\netstandard2.0\*.dll                               %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.diagnostics.diagnosticsource\5.0.1\lib\net5.0\*.dll                  %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.memory\4.5.4\lib\netstandard2.0\*.dll                                %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.security.accesscontrol\5.0.0\lib\netstandard2.0\*.dll                %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.security.principal.windows\5.0.0\lib\netstandard2.0\*.dll            %DestinationDirectory%\zongsoft\data\drivers\mssql\
copy /Y %Packages%\system.text.encoding.codepages\5.0.0\lib\netstandard2.0\*.dll               %DestinationDirectory%\zongsoft\data\drivers\mssql\

copy /Y %Packages%\cronos\0.7.1\lib\netstandard2.0\*.dll                                       %DestinationDirectory%\zongsoft\scheduling\cron\
