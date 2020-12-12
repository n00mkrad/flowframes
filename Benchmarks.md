# Flowframes Benchmarks

## RIFE (CUDA)

| GPU                 | Resolution/Factor | Storage / Write Speed      | Sample Size (Frames) | Speed (FPS Out) |
| ------------------- | ----------------- | -------------------------- | -------------------- | --------------- |
| RTX 2070 SUPER 8 GB | 1920x1080 - 2x    | NVME SSD - 1800 MB/S       | \>2000               | 14 FPS          |
| RTX 2070 SUPER 8 GB | 1280x720p - 2x    | NVME SSD - 1800 MB/S       | \>14000              | 25.5 FPS        |
| Quadro P5000 16 GB  | 1920x1080 - 2x    | Shadow Cloud PC (200 MB/S) | 1800                 | 10.8 FPS        |
| Quadro P5000 16 GB  | 1280x720p - 2x    | Shadow Cloud PC (200 MB/S) | 1800                 | 20.2 FPS        |

## RIFE-NCNN

| GPU                      | Resolution/Factor | Storage / Write Speed | Sample Size (Frames) | Speed (FPS Out) |
| ------------------------ | ----------------- | --------------------- | -------------------- | --------------- |
| Ryzen 4800U (Vega 8) 25W | 1920x1080 - 2x    | NVME SSD - 1400 MB/S  | \>75                 | 0.85 FPS        |
| Ryzen 4800U (Vega 8) 25W | 1280x720p - 2x    | NVME SSD - 1400 MB/S  | \>100                | 1.9 FPS         |