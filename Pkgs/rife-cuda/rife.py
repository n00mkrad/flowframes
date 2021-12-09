import sys
import io
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
import base64
warnings.filterwarnings("ignore")

abspath = os.path.abspath(__file__)
dname = os.path.dirname(abspath)
print("Changing working dir to {0}".format(dname))
os.chdir(os.path.dirname(dname))
print("Added {0} to temporary PATH".format(dname))
sys.path.append(dname)


parser = argparse.ArgumentParser(description='Interpolation for a pair of images')
parser.add_argument('--input', dest='input', type=str, default=None)
parser.add_argument('--output', required=False, default='frames-interpolated')
parser.add_argument('--model', required=False, default='models')
parser.add_argument('--imgformat', default="png")
parser.add_argument('--rbuffer', dest='rbuffer', type=int, default=200)
parser.add_argument('--wthreads', dest='wthreads', type=int, default=4)
parser.add_argument('--fp16', dest='fp16', action='store_true', help='half-precision mode')
parser.add_argument('--UHD', dest='UHD', action='store_true', help='support 4k video')
parser.add_argument('--scale', dest='scale', type=float, default=1.0, help='Try scale=0.5 for 4k video')
parser.add_argument('--exp', dest='exp', type=int, default=1)
parser.add_argument('--multi', dest='multi', type=int, default=2)
args = parser.parse_args()

if args.exp != 1:
    args.multi = (2 ** args.exp)
    
assert (not args.input is None)

if args.UHD and args.scale==1.0:
    args.scale = 0.5
    
assert args.scale in [0.25, 0.5, 1.0, 2.0, 4.0]

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
torch.set_grad_enabled(False)
if torch.cuda.is_available():
    torch.backends.cudnn.enabled = True
    torch.backends.cudnn.benchmark = True
    if(args.fp16):
        torch.set_default_tensor_type(torch.cuda.HalfTensor)
        print("RIFE is running in FP16 mode.")
else:
    print("WARNING: CUDA is not available, RIFE is running on CPU! [ff:nocuda-cpu]")
    
try:
    print("\nSystem Info:")
    print("Python: {} - Pytorch: {} - cuDNN: {}".format(sys.version, torch.__version__, torch.backends.cudnn.version()))
    print("Hardware Acceleration: Using {} device(s), first is {}".format( torch.cuda.device_count(), torch.cuda.get_device_name(0)))
except:
    print("Failed to get hardware info!")

try:
    try:
        print(f"Trying to load v3 (new) model using arch files from {os.path.join(dname, args.model)}")
        from arch.RIFE_HDv3 import Model
        model = Model()
        
        if not hasattr(model, 'version'):
            model.version = 0
        else:
            print("Using >= 3.9 model.")
        
        model.load_model(os.path.join(dname, args.model), -1)
        print("Loaded v3.x/4.x model.")
    except:
        try:
            print(f"Trying to load v3 (legacy) model from {os.path.join(dname, args.model)}")
            from model.RIFE_HDv3 import Model
            model = Model()
            model.load_model(os.path.join(dname, args.model), -1)
            print("Loaded v3.x HD model.")
        except:
            print(f"Trying to load v2 model from {os.path.join(dname, args.model)}")
            from model.RIFE_HDv2 import Model
            model = Model()
            model.load_model(os.path.join(dname, args.model), -1)
            print("Loaded v2.x HD model.")
except:
    print(f"Trying to load v1 model from {os.path.join(dname, args.model)}")
    from model.RIFE_HD import Model
    model = Model()
    model.load_model(os.path.join(dname, args.model), -1)
    print("Loaded v1.x HD model")

model.eval()
model.device()

path = args.input
name = os.path.basename(path)
interp_output_path = (args.output).join(path.rsplit(name, 1))
print("interp_output_path: " + interp_output_path)

cnt = 1

videogen = []
for f in os.listdir(args.input):
    if 'png' in f or 'jpg' in f:
        videogen.append(f)
tot_frame = len(videogen)
videogen.sort(key= lambda x:int(x[:-4]))
img_path = os.path.join(args.input, videogen[0])
lastframe = cv2.imdecode(np.fromfile(img_path, dtype=np.uint8), cv2.IMREAD_UNCHANGED)[:, :, ::-1].copy()
videogen = videogen[1:]    
h, w, _ = lastframe.shape
vid_out = None
if not os.path.exists(interp_output_path):
    os.mkdir(interp_output_path)
    

def clear_write_buffer(user_args, write_buffer, thread_id):
    os.chdir(interp_output_path)
    while True:
        item = write_buffer.get()
        if item is None:
            break
        frameNum = item[0]
        img = item[1]
        print('[T{}] => {:0>8d}.{}'.format(thread_id, frameNum, args.imgformat))
        #imgBytes = base64.b64encode(cv2.imencode(f'.{args.imgformat}', img[:, :, ::-1], [cv2.IMWRITE_PNG_COMPRESSION, 2])[1].tostring())
        #print(f"{frameNum:08}:"+ imgBytes.decode('utf-8') + "\n\n\n\n")
        cv2.imwrite('{:0>8d}.{}'.format(frameNum, args.imgformat), img[:, :, ::-1], [cv2.IMWRITE_PNG_COMPRESSION, 2])

def build_read_buffer(user_args, read_buffer, videogen):
    for frame in videogen:
        if not user_args.input is None:
            img_path = os.path.join(user_args.input, frame)
            frame = cv2.imdecode(np.fromfile(img_path, dtype=np.uint8), cv2.IMREAD_UNCHANGED)[:, :, ::-1].copy()
        read_buffer.put(frame)
    read_buffer.put(None)

def make_inference(I0, I1, n):    
    global model
    if hasattr(model, 'version') and model.version >= 3.9:
        res = []
        for i in range(n):
            res.append(model.inference(I0, I1, (i+1) * 1. / (n+1), args.scale))
        return res
    else:
        middle = model.inference(I0, I1, args.scale)
        if n == 1:
            return [middle]
        first_half = make_inference(I0, middle, n=n//2)
        second_half = make_inference(middle, I1, n=n//2)
        if n%2:
            return [*first_half, middle, *second_half]
        else:
            return [*first_half, *second_half]
    
def pad_image(img):
    if(args.fp16):
        return F.pad(img, padding).half()
    else:
        return F.pad(img, padding)

print(f"Using scale {args.scale}.")
tmp = max(128, int(128 / args.scale))
ph = ((h - 1) // tmp + 1) * tmp
pw = ((w - 1) // tmp + 1) * tmp
padding = (0, pw - w, 0, ph - h)

write_buffer = Queue(maxsize=args.rbuffer)
read_buffer = Queue(maxsize=args.rbuffer)
_thread.start_new_thread(build_read_buffer, (args, read_buffer, videogen))

for x in range(args.wthreads):
    _thread.start_new_thread(clear_write_buffer, (args, write_buffer, x))

I1 = torch.from_numpy(np.transpose(lastframe, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
I1 = pad_image(I1)

while True:
    frame = read_buffer.get()
    if frame is None:
        break
    I0 = I1
    I1 = torch.from_numpy(np.transpose(frame, (2,0,1))).to(device, non_blocking=True).unsqueeze(0).float() / 255.
    I1 = pad_image(I1)

    output = make_inference(I0, I1, args.multi-1)
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
    time.sleep(0.5)
time.sleep(0.5)
