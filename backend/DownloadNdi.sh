mkdir ndi-build
cd ndi-build

echo "Downloading"
wget https://downloads.ndi.tv/SDK/NDI_SDK_Linux/Install_NDI_SDK_v5_Linux.tar.gz

#code from https://aur.archlinux.org/cgit/aur.git/tree/PKGBUILD?h=ndi-sdk
_target_line="$(sed -n '/^__NDI_ARCHIVE_BEGIN__$/=' "Install_NDI_SDK_v${_majver}_Linux.sh")"
_target_line="$((_target_line + 1))"

echo "Extracting"

tail -n +"$_target_line" "Install_NDI_SDK_v5_Linux.sh" | tar -zxvf -

echo "Copying libs"

cp -v lib/x86_64-linux-gnu/libndi.so.5.5.2 ../out/

echo "Cleaning up"

cd ..
rm -rfv ndi-build