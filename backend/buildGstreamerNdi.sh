#!/bin/sh

git clone https://gitlab.freedesktop.org/gstreamer/gst-plugins-rs
cd gst-plugins-rs
cargo install cargo-c
cargo cbuild -p gst-plugin-ndi
cp -v target/x86_64-unknown-linux-gnu/debug/libgstndi.so /backend/out/lib/gstreamer/

cd ../
rm -rfv gst-plugins-rs