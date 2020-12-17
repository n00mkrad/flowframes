import sys
import os
import cv2
import torch
import argparse
import numpy as np
#from tqdm import tqdm
from torch.nn import functional as F
import warnings
import _thread
import skvideo.io
from queue import Queue, Empty
#import moviepy.editor
import shutil
warnings.filterwarnings("ignore")

abspath = os.path.abspath(__file__)
dname = os.path.dirname(abspath)
print("Changing working dir to {0}".format(dname))
os.chdir(os.path.dirname(dname))
print("Added {0} to temporary PATH".format(dname))
sys.path.append(dname)

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
if torch.cuda.is_available():
    torch.set_grad_enabled(False)
    torch.backends.cudnn.enabled = True
    torch.backends.cudnn.benchmark = True
else:
    print("WARNING: CUDA is not available, RIFE is running on CPU! [ff:nocuda-cpu]")
    
try:
    print("\nSystem Info:")
    print("Python: {} - Pytorch: {} - cuDNN: {}".format(sys.version, torch.__version__, torch.backends.cudnn.version()))
    print("Hardware Acceleration: Using {} device(s), first is {}".format( torch.cuda.device_count(), torch.cuda.get_device_name(0)))
except:
    print("Failed to get hardware info!")

parser = argparse.ArgumentParser(description='Interpolation for a pair of images')
parser.add_argument('--video', dest='video', type=str, default=None)
parser.add_argument('--img', dest='img', type=str, default=None)
parser.add_argument('--output', required=False, default='frames-interpolated')
parser.add_argument('--imgformat', default="png")
parser.add_argument('--montage', default=False, dest='montage', action='store_true', help='montage origin video')
parser.add_argument('--UHD', dest='UHD', action='store_true', help='support 4k video')
parser.add_argument('--skip', dest='skip', default=False, action='store_true', help='whether to remove static frames before processing')
parser.add_argument('--fps', dest='fps', type=int, default=None)
parser.add_argument('--png', dest='png', default=True, action='store_true', help='whether to vid_out png format vid_outs')
parser.add_argument('--ext', dest='ext', type=str, default='mp4', help='vid_out video extension')
parser.add_argument('--exp', dest='exp', type=int, default=1)
args = parser.parse_args()
assert (not args.video is None or not args.img is None)
if not args.img is None:
    args.png = True

from model.RIFE_HD import Model
model = Model()
model.load_model(os.path.join(dname, "models"), -1)
model.eval()
model.device()

path = args.img
name = os.path.basename(path)
interp_output_path = (args.output).join(path.rsplit(name, 1))
print("\ninterp_output_path: " + interp_output_path)

if not args.video is None:
    videoCapture = cv2.VideoCapture(args.video)
    fps = videoCapture.get(cv2.CAP_PROP_FPS)
    tot_frame = videoCapture.get(cv2.CAP_PROP_FRAME_COUNT)
    videoCapture.release()
    if args.fps is None:
        fpsNotAssigned = True
        args.fps = fps * (2 ** args.exp)
    else:
        fpsNotAssigned = False
    videogen = skvideo.io.vreader(args.video)
    lastframe = next(videogen)
    fourcc = cv2.VideoWriter_fourcc('m', 'p', '4', 'v')
    video_path_wo_ext, ext = os.path.splitext(args.video)
    print('{} frames in total'.format(tot_frame))
else:
    videogen = []
    for f in os.listdir(args.img):
        if 'png' in f:
            videogen.append(f)
    tot_frame = len(videogen)
    videogen.sort(key= lambda x:int(x[:-4]))
    lastframe = cv2.imread(os.path.join(args.img, videogen[0]))[:, :, ::-1].copy()
    videogen = videogen[1:]    
h, w, _ = lastframe.shape
vid_out = None
if args.png:
    if not os.path.exists(interp_output_path):
        os.mkdir(interp_output_path)
else:
    vid_out = cv2.VideoWriter('{}_{}X_{}fps.{}'.format(video_path_wo_ext, args.exp, int(np.round(args.fps)), args.ext), fourcc, args.fps, (w, h))
    
