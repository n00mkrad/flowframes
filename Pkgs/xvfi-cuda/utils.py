from __future__ import division
import os, glob, sys, torch, shutil, random, math, time, cv2
import numpy as np
import torch.utils.data as data
import torch.nn as nn
#import pandas as pd
import torch.nn.functional as F
from datetime import datetime
from torch.nn import init
#from skimage.measure import compare_ssim
#from skimage.metrics import structural_similarity
from torch.autograd import Variable
from torchvision import models


class save_manager():
    def __init__(self, args):
        self.args = args
        self.model_dir = self.args.net_type + '_' + self.args.dataset + '_exp' + str(self.args.exp_num)
        print("model_dir:", self.model_dir)
        # ex) model_dir = "XVFInet_exp1"

        self.checkpoint_dir = os.path.join(self.args.checkpoint_dir, self.model_dir)
        # './checkpoint_dir/XVFInet_exp1"
        #check_folder(self.checkpoint_dir)

        #print("checkpoint_dir:", self.checkpoint_dir)

        #self.text_dir = os.path.join(self.args.text_dir, self.model_dir)
        #print("text_dir:", self.text_dir)

        #""" Save a text file """
        #if not os.path.exists(self.text_dir + '.txt'):
        #    self.log_file = open(self.text_dir + '.txt', 'w')
        #    # "w" - Write - Opens a file for writing, creates the file if it does not exist
        #    self.log_file.write('----- Model parameters -----\n')
        #    self.log_file.write(str(datetime.now())[:-7] + '\n')
        #    for arg in vars(self.args):
        #        self.log_file.write('{} : {}\n'.format(arg, getattr(self.args, arg)))
        #    # ex) ./text_dir/XVFInet_exp1.txt
        #    self.log_file.close()

    # "a" - Append - Opens a file for appending, creates the file if it does not exist

    def write_info(self, strings):
        self.log_file = open(self.text_dir + '.txt', 'a')
        self.log_file.write(strings)
        self.log_file.close()

    def save_best_model(self, combined_state_dict, best_PSNR_flag):
        file_name = os.path.join(self.checkpoint_dir, self.model_dir + '_latest.pt')
        # file_name = "./checkpoint_dir/XVFInet_exp1/XVFInet_exp1_latest.ckpt
        torch.save(combined_state_dict, file_name)
        if best_PSNR_flag:
            shutil.copyfile(file_name, os.path.join(self.checkpoint_dir, self.model_dir + '_best_PSNR.pt'))

    # file_path = "./checkpoint_dir/XVFInet_exp1/XVFInet_exp1_best_PSNR.ckpt

    def save_epc_model(self, combined_state_dict, epoch):
        file_name = os.path.join(self.checkpoint_dir, self.model_dir + '_epc' + str(epoch) + '.pt')
        # file_name = "./checkpoint_dir/XVFInet_exp1/XVFInet_exp1_epc10.ckpt
        torch.save(combined_state_dict, file_name)

    def load_epc_model(self, epoch):
        checkpoint = torch.load(os.path.join(self.checkpoint_dir, self.model_dir + '_epc' + str(epoch - 1) + '.pt'))
        print("load model '{}', epoch: {}, best_PSNR: {:3f}".format(
            os.path.join(self.checkpoint_dir, self.model_dir + '_epc' + str(epoch - 1) + '.pt'), checkpoint['last_epoch'] + 1,
            checkpoint['best_PSNR']))
        return checkpoint

    def load_model(self, mdl_dir):
        # checkpoint = torch.load(self.checkpoint_dir + '/' + self.model_dir + '_latest.pt')
        checkpoint = torch.load(os.path.join(mdl_dir, "checkpoint.pt"), map_location='cuda:0')
        print("load model '{}', epoch: {},".format(
            os.path.join(mdl_dir, "checkpoint.pt"), checkpoint['last_epoch'] + 1))
        return checkpoint

    def load_best_PSNR_model(self, ):
        checkpoint = torch.load(os.path.join(self.checkpoint_dir, self.model_dir + '_best_PSNR.pt'))
        print("load _best_PSNR model '{}', epoch: {}, best_PSNR: {:3f}, best_SSIM: {:3f}".format(
            os.path.join(self.checkpoint_dir, self.model_dir + '_best_PSNR.pt'), checkpoint['last_epoch'] + 1,
            checkpoint['best_PSNR'], checkpoint['best_SSIM']))
        return checkpoint


def check_folder(log_dir):
    if not os.path.exists(log_dir):
        os.makedirs(log_dir)
    return log_dir


def weights_init(m):
    classname = m.__class__.__name__
    if (classname.find('Conv2d') != -1) or (classname.find('Conv3d') != -1):
        init.xavier_normal_(m.weight)
        # init.kaiming_normal_(m.weight, nonlinearity='relu')
        if hasattr(m, 'bias') and m.bias is not None:
            init.zeros_(m.bias)


