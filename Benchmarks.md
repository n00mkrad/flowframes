

# Flowframes Benchmarks

Here you can find Flowframes benchmarks (mostly RIFE) that you can use as a performance reference.

To avoid clutter, only benchmarks at 1080p/720p and 2x interpolation factor are listed.

In "Resolution/Factor", enter your input resolution and the interpolation factor. For "Drive", enter the storage type where your temp folder is stored (temp folder location can be set in the Settings, is in the same directory as your output by default). An SSD is recommended for benchmarking as HDDs can slow things down.

Sample size means how many frames have been interpolated at the time you measured the speed. The higher, the more accurate. In the last column, enter your FPS (Out).

## RIFE (CUDA)

| GPU                           | Ver  | Size/Factor    | Drive    | Sample Size | Speed (FPS Out) |
| ----------------------------- | ---- | -------------- | -------- | ----------- | --------------- |
| RTX 2070 SUPER 8 GB           | 1.18 | 1920x1080 - 2x | NVME SSD | \>2000      | 14 FPS          |
| RTX 2070 SUPER 8 GB           | 1.18 | 1280x720p - 2x | NVME SSD | \>14000     | 25.5 FPS        |
| Quadro P5000 16 GB            | 1.18 | 1920x1080 - 2x | SAN/SSHD | 1800        | 10.8 FPS        |
| Quadro P5000 16 GB            | 1.18 | 1280x720p - 2x | SAN/SSHD | 1800        | 20.2 FPS        |
| GTX 1080 Ti 11 GB             | 1.18 | 1920x1080 - 2x | NVME SSD | >1400       | 12.2 FPS        |
| GTX 1080 Ti 11 GB             | 1.18 | 1280x720p - 2x | NVME SSD | >1400       | 22.8 FPS        |
| RTX 3070 Ti 8 GB [cuDNN 8.05] | 1.19 | 1920x1080 - 2x | NVME SSD | >1400       | 16 FPS          |
| RTX 3070 Ti 8 GB [cuDNN 8.05] | 1.19 | 1280x720p - 2x | NVME SSD | >1400       | 33 FPS          |

## RIFE-NCNN

AMD:

| GPU                      | Size/Factor    | Drive    | Sample Size | Speed (FPS Out) |
| ------------------------ | -------------- | -------- | ----------- | --------------- |
| Ryzen 4800U (Vega 8) 25W | 1920x1080 - 2x | NVME SSD | \>75        | 0.85 FPS        |
| Ryzen 4800U (Vega 8) 25W | 1280x720p - 2x | NVME SSD | \>100       | 1.9 FPS         |
| RX 5700 XT 8 GB          | 1920x1080 - 2x | NVME SSD | \>2000      | 8 FPS           |
| RX 5700 XT 8 GB          | 1280x720p - 2x | NVME SSD | \>2000      | 15 FPS          |
| RX 6900 XT 16 GB         | 1920x1080 - 2x | NVME SSD | >1400       | 10.5 FPS        |
| RX 6900 XT 16 GB         | 1280x720p - 2x | NVME SSD | >1400       | 21.4 FPS        |

Nvidia:

| GPU                 | Size/Factor    | Drive    | Sample Size | Speed (FPS Out) |
| ------------------- | -------------- | -------- | ----------- | --------------- |
| RTX 2070 SUPER 8 GB | 1920x1080 - 2x | NVME SSD | \>1000      | 5.4 FPS         |
| RTX 2070 SUPER 8 GB | 1280x720p - 2x | NVME SSD | \>1000      | 12 FPS          |
| RTX 3070 8 GB       | 1920x1080 - 2x | NVME SSD | >1400       | 6.4 FPS         |
| RTX 3070 8 GB       | 1280x720p - 2x | NVME SSD | >1400       | 14 FPS          |