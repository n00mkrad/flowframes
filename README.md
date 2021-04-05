# Flowframes - Windows GUI for Video Interpolation

## [itch.io (Free Builds)](https://nmkd.itch.io/flowframes) | [Patreon (Latest Builds)](https://www.patreon.com/n00mkrad) | [Discord](https://discord.com/invite/eJHD2NSJRe)

Flowframes Windows GUI for video interpolation - Supports RIFE (Pytorch & NCNN), DAIN (NCNN), and FLAVR (Pytorch) implementations.

Flowframes is **open-source donationware**. Builds are released for free on itch after an early-access period on Patreon. This repo's code is complete and does not "paywall" experienced users who want to compile the program themselves or want to contribute to the development.

However, **I do not provide support for self-built versions** as I can't guarantee that the code of this repo is stable at any given moment.

![img](https://i.imgur.com/HHZxUYo.png)

## Quick Installation

* Download on [itch](https://nmkd.itch.io/flowframes) or, for the most recent beta versions, on [Patreon](https://www.patreon.com/n00mkrad). This repo does not provide builds.
* Extract the 7z archive (7-zip or similar software required!) to any folder
* Run Flowframes.exe



## Using A Pytorch Implementation

Flowframes comes with RIFE-NCNN which runs on Tencent's NCNN framework, which allows it to run on any modern (Vulkan-capable) GPU.

However, the official RIFE implementation run best via its original Pytorch implementation.

The requirements to run these are the following:

* A **modern Nvidia GPU** (750 Ti, 900/1000/1600/2000/3000 Series).
* A **Python** installation including Pytorch (1.5 or later) as well as the packages `opencv-python` and `imageio`.
  * The python runtime with pytorch is included in the Flowframes "Full" and "Full-RTX3000" downloads.

[More Details On Python Dependencies](PythonDependencies.md)



## Running A Pytorch Implementation on Nvidia Ampere GPUs

The python runtime included in the "Full-RTX3000" package includes support for Ampere GPUs. If you want to use your own installation, [use Pytorch 1.7.1](https://pytorch.org/get-started/locally/) or newer on Python 3.8.x.

Important: Ampere GPUs perform worse than they should on cuDNN 8.04 and older. If your cuDNN version is not >=8.05, you can manually update it by downloading it from Nvidia and replacing the DLLs in the torch folder. The embedded runtime already includes those files.



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

* Encoding Options: Set options for video/GIF encoding. Refer to the **FFmpeg** documentation for details.
* Minimum Video Length: Make sure the output is as long as this value by looping it.
* Maximum Output Frame Rate: Limit frame rate by downsampling, for example, if you want a 60 FPS output from a 24 FPS video.

### Debugging / Experimental

* Show Hidden CMD Windows: This will show the windows for AI processes. Can be useful for debugging.
* FFprobe: Count Frames Manually: This uses a slower way of getting the input video's total frame count, but works more reliably. 



## System Requirements

#### Minimum: 

* Vulkan-capable GPU (Nvidia Kepler or newer, AMD GCN 2 or newer)

#### Recommended: 

* Modern CUDA-capable GPU (Nvidia Maxwell or newer) with 6 GB VRAM or more
* 16 GB RAM
* Modern CPU (Intel Core 7xxx Series or newer, AMD Ryzen Series)



## Frequently Asked Questions (FAQ)

**Q:** What's the difference between RIFE CUDA and RIFE NCNN? Which one should I use?  
**A:** The results should be identical, however, RIFE-NCNN also runs on AMD cards, CUDA only on Nvidia. If you have an Nvidia card, use CUDA as it's faster.

**Q:** What is frame de-duplication for? When should I enable or disable it?  
**A:** It's primarily for 2D animation, where the video has consecutive frames without changes. These have to be removed before interpolation to avoid choppy outputs. Enable it for 2D animation, disable it for constant frame rate content like camera footage or 3D rendered videos.

**Q:** My output looks very choppy, especially in dark (or low-contrast) scenes!  
**A:** Disable De-Duplication (or reduce the threshold if you still need it)

**Q:** What's the technical difference between the de-duplication modes "Remove During Extraction" and "Remove After Extraction"?  
**A:** "During" uses ffmpeg's `mpdecimate` filter and won't extract duplicate frames at all. "After" extracts all frames and *then* checks for duplicates by checking the image difference using Magick.NET, which is slower but more accurate and flexible.

**Q:** How does Auto-Encode work, and should I enable or disable it?  
**A:** It encodes your output video during interpolation, instead of afterwards. Enable it unless you have a very weak CPU.

**Q:** I downloaded a "Full" package but now want to switch to my own system Python installation. How do I do that?  
**A:** Go to `FlowframesData/pkgs/` and delete the folders `py-tu` or `py-amp`, whichever you have. Flowframes will now try to use system python.