def get_train_data(args, max_t_step_size):
    if args.dataset == 'X4K1000FPS':
        data_train = X_Train(args, max_t_step_size)
    elif args.dataset == 'Vimeo':
        data_train = Vimeo_Train(args)
    dataloader = torch.utils.data.DataLoader(data_train, batch_size=args.batch_size, drop_last=True, shuffle=True,
                                             num_workers=int(args.num_thrds), pin_memory=False)
    return dataloader


def get_test_data(args, multiple, validation):
    if args.dataset == 'X4K1000FPS' and args.phase != 'test_custom':
        data_test = X_Test(args, multiple, validation)  # 'validation' for validation while training for simplicity
    elif args.dataset == 'Vimeo' and args.phase != 'test_custom':
        data_test = Vimeo_Test(args, validation)
    elif args.phase == 'test_custom':
        data_test = Custom_Test(args, multiple)
    dataloader = torch.utils.data.DataLoader(data_test, batch_size=1, drop_last=True, shuffle=False, pin_memory=False)
    return dataloader


def frames_loader_train(args, candidate_frames, frameRange):
    frames = []
    for frameIndex in frameRange:
        frame = cv2.imread(candidate_frames[frameIndex])
        frames.append(frame)
    (ih, iw, c) = frame.shape
    frames = np.stack(frames, axis=0)  # (T, H, W, 3)
    if args.need_patch:  ## random crop
        ps = args.patch_size
        ix = random.randrange(0, iw - ps + 1)
        iy = random.randrange(0, ih - ps + 1)
        frames = frames[:, iy:iy + ps, ix:ix + ps, :]

    if random.random() < 0.5:  # random horizontal flip
        frames = frames[:, :, ::-1, :]

    # No vertical flip

    rot = random.randint(0, 3)  # random rotate
    frames = np.rot90(frames, rot, (1, 2))

    """ np2Tensor [-1,1] normalized """
    frames = RGBframes_np2Tensor(frames, args.img_ch)

    return frames


def frames_loader_test(args, I0I1It_Path, validation):
    frames = []
    for path in I0I1It_Path:
        frame = cv2.imread(path)
        frames.append(frame)
    (ih, iw, c) = frame.shape
    frames = np.stack(frames, axis=0)  # (T, H, W, 3)

    if args.dataset == 'X4K1000FPS':
        if validation:
            ps = 512
            ix = (iw - ps) // 2
            iy = (ih - ps) // 2
            frames = frames[:, iy:iy + ps, ix:ix + ps, :]

    """ np2Tensor [-1,1] normalized """
    frames = RGBframes_np2Tensor(frames, args.img_ch)

    return frames


def RGBframes_np2Tensor(imgIn, channel):
    ## input : T, H, W, C
    if channel == 1:
        # rgb --> Y (gray)
        imgIn = np.sum(imgIn * np.reshape([65.481, 128.553, 24.966], [1, 1, 1, 3]) / 255.0, axis=3,
                       keepdims=True) + 16.0

    # to Tensor
    ts = (3, 0, 1, 2)  ############# dimension order should be [C, T, H, W]
    imgIn = torch.Tensor(imgIn.transpose(ts).astype(float)).mul_(1.0)

    # normalization [-1,1]
    imgIn = (imgIn / 255.0 - 0.5) * 2

    return imgIn


    def __init__(self, args, multiple, validation):
        self.args = args
        self.multiple = multiple
        self.validation = validation
        if validation:
            self.testPath = make_2D_dataset_X_Test(self.args.val_data_path, multiple, t_step_size=32)
        else:  ## test
            self.testPath = make_2D_dataset_X_Test(self.args.test_data_path, multiple, t_step_size=32)

        self.nIterations = len(self.testPath)

        # Raise error if no images found in test_data_path.
        if len(self.testPath) == 0:
            if validation:
                raise (RuntimeError("Found 0 files in subfolders of: " + self.args.val_data_path + "\n"))
            else:
                raise (RuntimeError("Found 0 files in subfolders of: " + self.args.test_data_path + "\n"))

    def __getitem__(self, idx):
        I0, I1, It, t_value, scene_name = self.testPath[idx]

        I0I1It_Path = [I0, I1, It]

        frames = frames_loader_test(self.args, I0I1It_Path, self.validation)
        # including "np2Tensor [-1,1] normalized"

        I0_path = I0.split(os.sep)[-1]
        I1_path = I1.split(os.sep)[-1]
        It_path = It.split(os.sep)[-1]

        return frames, np.expand_dims(np.array(t_value, dtype=np.float32), 0), scene_name, [It_path, I0_path, I1_path]

    def __len__(self):
        return self.nIterations


    def __init__(self, args, validation):
        self.args = args
        self.framesPath = []
        f = open(os.path.join(args.vimeo_data_path, 'tri_testlist.txt'), 'r')
        while True:
            scene_path = f.readline().split('\n')[0]
            if not scene_path: break
            frames_list = sorted(glob.glob(os.path.join(args.vimeo_data_path, 'sequences', scene_path,
                                                        '*.*')))  # '../Datasets/vimeo_triplet/sequences/%05d/%04d/*.png'
            self.framesPath.append(frames_list)
        if validation:
            self.framesPath = self.framesPath[::37]
        f.close

        self.num_scene = len(self.framesPath)  # total test scenes
        if len(self.framesPath) == 0:
            raise (RuntimeError("Found no files in subfolders of: " + args.vimeo_data_path + "\n"))
        else:
            print("# of Vimeo triplet testset : ", self.num_scene)

    def __getitem__(self, idx):
        scene_name = self.framesPath[idx][0].split(os.sep)
        scene_name = os.path.join(scene_name[-3], scene_name[-2])
        I0, It, I1 = self.framesPath[idx]
        I0I1It_Path = [I0, I1, It]
        frames = frames_loader_test(self.args, I0I1It_Path, validation=False)

        I0_path = I0.split(os.sep)[-1]
        I1_path = I1.split(os.sep)[-1]
        It_path = It.split(os.sep)[-1]

        return frames, np.expand_dims(np.array(0.5, dtype=np.float32), 0), scene_name, [It_path, I0_path, I1_path]

    def __len__(self):
        return self.num_scene

