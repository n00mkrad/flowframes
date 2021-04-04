import os
import torch
import cv2
import pdb
import time
import sys
import torchvision
from PIL import Image
import numpy as np
import _thread
from torchvision.io import read_video, write_video
import torch.nn.functional as F

abspath = os.path.abspath(__file__)
dname = os.path.dirname(abspath)
print("Changing working dir to {0}".format(dname))
os.chdir(os.path.dirname(dname))
print("Added {0} to temporary PATH".format(dname))
sys.path.append(dname)

from dataset.transforms import ToTensorVideo, Resize

import argparse

parser = argparse.ArgumentParser()

parser.add_argument('--input', dest='input', type=str, default=None)
parser.add_argument('--output', required=False, default='frames-interpolated')
parser.add_argument("--factor", type=int, choices=[2,4,8], help="How much interpolation needed. 2x/4x/8x.")
parser.add_argument("--model", type=str, help="path for stored model")
parser.add_argument("--up_mode", type=str, help="Upsample Mode", default="transpose")
parser.add_argument('--fp16', dest='fp16', action='store_true', help='half-precision mode')
parser.add_argument('--imgformat', default="png")
parser.add_argument("--output_ext", type=str, help="Output video format", default=".avi")
parser.add_argument("--input_ext", type=str, help="Input video format", default=".mp4")
parser.add_argument("--downscale", type=float, help="Downscale input res. for memory", default=1)
args = parser.parse_args()

input_ext = args.input_ext

path = args.input
base = os.path.basename(path)
interp_input_path = os.path.join(dname, args.input)
interp_output_path = os.path.join(dname, args.output)


torch.set_grad_enabled(False)
if torch.cuda.is_available():
    torch.backends.cudnn.enabled = True
    torch.backends.cudnn.benchmark = True
    if(args.fp16):
        torch.set_default_tensor_type(torch.cuda.HalfTensor)
        print("FLAVR is running in FP16 mode.")
else:
    print("WARNING: CUDA is not available, FLAVR is running on CPU! [ff:nocuda-cpu]")


n_outputs = args.factor - 1

model_name = "unet_18"
nbr_frame = 4
joinType = "concat"

def loadModel(model, checkpoint):
    
    saved_state_dict = torch.load(checkpoint)['state_dict']
    saved_state_dict = {k.partition("module.")[-1]:v for k,v in saved_state_dict.items()}
    model.load_state_dict(saved_state_dict)

checkpoint = os.path.join(dname, args.model)
from model.FLAVR_arch import UNet_3D_3D

model = UNet_3D_3D(model_name.lower(), n_inputs=4, n_outputs=n_outputs,  joinType=joinType, upmode=args.up_mode)
loadModel(model, checkpoint)
model = model.cuda()

in_files = sorted(os.listdir(interp_input_path))

def make_image(img):
    q_im = img.data.mul(255.).clamp(0,255).round()
    im = q_im.permute(1, 2, 0).cpu().numpy().astype(np.uint8)
    im = cv2.cvtColor(im, cv2.COLOR_RGB2BGR)
    return im

def files_to_videoTensor(path, downscale=1.):
    from PIL import Image
    global in_files
    in_files_fixed = in_files
    in_files_fixed.insert(0, in_files[0])   # Workaround: Insert extra entry before
    in_files_fixed.append(in_files[-1])   # Workaround: Insert extra entry after
    images = [torch.Tensor(np.asarray(Image.open(os.path.join(path, f)))).type(torch.uint8) for f in in_files]
    print(images[0].shape)
    videoTensor = torch.stack(images)
    return videoTensor

def video_transform(videoTensor, downscale=1):
    T, H, W = videoTensor.size(0), videoTensor.size(1), videoTensor.size(2)
    downscale = int(downscale * 8)
    resizes = 8*(H//downscale), 8*(W//downscale)
    transforms = torchvision.transforms.Compose([ToTensorVideo(), Resize(resizes)])
    videoTensor = transforms(videoTensor)
    
    print("Resizing to %dx%d"%(resizes[0], resizes[1]) )
    return videoTensor, resizes

videoTensor = files_to_videoTensor(interp_input_path, args.downscale)

print(f"Video Tensor len: {len(videoTensor)}")
idxs = torch.Tensor(range(len(videoTensor))).type(torch.long).view(1, -1).unfold(1,size=nbr_frame,step=1).squeeze(0)
print(f"len(idxs): {len(idxs)}")
videoTensor, resizes = video_transform(videoTensor, args.downscale)
print("Video tensor shape is ", videoTensor.shape)

frames = torch.unbind(videoTensor, 1)
n_inputs = len(frames)
width = n_outputs + 1


model = model.eval()

frame_num = 1

def load_and_write_img (writedir, writename, path_load):
    os.chdir(writedir)
    cv2.imwrite(writename, cv2.imdecode(np.fromfile(path_load, dtype=np.uint8), cv2.IMREAD_UNCHANGED), [cv2.IMWRITE_PNG_COMPRESSION, 1])

def write_img (writedir, writename, img):
    os.chdir(writedir)
    cv2.imwrite(writename, img, [cv2.IMWRITE_PNG_COMPRESSION, 1])


for i in (range(len(idxs))):
    idxSet = idxs[i]
    inputs = [frames[idx_].cuda().unsqueeze(0) for idx_ in idxSet]
    with torch.no_grad():
        outputFrame = model(inputs)   
    outputFrame = [of.squeeze(0).cpu().data for of in outputFrame]
    #outputs.extend(outputFrame)
    #outputs.append(inputs[2].squeeze(0).cpu().data)
    
    print(f"Frame {i}")
    
    print(f"Writing source frame {'{:0>8d}.{}'.format(frame_num, args.imgformat)}")
    input_frame_path = os.path.join(interp_input_path, in_files[i+1])
    _thread.start_new_thread(load_and_write_img, (interp_output_path, '{:0>8d}.{}'.format(frame_num, args.imgformat), input_frame_path))
    frame_num += 1
    
    for img in outputFrame:
        print(f"Writing interp frame {'{:0>8d}.{}'.format(frame_num, args.imgformat)}")
        _thread.start_new_thread(write_img, (interp_output_path, '{:0>8d}.{}'.format(frame_num, args.imgformat), make_image(img)))
        frame_num += 1

print(f"Writing source frame {frame_num} [LAST]")
input_frame_path = os.path.join(interp_input_path, in_files[-1])
os.chdir(interp_output_path)
cv2.imwrite('{:0>8d}.{}'.format(frame_num, args.imgformat), cv2.imdecode(np.fromfile(input_frame_path, dtype=np.uint8), cv2.IMREAD_UNCHANGED), [cv2.IMWRITE_PNG_COMPRESSION, 2])      # Last input frame

time.sleep(0.5)