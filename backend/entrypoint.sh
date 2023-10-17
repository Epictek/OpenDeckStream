#!/bin/sh
set -e

export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools
export DOTNET_CLI_HOME="/tmp/DOTNET_CLI_HOME"
export DOTNET_CLI_TELEMETRY_OPTOUT=1

mkdir -p /backend/out
cd /backend/src/obs_recorder

dotnet publish -r linux-x64 -c Release -o /backend/out/ 

mkdir -p /backend/out/obs
mv /obs-portable/* /backend/out/obs/
mv /backend/out/obs/bin/64bit/obs-ffmpeg-mux /backend/out/
