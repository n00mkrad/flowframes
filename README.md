# Flowframes - Windows GUI for Video Interpolation
Flowframes Windows GUI for video interpolation - RIFE, DAIN-NCNN, CAIN-NCNN.



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
  * You can install a portable version of all those requirements from the Flowframes Installer. However, this does not support RTX 3000 cards yet.



#### Running A Pytorch AI on Nvidia Ampere (RTX 3000) GPUs

I do not have an Ampere card yet, so I can't fully test Flowframes on an RTX 3000 series GPU.

However, users have reported that you can run it by installing a recent **nightly build of Pytorch**. NCNN-based AIs however should work out of the box.