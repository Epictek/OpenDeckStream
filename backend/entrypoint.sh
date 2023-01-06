#!/bin/sh
set -e

cd /backend
mkdir -p /backend/out

dotnet publish -p:OutputType=exe -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=true --output out
chmod +x /backend/out/DeckyStream
mkdir -p /backend/out/lib/gstreamer

cp -r /usr/lib/libgst* /backend/out/lib/
cp -r /usr/lib/gstreamer-1.0/libgst* /backend/out/lib/gstreamer/ 