def clear_write_buffer(user_args, write_buffer):
    cnt = 1
    while True:
        item = write_buffer.get()
        if item is None:
            break
        if user_args.png:
            print('=> {:0>8d}.{}'.format(cnt, args.imgformat))
            cv2.imwrite('{}/{:0>8d}.{}'.format(interp_output_path, cnt, args.imgformat), item[:, :, ::-1])
            #cv2.imwrite('vid_out/{:0>7d}.png'.format(cnt), item[:, :, ::-1])
            cnt += 1
        else:
            vid_out.write(item[:, :, ::-1])

def build_read_buffer(user_args, read_buffer, videogen):
    for frame in videogen:
        if not user_args.img is None:
            frame = cv2.imread(os.path.join(user_args.img, frame))[:, :, ::-1].copy()
        if user_args.montage:
            frame = frame[:, left: left + w]
        read_buffer.put(frame)
    read_buffer.put(None)

def make_inference(I0, I1, exp):
    global model
    middle = model.inference(I0, I1, args.UHD)
    if exp == 1:
        return [middle]
    first_half = make_inference(I0, middle, exp=exp - 1)
    second_half = make_inference(middle, I1, exp=exp - 1)
    return [*first_half, middle, *second_half]

if args.montage:
    left = w // 4
    w = w // 2
if args.UHD:
    print("UHD mode enabled.")
    ph = ((h - 1) // 64 + 1) * 64
    pw = ((w - 1) // 64 + 1) * 64
else:
    ph = ((h - 1) // 32 + 1) * 32
    pw = ((w - 1) // 32 + 1) * 32
padding = (0, pw - w, 0, ph - h)
#pbar = tqdm(total=tot_frame)
skip_frame = 1
if args.montage:
    lastframe = lastframe[:, left: left + w]

write_buffer = Queue(maxsize=200)
read_buffer = Queue(maxsize=200)
_thread.start_new_thread(build_read_buffer, (args, read_buffer, videogen))
_thread.start_new_thread(clear_write_buffer, (args, write_buffer))

I1 = torch.from_numpy(np.transpose(lastframe, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
I1 = F.pad(I1, padding)

while True:
    frame = read_buffer.get()
    if frame is None:
        break
    I0 = I1
    I1 = torch.from_numpy(np.transpose(frame, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
    I1 = F.pad(I1, padding)
    #p = (F.interpolate(I0, (16, 16), mode='bilinear', align_corners=False)
    #     - F.interpolate(I1, (16, 16), mode='bilinear', align_corners=False)).abs().mean()
    #if p < 5e-3 and args.skip:
    #    if skip_frame % 100 == 0:
    #        print("Warning: Your video has {} static frames, skipping them may change the duration of the generated video.".format(skip_frame))
    #    skip_frame += 1
    #    #pbar.update(1)
    #    continue
    #if p > 0.2:             
    #    mid1 = lastframe
    #    mid0 = lastframe
    #    mid2 = lastframe
    #else:
    output = make_inference(I0, I1, args.exp)
    if args.montage:
        write_buffer.put(np.concatenate((lastframe, lastframe), 1))
        for mid in output:
            mid = (((mid[0] * 255.).byte().cpu().numpy().transpose(1, 2, 0)))
            write_buffer.put(np.concatenate((lastframe, mid[:h, :w]), 1))
    else:
        write_buffer.put(lastframe)
        for mid in output:
            mid = (((mid[0] * 255.).byte().cpu().numpy().transpose(1, 2, 0)))
            write_buffer.put(mid[:h, :w])
    #pbar.update(1)
    lastframe = frame
if args.montage:
    write_buffer.put(np.concatenate((lastframe, lastframe), 1))
else:
    write_buffer.put(lastframe)
import time
while(not write_buffer.empty()):
    time.sleep(0.1)
#pbar.close()
if not vid_out is None:
    vid_out.release()

# move audio to new video file if appropriate
if args.png == False and fpsNotAssigned == True and not args.skip:
    outputVideoFileName = '{}_{}X_{}fps.{}'.format(video_path_wo_ext, args.exp, int(np.round(args.fps)), args.ext)
    transferAudio(video_path_wo_ext + "." + args.ext, outputVideoFileName)
