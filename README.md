# AXSlime

A bridge to make AXIS trackers work with SlimeVR.

## Usage

1. Ensure you have [AXIS Control Center](https://downloads.axisxr.gg/cc/beta/default) installed and set up with your trackers.
2. Ensure you have [SlimeVR](https://slimevr.dev/download) installed.
3. Download the [latest release of AXSlime](https://github.com/ButterscotchV/AXSlime/releases/latest) and extract it.
4. Run AXIS Control Center and SlimeVR first, then run `AXSlime.exe`. You should see the trackers from the AXIS Control Center show up in SlimeVR.
5. After running for the first time, the config file `AXSlime_Config.json` will be generated next to `AXSlime.exe`.

### OSC Haptics Support

To enable OSC support, you will need to edit the config `AXSlime_Config.json`: Change the line `"osc_enabled": false` to `"osc_enabled": true`.
When OSC is enabled, you will see a message in the console when starting the program with the IP endpoint it is listening on.

Support is added for:

- [AXHaptics](https://github.com/TahvoDev/AXHaptics)
  - [Avatar Creation](https://github.com/TahvoDev/AXHaptics/wiki/Avatar-Creation)
- [bHaptics](https://github.com/bhaptics/VRChatOSC)
  - [How to update an avatar with bHaptics devices](https://bhaptics.notion.site/How-to-upload-an-avatar-with-bHaptics-devices-777b7dc686044291877b7ed21c27b7cd)

## Credit

- AXIS Unity SDK
  - <https://github.com/Refract-Technologies/AXIS-SDK-Unity>
- SlimeVR
  - <https://github.com/SlimeVR/>
  - <https://github.com/SlimeVR/SlimeVR-Server>
  - <https://github.com/SlimeVR/SlimeVR-Tracker-ESP>
- CoreOSC
  - <https://github.com/LucHeart/CoreOSC-UTF8-ASYNC>
- AxHaptics
  - <https://github.com/TahvoDev/AxHaptics>
