SET DestinationDirectory=.\plugins

@ECHO 复制第三方类库...

SET Packages=%USERPROFILE%\.nuget\packages

copy /Y %Packages%\mysql.data\8.0.22\lib\netstandard2.1\*.dll                                  %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.configuration.configurationmanager\4.7.0\lib\netstandard2.0\*.dll    %DestinationDirectory%\zongsoft\data\drivers\mysql\
copy /Y %Packages%\system.security.permissions\4.7.0\lib\netstandard2.0\*.dll                  %DestinationDirectory%\zongsoft\data\drivers\mysql\

copy /Y %Packages%\stackexchange.redis\2.2.4\lib\netcoreapp3.1\*.dll                           %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\microsoft.bcl.asyncinterfaces\1.1.1\lib\netstandard2.1\*.dll                %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\pipelines.sockets.unofficial\2.2.0\lib\netcoreapp3.1\*.dll                  %DestinationDirectory%\zongsoft\externals\redis\
copy /Y %Packages%\system.io.pipelines\4.7.1\lib\netstandard2.0\*.dll                          %DestinationDirectory%\zongsoft\externals\redis\