def make_2D_dataset_Custom_Test(dir, multiple):
    """ make [I0,I1,It,t,scene_folder] """
    """ 1D (accumulated) """
    testPath = []
    t = np.linspace((1 / multiple), (1 - (1 / multiple)), (multiple - 1))
    for scene_folder in sorted(glob.glob(os.path.join(dir, '*', ''))):  # [scene1, scene2, scene3, ...]
        frame_folder = sorted(glob.glob(scene_folder + '*.*'))  # ex) ['00000.png',...,'00123.png']
        for idx in range(0, len(frame_folder)):
            if idx == len(frame_folder) - 1:
                break
            for suffix, mul in enumerate(range(multiple - 1)):
                I0I1It_paths = []
                I0I1It_paths.append(frame_folder[idx])  # I0 (fix)
                I0I1It_paths.append(frame_folder[idx + 1])  # I1 (fix)
                target_t_Idx = frame_folder[idx].split(os.sep)[-1].split('.')[0]+'_' + str(suffix).zfill(3) + '.png'
                # ex) target t name: 00017.png => '00017_1.png'
                I0I1It_paths.append(os.path.join(scene_folder, target_t_Idx))  # It
                I0I1It_paths.append(t[mul]) # t
                I0I1It_paths.append(frame_folder[idx].split(os.path.join(dir, ''))[-1].split(os.sep)[0])  # scene1
                testPath.append(I0I1It_paths)
        break # limit to 1 directory - nmkd
    return testPath



class Custom_Test(data.Dataset):
    def __init__(self, args, multiple):
        self.args = args
        self.multiple = multiple
        self.testPath = make_2D_dataset_Custom_Test(self.args.custom_path, self.multiple)
        self.nIterations = len(self.testPath)

        # Raise error if no images found in test_data_path.
        if len(self.testPath) == 0:
            raise (RuntimeError("Found 0 files in subfolders of: " + self.args.custom_path + "\n"))

    def __getitem__(self, idx):
        I0, I1, It, t_value, scene_name = self.testPath[idx]
        dummy_dir = I1 # due to there is not ground truth intermediate frame.
        I0I1It_Path = [I0, I1, dummy_dir]

        frames = frames_loader_test(self.args, I0I1It_Path, None)
        # including "np2Tensor [-1,1] normalized"

        I0_path = I0.split(os.sep)[-1]
        I1_path = I1.split(os.sep)[-1]
        It_path = It.split(os.sep)[-1]

        return frames, np.expand_dims(np.array(t_value, dtype=np.float32), 0), scene_name, [It_path, I0_path, I1_path]

    def __len__(self):
        return self.nIterations


class L1_Charbonnier_loss(nn.Module):
    """L1 Charbonnierloss."""

    def __init__(self):
        super(L1_Charbonnier_loss, self).__init__()
        self.epsilon = 1e-3

    def forward(self, X, Y):
        loss = torch.mean(torch.sqrt((X - Y) ** 2 + self.epsilon ** 2))
        return loss


def set_rec_loss(args):
    loss_type = args.loss_type
    if loss_type == 'MSE':
        lossfunction = nn.MSELoss()
    elif loss_type == 'L1':
        lossfunction = nn.L1Loss()
    elif loss_type == 'L1_Charbonnier_loss':
        lossfunction = L1_Charbonnier_loss()

    return lossfunction


