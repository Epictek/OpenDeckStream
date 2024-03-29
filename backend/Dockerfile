FROM ghcr.io/steamdeckhomebrew/holo-base:latest

RUN pacman -Sy --noconfirm wget zlib clang && \
    wget https://dot.net/v1/dotnet-install.sh && \
    chmod +x dotnet-install.sh && \
    ./dotnet-install.sh --install-dir /usr/share/dotnet -channel 8.0 && \
    rm dotnet-install.sh

RUN pacman -S --noconfirm asio cmake libfdk-aac libxcomposite x264 swig luajit nlohmann-json python wayland pipewire xdg-desktop-portal ffmpeg git jansson libxinerama libxkbcommon-x11 curl speexdsp pciutils \
    && rm -rf /var/cache/pacman/pkg/*


RUN git clone https://github.com/paullouisageneau/libdatachannel.git \
    && cd libdatachannel \
    && git submodule update --init --recursive \
    && cmake -B build -DUSE_GNUTLS=0 -DUSE_NICE=0 -DCMAKE_BUILD_TYPE=Release -DCMAKE_INSTALL_PREFIX=/usr \
    && cd build \
    && make \
    && make install

RUN git clone https://github.com/epictek/obs-studio \
    && cd obs-studio \
    && git checkout gamescope-capture \
    && git submodule update --init --recursive \
    && mkdir build \
    && cd build \
    && cmake -DCMAKE_BUILD_TYPE=Release -DENABLE_VLC=OFF -DENABLE_PIPEWIRE=ON -DENABLE_BROWSER=OFF -DENABLE_NEW_MPEGTS_OUTPUT=OFF -DENABLE_DECKLINK=OFF -DENABLE_UI=OFF -DENABLE_AJA=0 -DENABLE_WEBRTC=ON -DLINUX_PORTABLE=ON -DENABLE_VST=OFF -DENABLE_QSV11=OFF -DENABLE_HEVC=ON -DCMAKE_INSTALL_PREFIX="/home/deck/homebrew/plugins/OpenDeckStream/bin/obs" ../ \
    && make \
    && make install

# building libjpeg-turbo for beam from source because the version in the repos is too old
# RUN wget https://sourceforge.net/projects/libjpeg-turbo/files/3.0.1/libjpeg-turbo-3.0.1.tar.gz \
#     && tar -xvf libjpeg-turbo-3.0.1.tar.gz \
#     && cd libjpeg-turbo-3.0.1 \
#     && cmake -B build -D CMAKE_INSTALL_PREFIX=/usr -D CMAKE_INSTALL_LIBDIR=/usr/lib \
#     && cmake --build build \
#     && cmake --install build

ENTRYPOINT [ "/backend/entrypoint.sh" ]
