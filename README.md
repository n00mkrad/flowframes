# Flowframes - Windows GUI for Video Interpolation
Flowframes Windows GUI for video interpolation - Supports RIFE, RIFE-NCNN, DAIN-NCNN networks.

Flowframes is **open-source donationware**. Builds are released for free on itch after an early-access period on Patreon. This repo's code is complete and does not "paywall" experienced users who want to compile the program themselves.

However, **I do not provide support for self-built versions** as I can't guarantee that the code of this repo is stable at any given moment.



## Installation

* Download on [itch](https://nmkd.itch.io/flowframes) or, for the most recent beta versions, on [Patreon](https://www.patreon.com/n00mkrad). This repo does not provide builds.
* Run Flowframes.exe
* Pre-1.18: Select the components you want to install (certain packages are required, cannot be unticked)

Starting with 1.18, the installer has been removed, and Flowframes is instead distributed as an all-in-one archive. Download the "Full" file if you are using a Maxwell/Pascal/Turing GPU and want to use embedded Pytorch. Use "NoPython" if you run an AMD GPU or want to use your system Python/Pytorch installation.



## Using A Pytorch Implementation

Some of the AI networks run on Tencent's NCNN framework, which allows them to run on any modern (Vulkan-capable) GPU.

However, others (like RIFE) run best via their original Pytorch implementation.

The requirements to run these are the following:

* A **modern Nvidia GPU** (750 Ti, 900/1000/1600/2000/3000 Series).
* A **Python** installation including Pytorch (1.5 or later) as well as the packages `opencv-python` and `imageio`.
  * You can install a portable version of all those requirements from the Flowframes Installer. This does not support RTX 3000 cards yet.

[More Details On Python Dependencies](PythonDependencies.md)



## Running A Pytorch Implementation on Nvidia Ampere GPUs

Ampere support is currently (Dec 2020) limited. The embedded Python runtime is not compatible with RTX 3000 cards. To enable compatiblity, [install Pytorch 1.7.1](https://pytorch.org/get-started/locally/) or newer on Python 3.8.x.

Important: Ampere GPUs perform worse than they should on cuDNN 8.04 and older. If your cuDNN version is not >=8.05, you can manually update it by downloading it from Nvidia and replacing the DLLs in the torch folder. If you don't want to do that, you can wait until 8.05 is included in Pytorch.



## Configuration

All Settings have reasonable defaults, so users do not need to do any configuration before using the program.

Here is an explanation of some of the more important settings.

### General

* Maximum Video Size: Frames are exported at this resolution if the video is larger. Lower resolutions speed up interpolation a lot.

### Interpolation

* Copy Audio: Audio will be saved to a separate file when extracting the frames and will afterwards be merged into the output.
  * Not guaranteed to work with all audio codecs. Supported are: M4A/AAC, Vorbis, Opus, MP2, PCM/Raw.
* Remove Duplicate Frames: This is meant for 2D animation. Removing duplicates makes a smooth interpolation possible.
  * You can disable this completely if you only use content without duplicates (e.g. camera footage, CG renders).
* Animation Loop: This will make looped animations interpolate to a perfect loop by copying the first frame to the end of the frames.
* Don't Interpolate Scene Changes: This avoids interpolating scene changes (cuts) as this would produce weird a morphing effect.
* Auto-Encode: Encode video while interpolating. Optionally delete the already encoded frames to minimize disk space usage.
* Save Output Frames As JPEG: Save interpolated frames as JPEG before encoding. Not recommended unless you have little disk space.

### AI Specific Settings

* RIFE - UHD Mode - This mode changes some scaling parameters and should improve results on high-resolution video.
* GPU IDs: `0` is the default for setups with one dedicated GPU. Four dedicated GPUs would mean `0,1,2,3` for example.
* NCNN Processing Threads: Increasing this number to 2, 3 or 4 can improve GPU utilization, but also slow things down.

### Video Export

* Encoding Options: Set options for video/GIF encoding. Refer to the **FFmpeg** and **Gifski** documentations.
* Minimum Video Length: Make sure the output is as long as this value by looping it.
* Maximum Output Frame Rate: Limit frame rate, for example, if you want a 60 FPS output from a 24 FPS video.

### Debugging / Experimental

* Show Hidden CMD Windows: This will show the windows for AI processes. Can be useful for debugging.
* FFprobe: Count Frames Manually: This uses a slower way of getting the input video's total frame count, but works reliably. 