import os
import numpy as np
import torch
from torch.utils.data import Dataset, DataLoader
from torchvision import transforms
from PIL import Image
import random
import glob
import pdb


class GoPro(Dataset):
    def __init__(self, data_root , mode="train", interFrames=3, n_inputs=4, ext="png"):

        super().__init__()

        self.interFrames = interFrames
        self.n_inputs = n_inputs
        self.setLength = (n_inputs-1)*(interFrames+1)+1 ## We require these many frames in total for interpolating `interFrames` number of
                                                ## intermediate frames with `n_input` input frames.
        self.data_root = os.path.join(data_root , mode)
        
        video_list = os.listdir(self.data_root)
        self.frames_list = []

        self.file_list = []
        for video in video_list:
            frames = sorted(os.listdir(os.path.join(self.data_root , video)))
            n_sets = (len(frames) - self.setLength)//(interFrames+1)  + 1
            videoInputs = [frames[(interFrames+1)*i:(interFrames+1)*i+self.setLength ] for i in range(n_sets)]
            videoInputs = [[os.path.join(video , f) for f in group] for group in videoInputs]
            self.file_list.extend(videoInputs)

        self.transforms = transforms.Compose([
                transforms.CenterCrop(512),
                transforms.ToTensor()
            ])

    def __getitem__(self, idx):

        imgpaths = [os.path.join(self.data_root , fp) for fp in self.file_list[idx]]
        if random.random() > 0.5:
            imgpaths = imgpaths[::-1] ## random temporal flip

        # We can use compression based augmentations

        pick_idxs = list(range(0,self.setLength,self.interFrames+1))
        rem = self.interFrames%2
        gt_idx = list(range(self.setLength//2-self.interFrames//2 , self.setLength//2+self.interFrames//2+rem)) 

        input_paths = [imgpaths[idx] for idx in pick_idxs]
        gt_paths = [imgpaths[idx] for idx in gt_idx]
       
        images = [Image.open(pth_) for pth_ in input_paths]
        images = [self.transforms(img_) for img_ in images]

        gt_images = [Image.open(pth_) for pth_ in gt_paths]
        gt_images = [self.transforms(img_) for img_ in gt_images] 

        return images , gt_images

    def __len__(self):

        return len(self.file_list)

def get_loader(data_root, batch_size, shuffle, num_workers, test_mode=True, interFrames=3, n_inputs=4):

    if test_mode:
        mode = "test"
    else:
        mode = "train"

    dataset = GoPro(data_root , mode, interFrames=interFrames, n_inputs=n_inputs)
    return DataLoader(dataset, batch_size=batch_size, shuffle=shuffle, num_workers=num_workers, pin_memory=True)

if __name__ == "__main__":

    dataset = GoPro("./GoPro" , mode="train", interFrames=3, n_inputs=4)

    print(len(dataset))

    dataloader = DataLoader(dataset , batch_size=1, shuffle=True, num_workers=0)