class AverageClass(object):
    """ For convenience of averaging values """
    """ refer from "https://github.com/pytorch/examples/blob/master/imagenet/main.py" """

    def __init__(self, name, fmt=':f'):
        self.name = name
        self.fmt = fmt
        self.reset()

    def reset(self):
        self.val = 0.0
        self.avg = 0.0
        self.sum = 0.0
        self.count = 0.0

    def update(self, val, n=1.0):
        self.val = val
        self.sum += val * n
        self.count += n
        self.avg = self.sum / self.count

    def __str__(self):
        fmtstr = '{name} {val' + self.fmt + '} (avg:{avg' + self.fmt + '})'
        # Accm_Time[s]: 1263.517 (avg:639.701)    (<== if AverageClass('Accm_Time[s]:', ':6.3f'))
        return fmtstr.format(**self.__dict__)


class ProgressMeter(object):
    """ For convenience of printing diverse values by using "AverageClass" """
    """ refer from "https://github.com/pytorch/examples/blob/master/imagenet/main.py" """

    def __init__(self, num_batches, *meters, prefix=""):
        self.batch_fmtstr = self._get_batch_fmtstr(num_batches)
        self.meters = meters
        self.prefix = prefix

    def print(self, batch):
        entries = [self.prefix + self.batch_fmtstr.format(batch)]
        # # Epoch: [0][  0/196]
        entries += [str(meter) for meter in self.meters]
        print('\t'.join(entries))

    def _get_batch_fmtstr(self, num_batches):
        num_digits = len(str(num_batches // 1))
        fmt = '{:' + str(num_digits) + 'd}'
        return '[' + fmt + '/' + fmt.format(num_batches) + ']'


def metrics_evaluation_X_Test(pred_save_path, test_data_path, metrics_types, flow_flag=False, multiple=8, server=None):
    """
        pred_save_path = './test_img_dir/XVFInet_exp1/epoch_00099' when 'args.epochs=100'
        test_data_path = ex) 'F:/Jihyong/4K_1000fps_dataset/VIC_4K_1000FPS/X_TEST'
            format: -type1
                        -scene1
                            :
                        -scene5
                    -type2
                            :
                    -type3
                        :
                        -scene5
        "metrics_types": ["PSNR", "SSIM", "LPIPS", "tOF", "tLP100"]
        "flow_flag": option for saving motion visualization
        "final_test_type": ['first_interval', 1, 2, 3, 4]
        "multiple": x4, x8, x16, x32 for interpolation
     """

    pred_framesPath = []
    for type_folder in sorted(glob.glob(os.path.join(pred_save_path, '*', ''))):  # [type1,type2,type3,...]
        for scene_folder in sorted(glob.glob(os.path.join(type_folder, '*', ''))):  # [scene1,scene2,..]
            scene_framesPath = []
            for frame_path in sorted(glob.glob(scene_folder + '*.png')):
                scene_framesPath.append(frame_path)
            pred_framesPath.append(scene_framesPath)
    if len(pred_framesPath) == 0:
        raise (RuntimeError("Found 0 files in " + pred_save_path + "\n"))

    # GT_framesPath = make_2D_dataset_X_Test(test_data_path, multiple, t_step_size=32)
    # pred_framesPath = make_2D_dataset_X_Test(pred_save_path, multiple, t_step_size=32)

    # ex) pred_save_path: './test_img_dir/XVFInet_exp1/epoch_00099' when 'args.epochs=100'
    # ex) framesPath: [['./VIC_4K_1000FPS/VIC_Test/Fast/003_TEST_Fast/00000.png',...], ..., []] 2D List, len=30
    # ex) scenesFolder: ['Fast/003_TEST_Fast',...]

    keys = metrics_types
    len_dict = dict.fromkeys(keys, 0)
    Total_avg_dict = dict.fromkeys(["TotalAvg_" + _ for _ in keys], 0)
    Type1_dict = dict.fromkeys(["Type1Avg_" + _ for _ in keys], 0)
    Type2_dict = dict.fromkeys(["Type2Avg_" + _ for _ in keys], 0)
    Type3_dict = dict.fromkeys(["Type3Avg_" + _ for _ in keys], 0)

    # LPIPSnet = dm.DistModel()
    # LPIPSnet.initialize(model='net-lin', net='alex', use_gpu=True)

    total_list_dict = {}
    key_str = 'Metrics -->'
    for key_i in keys:
        total_list_dict[key_i] = []
        key_str += ' ' + str(key_i)
    key_str += ' will be measured.'
    print(key_str)

    for scene_idx, scene_folder in enumerate(pred_framesPath):
        per_scene_list_dict = {}
        for key_i in keys:
            per_scene_list_dict[key_i] = []
        pred_candidate = pred_framesPath[scene_idx]  # get all frames in pred_framesPath
        # GT_candidate = GT_framesPath[scene_idx]  # get 4800 frames
        # num_pred_frame_per_folder = len(pred_candidate)

        # save_path = os.path.join(pred_save_path, pred_scenesFolder[scene_idx])
        save_path = scene_folder[0]
        # './test_img_dir/XVFInet_exp1/epoch_00099/type1/scene1'

        # excluding both frame0 and frame1 (multiple of 32 indices)
        for frameIndex, pred_frame in enumerate(pred_candidate):
            # if server==87:
            # GTinterFrameIdx = pred_frame.split('/')[-1]  # ex) 8, when multiple = 4, # 87 server
            # else:
            # GTinterFrameIdx = pred_frame.split('\\')[-1]  # ex) 8, when multiple = 4
            # if not (GTinterFrameIdx % 32) == 0:
            if frameIndex > 0 and frameIndex < multiple:
                """ only compute predicted frames (excluding multiples of 32 indices), ex) 8, 16, 24, 40, 48, 56, ... """
                output_img = cv2.imread(pred_frame).astype(np.float32)  # BGR, [0,255]
                target_img = cv2.imread(pred_frame.replace(pred_save_path, test_data_path)).astype(
                    np.float32)  # BGR, [0,255]
                pred_frame_split = pred_frame.split(os.sep)
                msg = "[x%d] frame %s, " % (
                multiple, os.path.join(pred_frame_split[-3], pred_frame_split[-2], pred_frame_split[-1]))  # per frame

                if "tOF" in keys:  # tOF
                    # if (GTinterFrameIdx % 32) == int(32/multiple):
                    # if (frameIndex % multiple) == 1:
                    if frameIndex == 1:
                        # when first predicted frame in each interval
                        pre_out_grey = cv2.cvtColor(cv2.imread(pred_candidate[0]).astype(np.float32),
                                                    cv2.COLOR_BGR2GRAY)  #### CAUTION BRG
                        # pre_tar_grey = cv2.cvtColor(cv2.imread(pred_candidate[0].replace(pred_save_path, test_data_path)), cv2.COLOR_BGR2GRAY)  #### CAUTION BRG
                        pre_tar_grey = pre_out_grey  #### CAUTION BRG

                    # if not H_match_flag or not W_match_flag:
                    #    pre_tar_grey = pre_tar_grey[:new_t_H, :new_t_W, :]

                    # pre_tar_grey = pre_out_grey

                    output_grey = cv2.cvtColor(output_img, cv2.COLOR_BGR2GRAY)
                    target_grey = cv2.cvtColor(target_img, cv2.COLOR_BGR2GRAY)

                    target_OF = cv2.calcOpticalFlowFarneback(pre_tar_grey, target_grey, None, 0.5, 3, 15, 3, 5, 1.2, 0)
                    output_OF = cv2.calcOpticalFlowFarneback(pre_out_grey, output_grey, None, 0.5, 3, 15, 3, 5, 1.2, 0)
                    # target_OF, ofy, ofx = crop_8x8(target_OF) #check for size reason
                    # output_OF, ofy, ofx = crop_8x8(output_OF)
                    OF_diff = np.absolute(target_OF - output_OF)
                    if flow_flag:
                        """ motion visualization """
                        flow_path = save_path + '_tOF_flow'
                        check_folder(flow_path)
                        # './test_img_dir/XVFInet_exp1/epoch_00099/Fast/003_TEST_Fast_tOF_flow'
                        tOFpath = os.path.join(flow_path, "tOF_flow_%05d.png" % (GTinterFrameIdx))
                        # ex) "./test_img_dir/epoch_005/Fast/003_TEST_Fast/00008_tOF" when start_idx=0, multiple=4, frameIndex=0
                        hsv = np.zeros_like(output_img)  # check for size reason
                        hsv[..., 1] = 255
                        mag, ang = cv2.cartToPolar(OF_diff[..., 0], OF_diff[..., 1])
                        # print("tar max %02.6f, min %02.6f, avg %02.6f" % (mag.max(), mag.min(), mag.mean()))
                        maxV = 0.4
                        mag = np.clip(mag, 0.0, maxV) / maxV
                        hsv[..., 0] = ang * 180 / np.pi / 2
                        hsv[..., 2] = mag * 255.0  #
                        bgr = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)
                        cv2.imwrite(tOFpath, bgr)
                        print("png for motion visualization has been saved in [%s]" %
                              (flow_path))
                    OF_diff_tmp = np.sqrt(np.sum(OF_diff * OF_diff, axis=-1)).mean()  # l1 vector norm
                    # OF_diff, ofy, ofx = crop_8x8(OF_diff)
                    total_list_dict["tOF"].append(OF_diff_tmp)
                    per_scene_list_dict["tOF"].append(OF_diff_tmp)
                    msg += "tOF %02.2f, " % (total_list_dict["tOF"][-1])

                    pre_out_grey = output_grey
                    pre_tar_grey = target_grey

                # target_img, ofy, ofx = crop_8x8(target_img)
                # output_img, ofy, ofx = crop_8x8(output_img)

                if "PSNR" in keys:  # psnr
                    psnr_tmp = psnr(target_img, output_img)
                    total_list_dict["PSNR"].append(psnr_tmp)
                    per_scene_list_dict["PSNR"].append(psnr_tmp)
                    msg += "PSNR %02.2f" % (total_list_dict["PSNR"][-1])

                if "SSIM" in keys:  # ssim
                    ssim_tmp = ssim_bgr(target_img, output_img)
                    total_list_dict["SSIM"].append(ssim_tmp)
                    per_scene_list_dict["SSIM"].append(ssim_tmp)

                    msg += ", SSIM %02.2f" % (total_list_dict["SSIM"][-1])

                # msg += ", crop (%d, %d)" % (ofy, ofx) # per frame (not scene)
                print(msg)

        """ after finishing one scene """
        per_scene_pd_dict = {}  # per scene
        for cur_key in keys:
            # save_path = './test_img_dir/XVFInet_exp1/epoch_00099/Fast/003_TEST_Fast'
            num_data = cur_key + "_[x%d]_[%s]" % (multiple, save_path.split(os.sep)[-2])  # '003_TEST_Fast'
            # num_data => ex) PSNR_[x8]_[041_TEST_Fast]
            """ per scene """
            per_scene_cur_list = np.float32(per_scene_list_dict[cur_key])
            per_scene_pd_dict[num_data] = pd.Series(per_scene_cur_list)  # dictionary
            per_scene_num_data_sum = per_scene_cur_list.sum()
            per_scene_num_data_len = per_scene_cur_list.shape[0]
            per_scene_num_data_mean = per_scene_num_data_sum / per_scene_num_data_len
            """ accumulation """
            cur_list = np.float32(total_list_dict[cur_key])
            num_data_sum = cur_list.sum()
            num_data_len = cur_list.shape[0]  # accum
            num_data_mean = num_data_sum / num_data_len
            print(" %s, (per scene) max %02.4f, min %02.4f, avg %02.4f" %
                  (num_data, per_scene_cur_list.max(), per_scene_cur_list.min(), per_scene_num_data_mean))  #

            Total_avg_dict["TotalAvg_" + cur_key] = num_data_mean  # accum, update every iteration.

            len_dict[cur_key] = num_data_len  # accum, update every iteration.

            # folder_dict["FolderAvg_" + cur_key] += num_data_mean
            if scene_idx < 5:
                Type1_dict["Type1Avg_" + cur_key] += per_scene_num_data_mean
            elif (scene_idx >= 5) and (scene_idx < 10):
                Type2_dict["Type2Avg_" + cur_key] += per_scene_num_data_mean
            elif (scene_idx >= 10) and (scene_idx < 15):
                Type3_dict["Type3Avg_" + cur_key] += per_scene_num_data_mean

        mode = 'w' if scene_idx == 0 else 'a'

        total_csv_path = os.path.join(pred_save_path, "total_metrics.csv")
        # ex) pred_save_path: './test_img_dir/XVFInet_exp1/epoch_00099' when 'args.epochs=100'
        pd.DataFrame(per_scene_pd_dict).to_csv(total_csv_path, mode=mode)

    """ combining all results after looping all scenes. """
    for key in keys:
        Total_avg_dict["TotalAvg_" + key] = pd.Series(
            np.float32(Total_avg_dict["TotalAvg_" + key]))  # replace key (update)
        Type1_dict["Type1Avg_" + key] = pd.Series(np.float32(Type1_dict["Type1Avg_" + key] / 5))  # replace key (update)
        Type2_dict["Type2Avg_" + key] = pd.Series(np.float32(Type2_dict["Type2Avg_" + key] / 5))  # replace key (update)
        Type3_dict["Type3Avg_" + key] = pd.Series(np.float32(Type3_dict["Type3Avg_" + key] / 5))  # replace key (update)

        print("%s, total frames %d, total avg %02.4f, Type1 avg %02.4f, Type2 avg %02.4f, Type3 avg %02.4f" %
              (key, len_dict[key], Total_avg_dict["TotalAvg_" + key],
               Type1_dict["Type1Avg_" + key], Type2_dict["Type2Avg_" + key], Type3_dict["Type3Avg_" + key]))

    pd.DataFrame(Total_avg_dict).to_csv(total_csv_path, mode='a')
    pd.DataFrame(Type1_dict).to_csv(total_csv_path, mode='a')
    pd.DataFrame(Type2_dict).to_csv(total_csv_path, mode='a')
    pd.DataFrame(Type3_dict).to_csv(total_csv_path, mode='a')

    print("csv file of all metrics for all scenes has been saved in [%s]" %
          (total_csv_path))
    print("Finished.")


def to_uint8(x, vmin, vmax):
    ##### color space transform, originally from https://github.com/yhjo09/VSR-DUF #####
    x = x.astype('float32')
    x = (x - vmin) / (vmax - vmin) * 255  # 0~255
    return np.clip(np.round(x), 0, 255)


def psnr(img_true, img_pred):
    ##### PSNR with color space transform, originally from https://github.com/yhjo09/VSR-DUF #####
    """
    # img format : [h,w,c], RGB
    """
    # Y_true = _rgb2ycbcr(to_uint8(img_true, 0, 255), 255)[:, :, 0]
    # Y_pred = _rgb2ycbcr(to_uint8(img_pred, 0, 255), 255)[:, :, 0]
    diff = img_true - img_pred
    rmse = np.sqrt(np.mean(np.power(diff, 2)))
    if rmse == 0:
        return float('inf')
    return 20 * np.log10(255. / rmse)


def ssim_bgr(img_true, img_pred):  ##### SSIM for BGR, not RGB #####
    """
    # img format : [h,w,c], BGR
    """
    Y_true = _rgb2ycbcr(to_uint8(img_true, 0, 255)[:, :, ::-1], 255)[:, :, 0]
    Y_pred = _rgb2ycbcr(to_uint8(img_pred, 0, 255)[:, :, ::-1], 255)[:, :, 0]
    # return compare_ssim(Y_true, Y_pred, data_range=Y_pred.max() - Y_pred.min())
    return structural_similarity(Y_true, Y_pred, data_range=Y_pred.max() - Y_pred.min())


def im2tensor(image, imtype=np.uint8, cent=1., factor=255. / 2.):
    # def im2tensor(image, imtype=np.uint8, cent=1., factor=1.):
    return torch.Tensor((image / factor - cent)
                        [:, :, :, np.newaxis].transpose((3, 2, 0, 1)))


# [0,255]2[-1,1]2[1,3,H,W]-shaped

def denorm255(x):
    out = (x + 1.0) / 2.0
    return out.clamp_(0.0, 1.0) * 255.0


def denorm255_np(x):
    # numpy
    out = (x + 1.0) / 2.0
    return out.clip(0.0, 1.0) * 255.0


def _rgb2ycbcr(img, maxVal=255):
    ##### color space transform, originally from https://github.com/yhjo09/VSR-DUF #####
    O = np.array([[16],
                  [128],
                  [128]])
    T = np.array([[0.256788235294118, 0.504129411764706, 0.097905882352941],
                  [-0.148223529411765, -0.290992156862745, 0.439215686274510],
                  [0.439215686274510, -0.367788235294118, -0.071427450980392]])

    if maxVal == 1:
        O = O / 255.0

    t = np.reshape(img, (img.shape[0] * img.shape[1], img.shape[2]))
    t = np.dot(t, np.transpose(T))
    t[:, 0] += O[0]
    t[:, 1] += O[1]
    t[:, 2] += O[2]
    ycbcr = np.reshape(t, [img.shape[0], img.shape[1], img.shape[2]])

    return ycbcr


class set_smoothness_loss(nn.Module):
    def __init__(self, weight=150.0, edge_aware=True):
        super(set_smoothness_loss, self).__init__()
        self.edge_aware = edge_aware
        self.weight = weight ** 2

    def forward(self, flow, img):
        img_gh = torch.mean(torch.pow((img[:, :, 1:, :] - img[:, :, :-1, :]), 2), dim=1, keepdims=True)
        img_gw = torch.mean(torch.pow((img[:, :, :, 1:] - img[:, :, :, :-1]), 2), dim=1, keepdims=True)

        weight_gh = torch.exp(-self.weight * img_gh)
        weight_gw = torch.exp(-self.weight * img_gw)

        flow_gh = torch.abs(flow[:, :, 1:, :] - flow[:, :, :-1, :])
        flow_gw = torch.abs(flow[:, :, :, 1:] - flow[:, :, :, :-1])
        if self.edge_aware:
            return (torch.mean(weight_gh * flow_gh) + torch.mean(weight_gw * flow_gw)) * 0.5
        else:
            return (torch.mean(flow_gh) + torch.mean(flow_gw)) * 0.5


def get_batch_images(args, save_img_num, save_images):  ## For visualization during training phase
    width_num = len(save_images)
    log_img = np.zeros((save_img_num * args.patch_size, width_num * args.patch_size, 3), dtype=np.uint8)
    pred_frameT, pred_coarse_flow, pred_fine_flow, frameT, simple_mean, occ_map = save_images
    for b in range(save_img_num):
        output_img_tmp = denorm255(pred_frameT[b, :])
        output_coarse_flow_tmp = pred_coarse_flow[b, :2, :, :]
        output_fine_flow_tmp = pred_fine_flow[b, :2, :, :]
        gt_img_tmp = denorm255(frameT[b, :])
        simple_mean_img_tmp = denorm255(simple_mean[b, :])
        occ_map_tmp = occ_map[b, :]

        output_img_tmp = np.transpose(output_img_tmp.detach().cpu().numpy(), [1, 2, 0]).astype(np.uint8)
        output_coarse_flow_tmp = flow2img(np.transpose(output_coarse_flow_tmp.detach().cpu().numpy(), [1, 2, 0]))
        output_fine_flow_tmp = flow2img(np.transpose(output_fine_flow_tmp.detach().cpu().numpy(), [1, 2, 0]))
        gt_img_tmp = np.transpose(gt_img_tmp.detach().cpu().numpy(), [1, 2, 0]).astype(np.uint8)
        simple_mean_img_tmp = np.transpose(simple_mean_img_tmp.detach().cpu().numpy(), [1, 2, 0]).astype(np.uint8)
        occ_map_tmp = np.transpose(occ_map_tmp.detach().cpu().numpy() * 255.0, [1, 2, 0]).astype(np.uint8)
        occ_map_tmp = np.concatenate([occ_map_tmp, occ_map_tmp, occ_map_tmp], axis=2)

        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 0 * args.patch_size:1 * args.patch_size,
        :] = simple_mean_img_tmp
        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 1 * args.patch_size:2 * args.patch_size,
        :] = output_img_tmp
        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 2 * args.patch_size:3 * args.patch_size,
        :] = gt_img_tmp
        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 3 * args.patch_size:4 * args.patch_size,
        :] = output_coarse_flow_tmp
        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 4 * args.patch_size:5 * args.patch_size,
        :] = output_fine_flow_tmp
        log_img[(b) * args.patch_size:(b + 1) * args.patch_size, 5 * args.patch_size:6 * args.patch_size,
        :] = occ_map_tmp

    return log_img


