import sys
import os
import json
import vapoursynth as vs
core = vs.core

clip = core.lsmas.LWLibavSource(r'D:\Temp\AV1AN\blackwidow.mkv')
print(f"Frames: {clip.num_frames}")