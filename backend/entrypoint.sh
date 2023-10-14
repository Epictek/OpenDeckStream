#!/bin/sh
set -e

export DOTNET_ROOT=/usr/share/dotnet
export PATH=$PATH:$DOTNET_ROOT:$DOTNET_ROOT/tools


cd /backend/src/obs_recorder

dotnet publish -r linux-x64 -c Release -o /backend/out/

mkdir -p /backend/out/obs
cp -rfv /obs-portable/* /backend/out/obs/
