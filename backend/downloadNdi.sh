#!/bin/sh

echo "Downloading"
mkdir ndi-build
wget https://downloads.ndi.tv/SDK/NDI_SDK_Linux/Install_NDI_SDK_v5_Linux.tar.gz

#code from https://aur.archlinux.org/cgit/aur.git/tree/PKGBUILD?h=ndi-sdk

bsdtar -x -f Install_NDI_SDK_v5_Linux.tar.gz -C ndi-build
cd ndi-build
_target_line="$(sed -n '/^__NDI_ARCHIVE_BEGIN__$/=' "Install_NDI_SDK_v5_Linux.sh")"
_target_line="$((_target_line + 1))"

echo "Extracting"

tail -n +"$_target_line" "Install_NDI_SDK_v5_Linux.sh" | tar -zxvf -

echo "Copying libs"

cp -v "NDI SDK for Linux/lib/x86_64-linux-gnu/libndi.so.5.5.2" /backend/out/lib/libndi.so.5
# cp -v "NDI SDK for Linux/lib/x86_64-linux-gnu/libndi.so*" /usr/lib/
# ldconfig
cd ..

# git clone https://github.com/teltek/gst-plugin-ndi
# cd gst-plugin-ndi
# cargo build
# cp target/release/libgstndi.so ../out/lib/gstreamer/

echo "Cleaning up"
rm Install_NDI_SDK_v5_Linux.tar.gz
rm -rfv ndi-build
