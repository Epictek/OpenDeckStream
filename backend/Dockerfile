FROM ghcr.io/steamdeckhomebrew/holo-base:latest
#RUN sed -r -i 's/\[(jupiter|core|extra|community|multilib|holo)\]/\[\1-rel\]/g' /etc/pacman.conf
#RUN pacman -Sy --noconfirm archlinux-keyring
RUN pacman -Sy --noconfirm gcc glibc gcc-libs wget
RUN pacman -Sy --noconfirm rustup git && rustup install stable
RUN pacman -Sy --noconfirm dotnet-sdk gstreamer gst-plugins-base gst-plugins-bad-libs gst-plugins-good gst-plugin-pipewire gstreamer-vaapi

ENTRYPOINT [ "/backend/entrypoint.sh" ]