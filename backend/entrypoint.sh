#!/bin/sh
set -e

export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
export DOTNET_CLI_HOME="/tmp/DOTNET_CLI_HOME"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

mkdir -p /backend/out
cd /backend/src/obs_recorder

# dotnet publish -r linux-x64 -c Release -o /backend/out/
dotnet publish -r linux-x64 -c Debug -o /backend/out/

#rm -rf /backend/out/obs_recorder.dbg

# cd /backend/src/xObsBeam/src/
# dotnet publish -c Release -o xobsbeam -r linux-x64 /p:NativeLib=Shared /p:SelfContained=true

OBS_LOCATION=/home/deck/homebrew/plugins/OpenDeckStream/bin/obs/


mkdir -p /backend/out/obs
mv $OBS_LOCATION/bin /backend/out/obs/
mv $OBS_LOCATION/data /backend/out/obs/
mv $OBS_LOCATION/obs-plugins /backend/out/obs/

# mv xobsbeam/* /backend/out/obs/obs-plugins/64bit/


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

# cp /usr/lib/libturbojpeg.so* /backend/out/libs/
# cp /usr/lib/libjpeg.so* /backend/out/libs/

