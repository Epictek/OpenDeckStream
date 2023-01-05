#!/bin/sh
set -e

cd /backend

dotnet publish -p:OutputType=exe -c Release -r linux-x64 -p:PublishSingleFile=true --self-contained true -p:PublishTrimmed=true --output out