def flow2img(flow, logscale=True, scaledown=6, output=False):
    """
    topleft is zero, u is horiz, v is vertical
    red is 3 o'clock, yellow is 6, light blue is 9, blue/purple is 12
    """
    u = flow[:, :, 1]
    # u = flow[:, :, 0]
    v = flow[:, :, 0]
    # v = flow[:, :, 1]

    colorwheel = makecolorwheel()
    ncols = colorwheel.shape[0]

    radius = np.sqrt(u ** 2 + v ** 2)
    if output:
        print("Maximum flow magnitude: %04f" % np.max(radius))
    if logscale:
        radius = np.log(radius + 1)
        if output:
            print("Maximum flow magnitude (after log): %0.4f" % np.max(radius))
    radius = radius / scaledown
    if output:
        print("Maximum flow magnitude (after scaledown): %0.4f" % np.max(radius))
    # rot = np.arctan2(-v, -u) / np.pi
    rot = np.arctan2(v, u) / np.pi

    fk = (rot + 1) / 2 * (ncols - 1)  # -1~1 maped to 0~ncols
    k0 = fk.astype(np.uint8)  # 0, 1, 2, ..., ncols

    k1 = k0 + 1
    k1[k1 == ncols] = 0

    f = fk - k0

    ncolors = colorwheel.shape[1]
    img = np.zeros(u.shape + (ncolors,))
    for i in range(ncolors):
        tmp = colorwheel[:, i]
        col0 = tmp[k0]
        col1 = tmp[k1]
        col = (1 - f) * col0 + f * col1

        idx = radius <= 1
        # increase saturation with radius
        col[idx] = 1 - radius[idx] * (1 - col[idx])
        # out of range
        col[~idx] *= 0.75
        # img[:,:,i] = np.floor(255*col).astype(np.uint8)

        img[:, :, i] = np.clip(255 * col, 0.0, 255.0).astype(np.uint8)

    # return img.astype(np.uint8)
    return img


