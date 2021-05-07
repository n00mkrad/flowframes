# Installation Of Python And Dependencies 

- Make sure your GPU drivers are up to date
- Install Python 3.8.6 from the [official website](https://www.python.org/downloads/release/python-386/)
- In CMD, run `pip install torch==1.8.1+cu111 torchvision==0.9.1+cu111 -f https://download.pytorch.org/whl/torch_stable.html`
- Run `pip install opencv-python sk-video imageio`



This should be sufficient to run RIFE and other Pytorch-based networks on any modern GPU, including the RTX 3000 (Ampere) series.

## Troubleshooting

If you are getting some kind of numpy error, try downgrading it to 1.19.3:

`pip install numpy==1.19.3`