FROM holo-toolchain-dotnet:latest

RUN pacman -S --noconfirm asio cmake git libfdk-aac libxcomposite x264 vlc swig luajit nlohmann-json python wayland pipewire xdg-desktop-portal ffmpeg git jansson libxinerama libxkbcommon-x11  curl speexdsp pciutils \
    && rm -rf /var/cache/pacman/pkg/*

RUN git clone https://github.com/Joshua-Ashton/obs-studio \
    && cd obs-studio \
    && git checkout gamescope-capture \
    && git submodule update --init --recursive \
    && mkdir build \
    && cd build \
    && cmake -DCMAKE_BUILD_TYPE=Debug -DENABLE_VLC=OFF -DENABLE_PIPEWIRE=ON -DENABLE_BROWSER=OFF -DENABLE_NEW_MPEGTS_OUTPUT=OFF -DENABLE_DECKLINK=OFF -DENABLE_UI=OFF -DENABLE_AJA=0 -DENABLE_WEBRTC=0 -DLINUX_PORTABLE=ON -DENABLE_VST=OFF -DENABLE_QSV11=OFF -DENABLE_HEVC=ON -DCMAKE_INSTALL_PREFIX="/obs-portable" ../ \
    && make \
    && make install


ENTRYPOINT [ "/backend/entrypoint.sh" ]
