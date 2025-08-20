import sys
import os
import json
import time
import functools
import glob
import vapoursynth as vs
core = vs.core

# Vars from command line (via VSPipe)
input_path = globals().get("input", "")
temp_dir_path = globals().get("tmpDir", "")
cache_file = globals().get("cache", "")
fps_in = globals().get("inFps", "")
fps_out = globals().get("outFps", "")
fps_out_resampled = globals().get("outFpsRes", "")
res_scaled = globals().get("resSc", "")
pad = globals().get("pad", "0x0")
frames = globals().get("frames", "") == 'True'
dedupe = globals().get("dedupe", "") == 'True'
allow_redupe = globals().get("redupe", "") == 'True'
match_duration = globals().get("matchDur", "") == 'True'
sc_sens = float(globals().get("sc", ""))
loop = globals().get("loop", "") == 'True'
factor = globals().get("factor", "")
realtime = globals().get("rt", "") == 'True'
perf_osd = realtime and globals().get("osd", "") == 'True'
show_frame_nums = globals().get("debugFrNums", "") == 'True'
show_vars = globals().get("debugVars", "") == 'True'
trim = globals().get("trim", "")
override_color_matrix = globals().get("cMatrix", "")
alpha = globals().get("alpha", "") == 'True'

if alpha:
    show_frame_nums = False
    show_vars = False
    perf_osd = False

