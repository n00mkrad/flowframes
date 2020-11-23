import sys
import cv2
import os
import numpy as np
import shutil
import torch
import torchvision
from torchvision import transforms
from torch.nn import functional as F
from PIL import Image
from model.RIFE import Model
from glob import glob
from imageio import imread, imsave
from torch.autograd import Variable

RIFE_model = Model()
RIFE_model.load_model('./models')
RIFE_model.eval()
RIFE_model.device()
 
#print("Input Path: {0}".format(sys.argv[1]))
path = sys.argv[1]

name = os.path.basename(path)
length = len(glob(path + '/*.png'))
interp_output_path = path.replace(name, name+'-interpolated')
os.makedirs(interp_output_path, exist_ok = True)
output_path = path.replace('tmp', 'output')
if os.path.isfile(output_path):
    exit

with torch.no_grad():
    if not os.path.isfile('{:s}/00000001.png'.format(interp_output_path)):
        output_frame_number = 1
        for input_frame_number in range(1, length):
            print("Interpolating frame {0} of {1}...".format(input_frame_number, length))
            frame_0_path = '{:s}/{:08d}.png'.format(path, input_frame_number)
            frame_1_path = '{:s}/{:08d}.png'.format(path, input_frame_number + 1)
            frame_0 = cv2.imread(frame_0_path)
            frame_1 = cv2.imread(frame_1_path)

            h, w, _ = frame_0.shape
            ph = h // 32 * 32+32
            pw = w // 32 * 32+32
            padding = (0, pw - w, 0, ph - h)
            frame_0 = torch.tensor(frame_0.transpose(2, 0, 1)).cuda() / 255.
            frame_1 = torch.tensor(frame_1.transpose(2, 0, 1)).cuda() / 255.
            imgs = F.pad(torch.cat((frame_0, frame_1), 0).float(), padding)
            res = RIFE_model.inference(imgs.unsqueeze(0)) * 255

            shutil.copyfile(frame_0_path, '{:s}/{:08d}.png'.format(interp_output_path, output_frame_number))
            output_frame_number += 1
            cv2.imwrite('{:s}/{:08d}.png'.format(interp_output_path, output_frame_number), res[0].cpu().numpy().transpose(1, 2, 0)[:h, :w])
            output_frame_number += 1

            if output_frame_number == length*2 - 1:
                shutil.copyfile(frame_1_path, '{:s}/{:08d}.png'.format(interp_output_path, output_frame_number))
                output_frame_number += 1
print("Done!")