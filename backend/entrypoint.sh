#!/bin/sh
set -e

cd /backend
mkdir -p /backend/out

dotnet publish -p:OutputType=exe -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=true --output out
chmod +x /backend/out/deckystream

mkdir /backend/out/lib/
mkdir /backend/out/lib/gstreamer

cp -rv /usr/lib/libgst* /backend/out/lib/
cp -rv /usr/lib/gstreamer-1.0/libgst* /backend/out/lib/gstreamer/

./downloadNdi.sh