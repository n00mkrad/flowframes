import os
import numpy as np
import torch
from torch.utils.data import Dataset, DataLoader
from torchvision import transforms
from PIL import Image
import random
import glob


class Middelburry(Dataset):
    def __init__(self, data_root , ext="png"):

        super().__init__()

        self.data_root = data_root
        self.file_list = os.listdir(self.data_root)

        self.transforms = transforms.Compose([
                transforms.ToTensor()
            ])

    def __getitem__(self, idx):

        imgpath = os.path.join(self.data_root , self.file_list[idx])
        name = self.file_list[idx]
        if name == "Teddy": ## Handle inputs with just two inout frames. FLAVR takes atleast 4.
            imgpaths = [os.path.join(imgpath , "frame10.png") , os.path.join(imgpath , "frame10.png") ,os.path.join(imgpath , "frame11.png") ,os.path.join(imgpath , "frame11.png") ]
        else:
            imgpaths = [os.path.join(imgpath , "frame09.png") , os.path.join(imgpath , "frame10.png") ,os.path.join(imgpath , "frame11.png") ,os.path.join(imgpath , "frame12.png") ]

        images = [Image.open(img).convert('RGB') for img in imgpaths]
        images = [self.transforms(img) for img in images]

        sizes = images[0].shape

        return images , name 

    def __len__(self):

        return len(self.file_list)

def get_loader(data_root, batch_size, shuffle, num_workers, test_mode=True):

    dataset =  Middelburry(data_root)
    return DataLoader(dataset, batch_size=batch_size, shuffle=False, num_workers=num_workers, pin_memory=True)
