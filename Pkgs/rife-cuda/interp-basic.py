import sys
import cv2
import os
import numpy as np
import shutil
import argparse
import torch
import torchvision
from torchvision import transforms
from torch.nn import functional as F
from PIL import Image

abspath = os.path.abspath(__file__)
dname = os.path.dirname(abspath)
print("Changing working dir to {0}".format(dname))
os.chdir(os.path.dirname(dname))
print("Added {0} to PATH".format(dname))
sys.path.append(dname)

from model.RIFE import Model
from glob import glob
from imageio import imread, imsave
from torch.autograd import Variable

device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

if torch.cuda.is_available():
    torch.set_grad_enabled(False)
    torch.backends.cudnn.enabled = True
    torch.backends.cudnn.benchmark = True
else:
    print("WARNING: CUDA is not available, RIFE is running on CPU! [ff:nocuda-cpu]")

RIFE_model = Model()
RIFE_model.load_model(os.path.join(dname, "models"))
RIFE_model.eval()
RIFE_model.device()
 

parser = argparse.ArgumentParser()
parser.add_argument('--input', required=True)
parser.add_argument('--output', required=False, default='frames-interpolated')
parser.add_argument('--times', default=2, type=int)
parser.add_argument('--imgformat', default="png")
args = parser.parse_args()

path = args.input


name = os.path.basename(path)
length = len(glob(path + '/*.png'))
#interp_output_path = path.replace(name, name+'-interpolated')
interp_output_path = (args.output).join(path.rsplit(name, 1))
os.makedirs(interp_output_path, exist_ok = True)
#output_path = path.replace('tmp', 'output')

try:
    print("In Path:  {0}".format(path))
    print("Out Path: {0}".format(interp_output_path))
except:
    print("Failed to print in/out paths. This might not be a problem, but it shouldn't happen either.")

#if os.path.isfile(output_path):
#    exit

ext = args.imgformat

with torch.no_grad():
#    if not os.path.isfile('{:s}/00000001.png'.format(interp_output_path)):
    output_frame_number = 1
    # shutil.copyfile('{:s}/{:08d}.png'.format(path, output_frame_number), '{:s}/00000001.png'.format(interp_output_path))    # Copy first frame
    cv2.imwrite('{:s}/00000001.{}'.format(interp_output_path, ext), cv2.imread('{:s}/{:08d}.png'.format(path, output_frame_number), 1))      # Write first frame
    output_frame_number += 1
    for input_frame_number in range(1, length):
        print("Interpolating frame {0} of {1}...".format(input_frame_number, length))
        frame_0_path = '{:s}/{:08d}.png'.format(path, input_frame_number)
        frame_1_path = '{:s}/{:08d}.png'.format(path, input_frame_number + 1)
        frame0 = cv2.imread(frame_0_path)
        frame1 = cv2.imread(frame_1_path)
        
        img0 = (torch.tensor(frame0.transpose(2, 0, 1)).to(device, non_blocking=True) / 255.).unsqueeze(0)
        img1 = (torch.tensor(frame1.transpose(2, 0, 1)).to(device, non_blocking=True) / 255.).unsqueeze(0)
        n, c, h, w = img0.shape
        ph = ((h - 1) // 32 + 1) * 32
        pw = ((w - 1) // 32 + 1) * 32
        padding = (0, pw - w, 0, ph - h)
        img0 = F.pad(img0, padding)
        img1 = F.pad(img1, padding)

        img_list = [img0, img1]
        for i in range(args.times):
            tmp = []
            for j in range(len(img_list) - 1):
                mid = RIFE_model.inference(img_list[j], img_list[j + 1])
                tmp.append(img_list[j])
                tmp.append(mid)
            tmp.append(img1)
            img_list = tmp
        
        #print("Out Frame Num: {0}".format(output_frame_number))
        for i in range(len(img_list)):
            if i == 0:
                continue
            cv2.imwrite('{:s}/{:08d}.{}'.format(interp_output_path, output_frame_number, ext), (img_list[i][0] * 255).byte().cpu().numpy().transpose(1, 2, 0)[:h, :w])
            #print("Writing image from array")
            #print("Out Frame Num: {0}".format(output_frame_number))
            output_frame_number += 1
            print("Written output frame {0}.".format(output_frame_number))
            

input_frame_number += 1;
print("Copying frame {0} of {1}...".format(input_frame_number, length))

print("Copying in/{0} to out/{1}".format(input_frame_number, output_frame_number))
# shutil.copyfile('{:s}/{:08d}.png'.format(path, input_frame_number), '{:s}/{:08d}.png'.format(interp_output_path, output_frame_number))    # Copy last frame
cv2.imwrite('{:s}/{:08d}.{}'.format(interp_output_path, output_frame_number, ext), cv2.imread('{:s}/{:08d}.png'.format(path, input_frame_number), 1))      # Write last frame

print("Done!")







