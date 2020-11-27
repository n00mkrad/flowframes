# Flowframes - Windows GUI for Video Interpolation
Flowframes Windows GUI for video interpolation - Supports RIFE, RIFE-NCNN, DAIN-NCNN, CAIN-NCNN networks.



## Installation

* Download the latest version on [itch](https://nmkd.itch.io/flowframes) or, for the most recent beta versions, on [Patreon](https://www.patreon.com/n00mkrad). This repo does not provide downloads.
* Run Flowframes.exe
* Select the components you want to install (certain packages are required, cannot be unticked)



## Using A Pytorch AI

Some of the AI networks run on Tencent's NCNN framework, which allows them to run on any modern (Vulkan-capable) GPU.

However, others (like RIFE) run best via their original Pytorch implementation.

The requirements to run these are the following:

* A **modern Nvidia GPU** (750 Ti, 900/1000/1600/2000/3000 Series).
* A **Python** installation including Pytorch (1.5 or later) as well as the packages `opencv-python` and `imageio`.
  * You can install a portable version of all those requirements from the Flowframes Installer. This does not support RTX 3000 cards yet.



#### Running A Pytorch AI on Nvidia Ampere (RTX 3000) GPUs

I do not have an Ampere card yet, so I can't fully test Flowframes on an RTX 3000 series GPU.

However, users have reported that you can run it by installing a recent **nightly build of Pytorch**. NCNN-based AIs should work out of the box.



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
* Save Output Frames As JPEG: Save interpolated frames as JPEG before encoding. Not recommended unless you have little disk space.

### AI Specific Settings

* RIFE - Use Fast Parallel Mode - Speeds up RIFE interpolation a lot if you have lots of VRAM. Not recommended with <8GB GPUs.
* GPU IDs: `0` is the default for setups with one dedicated GPU. Four dedicated GPUs would mean `0,1,2,3` for example.
* NCNN Processing Threads: Increasing this number to 2, 3 or 4 can improve GPU utilization, but also slow things down.

### Video Export

* Encoding Options: Set options for video/GIF encoding. Refer to the **FFmpeg** and Gifski documentations.
* Minimum Video Length: Make sure the output is as long as this value by looping it.
* Maximum Output Frame Rate: Limit frame rate, for example, if you want a 60 FPS output from a 24 FPS video.

### Debugging / Experimental

* Show Hidden CMD Windows: This will show the windows for AI processes. Can be useful for debugging.
* FFprobe: Count Frames Manually: This uses a slower way of getting the input video's total frame count, but works reliably. 