# Construct & parse additional variables
frames_dir = os.path.join(temp_dir_path, 'frames')
inframes_json_path = os.path.join(temp_dir_path, 'input.json')
frames_vs_json_path = os.path.join(temp_dir_path, 'frames.vs.json')
vfr_resample_json_path = os.path.join(temp_dir_path, 'frameIndexes.json')
infps_num, infps_den = map(int, fps_in.split('/'))
outfps_num, outfps_den = map(int, fps_out.split('/'))
outfps_res_num, outfps_res_den = map(int, fps_out_resampled.split('/'))
res_scaled_x, res_scaled_y = map(int, res_scaled.split('x')) # Scaled resolution
pad_x, pad_y = map(int, pad.split('x')) # Padding right/bottom
txt_scale = max(1, min(res_scaled_x // 1000, 4)) # Text scale = scaled width divided by 1000, rounded to int, and clamped to 1-4

frames_produced_total = 0

# Load frames or video
if frames:
    frames = sorted(glob.glob(os.path.join(frames_dir, "*.*")))
    ext = os.path.splitext(frames[0])[1]
    first = os.path.splitext(os.path.basename(frames[0]))[0]
    pattern = os.path.join(frames_dir, f"%0{len(first)}d{ext}")  # Construct the file pattern with the proper padding
    clip = core.imwri.Read(rf"{pattern}", firstnum=int(first))   # Load the image sequence with imwri
    clip = core.std.AssumeFPS(clip, fpsnum=infps_num, fpsden=infps_den)  # Set the frame rate for the image sequence
else:
    clip = core.lsmas.LWLibavSource(input_path, cachefile=cache_file) # Load video with lsmash
    if alpha:
        clip = core.std.PropToClip(clip, prop='_Alpha') # Process only alpha channel

width_src = clip.width  # Input resolution width
height_src = clip.height  # Input resolution height
framecount_src = len(clip)  # Amount of source frames
reordered_clip = clip[0]

# Deduplication
if dedupe:
    with open(inframes_json_path) as json_file:
        frame_list = json.load(json_file)
        for i in frame_list:
            reordered_clip = reordered_clip + clip[i]
    clip = reordered_clip.std.Trim(1, reordered_clip.num_frames - 1) # Dedupe trim

# Trim
if trim:
    src_trim_start, src_trim_end = map(int, trim.split('/')) # Trim start and end frames
    clip = clip.std.Trim(src_trim_start, src_trim_end)

# Loop: Copy first frame to end of clip
if loop and not frames:
    first_frame = clip[0]
    clip = clip + first_frame

# Store properties of the first frame for later use
first_frame_props = clip.get_frame(0).props
c_matrix = '709'

try:
    m = first_frame_props._Matrix
    if override_color_matrix: c_matrix = override_color_matrix
    elif m == 0:  c_matrix = 'rgb'
    elif m == 4:  c_matrix = 'fcc'
    elif m == 5:  c_matrix = '470bg'
    elif m == 6:  c_matrix = '170m'
    elif m == 7:  c_matrix = '240m'
    elif m == 8:  c_matrix = 'ycgco'
    elif m == 9:  c_matrix = '2020ncl'
    elif m == 10: c_matrix = '2020cl'
    elif m == 12: c_matrix = 'chromancl'
    elif m == 13: c_matrix = 'chromacl'
    elif m == 14: c_matrix = 'ictcp'
except:
    c_matrix = '709'

# Store color range (same as first frame)
col_range = 'full' if first_frame_props.get('_ColorRange') == 0 else 'limited'

resize = res_scaled and res_scaled != "0x0" and res_scaled != f"{width_src}x{height_src}"
res_w = res_scaled_x if resize else width_src
res_h = res_scaled_y if resize else height_src

# Scene change detection
if sc_sens > 0.01:
    clip = core.misc.SCDetect(clip=clip, threshold=sc_sens)

# Convert to RGBS from YUV or RGB
colors = "YUV" if clip.format.color_family == vs.YUV else "RGB"
if colors == "YUV":
    clip = core.resize.Bicubic(clip=clip, format=vs.RGBS, matrix_in_s=c_matrix, range_s=col_range, width=res_w, height=res_h)
else:
    clip = core.resize.Bicubic(clip=clip, format=vs.RGBS, width=res_w, height=res_h)

info_str = f"FPS Inp: {fps_in}\nFPS Out: {fps_out}\nFPS Rsp: {fps_out_resampled}\nRes Inp: {width_src}x{height_src}\nRes Scl: {res_scaled}\nPad: {pad}\nColors: {colors} {col_range}\nDe/Redupe: {dedupe}/{dedupe and allow_redupe}\n"
info_str += f"Loop: {loop}\nScn Detect: {sc_sens}\nMatch Dur: {match_duration}\nTrim: {trim}"

# Padding to achieve a compatible resolution (some models need a certain modulo)
if pad_x > 0 or pad_y > 0:
    clip = core.std.AddBorders(clip, right=pad_x, bottom=pad_y)

pre_interp_frames = len(clip)

# RIFE Variables
r_mdlpath = globals().get("mdl", "")
r_gpu = int(globals().get("gpu", "0"))
r_threads = int(globals().get("gpuThrds", "1"))
r_uhd = globals().get("uhd", "") == 'True'
r_tta = globals().get("tta", "") == 'True'
info_str += f"\nGPU: {r_gpu} ({r_threads} thrds)\nUHD: {r_uhd}\nMatrix: {c_matrix}"

# OSD (input clip)
def on_frame_in(n, clip):
    if show_frame_nums:
        clip = core.text.Text(clip, text=f"IN:  {n:06d}", alignment=7, scale=txt_scale)
    return clip

clip = core.std.FrameEval(clip, functools.partial(on_frame_in, clip=clip))

# RIFE Interpolation
r_fac_num, r_fac_den = map(int, factor.split('/'))
clip = core.rife.RIFE(clip, factor_num=r_fac_num, factor_den=r_fac_den, model_path=r_mdlpath, gpu_id=(None if r_gpu < 0 else r_gpu), gpu_thread=r_threads, tta=r_tta, uhd=r_uhd, sc=sc_sens > 0.01)

frm_count_after_interp = len(clip)

# Reduplication
if dedupe and allow_redupe and not realtime:
    reordered_clip = clip[0]
    with open(frames_vs_json_path, 'r') as json_file:
        frame_list = json.load(json_file)
        for i in frame_list:
            reordered_clip = reordered_clip + clip[i]
    clip = reordered_clip.std.Trim(1, reordered_clip.num_frames - 1) # Redupe trim

frm_count_after_redupe = len(clip)
info_str += f"\nBefore/After RD: {frm_count_after_interp}/{frm_count_after_redupe}"

# Set output format & color matrix
clip = vs.core.resize.Bicubic(clip, format=vs.YUV444P16, matrix_s=c_matrix) if not alpha else vs.core.resize.Bicubic(clip, format=vs.GRAY8, matrix_s=c_matrix)

# Undo compatibility padding by cropping the same area
if pad_x > 0 or pad_y > 0:
    clip = core.std.Crop(clip, right=pad_x, bottom=pad_y)

# Factor rounded to int, minus 1
end_dupe_count = r_fac_num // r_fac_den - 1
target_count_match = int(globals().get("targetMatch", ""))
target_count_true = target_count_match - end_dupe_count

if not dedupe:
    if loop:
        clip = clip.std.Trim(length=target_count_match) # Trim, loop enabled
    elif match_duration:
        clip = clip.std.Trim(length=target_count_true) # Trim, loop disabled, duration matching disabled

# OSD Variables
frames_produced_prev = 0
frames_produced_curr = 0
last_fps_upd_time = time.time() 
start_time = last_fps_upd_time 

# OSD etc.
def on_frame_out(n, clip): 
    global start_time, frames_produced_total
    frames_produced_total += 1
    if show_frame_nums:
        clip = core.text.Text(clip, text=f"\nOUT: {n:06d}", alignment=7, scale=txt_scale)
    if show_vars:
        clip = core.text.Text(clip, text=f"\n\n\n{info_str}", alignment=7, scale=txt_scale)
    if not perf_osd:
        return clip
    fps_avg_time = 2
    now = time.time()
    if now - start_time > fps_avg_time:
        global frames_produced_prev, frames_produced_curr, last_fps_upd_time
        fps_float = (clip.fps.numerator / clip.fps.denominator) 
        vid_time_float = (1 / fps_float) * n 
        frames_produced_curr += 1 
        if now - last_fps_upd_time > fps_avg_time: 
            last_fps_upd_time = now 
            frames_produced_prev = frames_produced_curr / fps_avg_time 
            frames_produced_curr = 0 
        speed = (frames_produced_prev / fps_float) * 100 
        osd_str = f"{time.strftime('%H:%M:%S', time.gmtime(vid_time_float))} - {frames_produced_prev:.2f}/{fps_float:.2f} FPS ({speed:.0f}%){' [!]' if speed < 95 else ''}" 
        clip = core.text.Text(clip, text=osd_str, alignment=1, scale=txt_scale) 
    return clip 

clip = core.std.FrameEval(clip, functools.partial(on_frame_out, clip=clip))

# Frame number debug overlay
if show_frame_nums:
    factor_str = f"{r_fac_num / r_fac_den:.2f}"
    # clip = core.text.FrameNum(clip, alignment=9, scale=txt_scale)  # Output frame counter
    clip = core.text.Text(clip, f"Frames: {framecount_src}/{pre_interp_frames} -> {len(clip)} [{factor_str}x]", alignment=9, scale=txt_scale)
    clip = core.text.Text(clip, f"Target (match): {target_count_match} - Target (true): {target_count_true} - End Dupes: {end_dupe_count}", alignment=3, scale=txt_scale)

# Frames picked to resample VFR video to fps_out_resampled
if os.path.isfile(vfr_resample_json_path):
    with open(vfr_resample_json_path) as json_file:
        frame_indexes = json.load(json_file)
        clip = core.std.Splice([clip[i] for i in frame_indexes if i < len(clip)])

# if fps_out_resampled != fps_out and outfps_res_num > 0 and outfps_res_den > 0:
#     clip = core.std.AssumeFPS(clip, fpsnum=outfps_res_num, fpsden=outfps_res_den)

# Loop video indefinitely for realtime mode
if realtime and loop:
    clip = clip.std.Loop(0)

clip.set_output()