def makecolorwheel():
    # Create a colorwheel for visualization
    RY = 15
    YG = 6
    GC = 4
    CB = 11
    BM = 13
    MR = 6

    ncols = RY + YG + GC + CB + BM + MR

    colorwheel = np.zeros((ncols, 3))

    col = 0
    # RY
    colorwheel[col:col + RY, 0] = 1
    colorwheel[col:col + RY, 1] = np.arange(0, 1, 1. / RY)
    col += RY

    # YG
    colorwheel[col:col + YG, 0] = np.arange(1, 0, -1. / YG)
    colorwheel[col:col + YG, 1] = 1
    col += YG

    # GC
    colorwheel[col:col + GC, 1] = 1
    colorwheel[col:col + GC, 2] = np.arange(0, 1, 1. / GC)
    col += GC

    # CB
    colorwheel[col:col + CB, 1] = np.arange(1, 0, -1. / CB)
    colorwheel[col:col + CB, 2] = 1
    col += CB

    # BM
    colorwheel[col:col + BM, 2] = 1
    colorwheel[col:col + BM, 0] = np.arange(0, 1, 1. / BM)
    col += BM

    # MR
    colorwheel[col:col + MR, 2] = np.arange(1, 0, -1. / MR)
    colorwheel[col:col + MR, 0] = 1

    return colorwheel
