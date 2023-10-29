#!/bin/sh
set -e

export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
export DOTNET_CLI_HOME="/tmp/DOTNET_CLI_HOME"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

mkdir -p /backend/out
cd /backend/src/obs_recorder

dotnet publish -r linux-x64 -c Release -o /backend/out/

# rm -rf /backend/out/obs_recorder.dbg

mkdir -p /backend/out/obs
mv /obs-portable/bin /backend/out/obs/
mv /obs-portable/data /backend/out/obs/
mv /obs-portable/obs-plugins /backend/out/obs/

rm /backend/out/obs/bin/64bit/libobs-scripting.*
rm /backend/out/obs/bin/64bit/libobs-frontend*
rm -r /backend/out/obs/data/obs-scripting
mv /backend/out/obs/bin/64bit/obs-ffmpeg-mux /backend/out/

mkdir -p /backend/out/libs/

pacman -Ql ffmpeg | grep '/usr/lib/.*\.so\.[0-9]*$' | awk '{print $2}' | xargs -I{} cp {} /backend/out/libs/

cp /usr/lib/libvpx.so.7 /backend/out/libs/
cp /usr/lib/libvidstab.so.1.1 /backend/out/libs/
cp /usr/lib/libdatachannel.so.0.19 /backend/out/libs/
cp /usr/lib/libssl.so.1.1 /backend/out/libs/
cp /usr/lib/libcrypto.so.1.1 /backend/out/libs/