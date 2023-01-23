#!/bin/sh
set -e

cd /backend
mkdir -p /backend/out

dotnet publish -p:OutputType=exe -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=true
cp -rfv /backend/bin/Release/net6.0/linux-x64/publish/* /backend/out/
chmod +x /backend/out/deckystream

mkdir -p /backend/out/lib/gstreamer

cp -rv /usr/lib/libgst* /backend/out/lib/
cp -rv /usr/lib/gstreamer-1.0/libgst* /backend/out/lib/gstreamer/

/backend/downloadNdi.sh

/backend/buildGstreamerNdi.sh