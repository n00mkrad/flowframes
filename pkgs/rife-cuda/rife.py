import sys
import os
import cv2
import torch
import argparse
import numpy as np
from torch.nn import functional as F
import warnings
import _thread
import skvideo.io
from queue import Queue, Empty
import shutil
warnings.filterwarnings("ignore")

abspath = os.path.abspath(__file__)
dname = os.path.dirname(abspath)
print("Changing working dir to {0}".format(dname))
os.chdir(os.path.dirname(dname))
print("Added {0} to temporary PATH".format(dname))
sys.path.append(dname)

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
torch.set_grad_enabled(False)
if torch.cuda.is_available():
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
parser.add_argument('--input', dest='input', type=str, default=None)
parser.add_argument('--output', required=False, default='frames-interpolated')
parser.add_argument('--model', required=False, default='models')
parser.add_argument('--imgformat', default="png")
parser.add_argument('--wthreads', dest='wthreads', type=int, default=4)
parser.add_argument('--UHD', dest='UHD', action='store_true', help='support 4k video')
parser.add_argument('--exp', dest='exp', type=int, default=1)
args = parser.parse_args()
assert (not args.input is None)

try:
    from model.RIFE_HD import Model
    model = Model()
    model.load_model(os.path.join(dname, args.model), -1)
except:
    from model.RIFE_HDv2 import Model
    model = Model()
    model.load_model(os.path.join(dname, args.model), -1)
model.eval()
model.device()

path = args.input
name = os.path.basename(path)
interp_output_path = (args.output).join(path.rsplit(name, 1))
print("\ninterp_output_path: " + interp_output_path)

cnt = 1

videogen = []
for f in os.listdir(args.input):
    if 'png' in f:
        videogen.append(f)
tot_frame = len(videogen)
videogen.sort(key= lambda x:int(x[:-4]))
lastframe = cv2.imread(os.path.join(args.input, videogen[0]))[:, :, ::-1].copy()
videogen = videogen[1:]    
h, w, _ = lastframe.shape
vid_out = None
if not os.path.exists(interp_output_path):
    os.mkdir(interp_output_path)
    

def clear_write_buffer(user_args, write_buffer, thread_id):
    while True:
        item = write_buffer.get()
        if item is None:
            break
        frameNum = item[0]
        img = item[1]
        print('[T{}] => {:0>8d}.{}'.format(thread_id, frameNum, args.imgformat))
        cv2.imwrite('{}/{:0>8d}.{}'.format(interp_output_path, frameNum, args.imgformat), img[:, :, ::-1], [cv2.IMWRITE_PNG_COMPRESSION, 2])

def build_read_buffer(user_args, read_buffer, videogen):
    for frame in videogen:
        if not user_args.input is None:
            #print("Loading input frame " + str(frame))
            frame = cv2.imread(os.path.join(user_args.input, frame))[:, :, ::-1].copy()
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

if args.UHD:
    print("UHD mode enabled.")
    ph = ((h - 1) // 64 + 1) * 64
    pw = ((w - 1) // 64 + 1) * 64
else:
    ph = ((h - 1) // 32 + 1) * 32
    pw = ((w - 1) // 32 + 1) * 32
padding = (0, pw - w, 0, ph - h)

write_buffer = Queue(maxsize=160)
read_buffer = Queue(maxsize=160)
_thread.start_new_thread(build_read_buffer, (args, read_buffer, videogen))

for x in range(args.wthreads):
    _thread.start_new_thread(clear_write_buffer, (args, write_buffer, x))

I1 = torch.from_numpy(np.transpose(lastframe, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
I1 = F.pad(I1, padding)

while True:
    frame = read_buffer.get()
    if frame is None:
        break
    I0 = I1
    I1 = torch.from_numpy(np.transpose(frame, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
    I1 = F.pad(I1, padding)

    output = make_inference(I0, I1, args.exp)
    write_buffer.put([cnt, lastframe])
    cnt += 1
    for mid in output:
        mid = (((mid[0] * 255.).byte().cpu().numpy().transpose(1, 2, 0)))
        # print(f"Adding #{cnt} to buffer.")
        write_buffer.put([cnt, mid[:h, :w]])
        cnt += 1

    lastframe = frame
write_buffer.put([cnt, lastframe])
import time
while(not write_buffer.empty()):
    time.sleep(0.2)
time.sleep(0.5)
    
    
    