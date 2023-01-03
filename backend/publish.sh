dotnet publish -p:OutputType=exe -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=true --output ../bin

# dotnet publish -p:OutputType=exe -c Debug -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=false --output ../bin