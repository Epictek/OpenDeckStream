# OpenDeckStream

OpenDeckStream is a Decky plugin specially designed for the SteamDeck. Utilizing `libobs` for recording, OpenDeckStream aims to provide you with an exceptional experience of recording your SteamDeck gameplay with a seamless, integrated, and easy-to-use interface.

In the future, we plan on implementing streaming capabilities to bring live casts of your gameplay directly from your SteamDeck to your favorite platforms.

## Features

* Easy-to-Use Interface
* Quick and High-Quality Recording with low performance hit
* Seamless Integration with SteamDeck
* Always recording replay buffer
* Planned Future Support for Streaming

## Installation
Install from the Decky Test Store.


## Dev Build Instructions 
Ensure you have git, docker and decky-cli installed

1. Clone the repository
```sh
git clone https://github.com/epictek/OpenDeckStream.git
```
1. Navigate to the project directory
```sh
cd OpenDeckStream
```

1. Build the project
```sh
decky plugin build -b
```

## Usage

Once OpenDeckStream is installed on your SteamDeck, you can access it under your Decky plugins. 

1. Open Quick Access Menu.
2. Navigate to the plugins section.
3. Choose OpenDeckStream from the list.
4. Start/Stop recording at your convenience.

To save the replay buffer use the ![menu](assets/sd_button_steam.svg) + ![steam](assets/sd_button_menu.svg) buttons

## Acknowledgments
A special thank you to the following teams for their incredible contributions:

- [@lulzsun](https://github.com/lulzsun) for creating the [LibObs C# wrapper](https://github.com/lulzsun/libobs.NET) that made this project possible, checkout his project called [RePlays](https://github.com/lulzsun/RePlays).

- The [OBS Studio Team](https://github.com/obsproject/obs-studio) for crafting libobs, the wonderful library that's the heart of this plugin.

- The [Steam Deck Homebrew](https://github.com/SteamDeckHomebrew) Team for the exceptional Decky plugin manager that aids seamless integration of plugins into the SteamDeck.

- [@nabel0](https://github.com/nabel0) and [@CarJem](https://github.com/CarJem) for providing their assistance with testing.
---

Thanks for checking out OpenDeckStream, happy gaming and recording!
