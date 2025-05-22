#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <cuda_runtime.h>

#pragma pack(push, 1)
typedef struct {
    unsigned short type;
    unsigned int size;
    unsigned short reserved1;
    unsigned short reserved2;
    unsigned int offset;
} BMPHeader;

typedef struct {
    unsigned int size;
    int width;
    int height;
    unsigned short planes;
    unsigned short bitCount;
    unsigned int compression;
    unsigned int sizeImage;
    int xPelsPerMeter;
    int yPelsPerMeter;
    unsigned int clrUsed;
    unsigned int clrImportant;
} BMPInfoHeader;
#pragma pack(pop)

// CUDA kernel: invert pixel colors
__global__ void invertKernel(unsigned char* data, int dataSize) {
    int i = blockIdx.x * blockDim.x + threadIdx.x;
    if (i < dataSize) {
        data[i] = 255 - data[i];
    }
}

extern "C" __declspec(dllexport)
void InvertImage(const char* inputPath, const char* outputPath)
{
    FILE* fp = fopen(inputPath, "rb");
    if (!fp) {
        printf("Nie można otworzyć pliku wejściowego: %s\n", inputPath);
        return;
    }

    BMPHeader header;
    BMPInfoHeader info;
    fread(&header, sizeof(BMPHeader), 1, fp);
    fread(&info, sizeof(BMPInfoHeader), 1, fp);

    if (info.bitCount != 24) {
        printf("Obsługiwane są tylko pliki BMP 24-bitowe!\n");
        fclose(fp);
        return;
    }

    int imageSize = info.width * info.height * 3;
    unsigned char* img = (unsigned char*)malloc(imageSize);
    fseek(fp, header.offset, SEEK_SET);
    fread(img, 1, imageSize, fp);
    fclose(fp);

    // CUDA memory and execution
    unsigned char* d_img;
    cudaMalloc(&d_img, imageSize);
    cudaMemcpy(d_img, img, imageSize, cudaMemcpyHostToDevice);

    int threads = 256;
    int blocks = (imageSize + threads - 1) / threads;
    invertKernel << <blocks, threads >> > (d_img, imageSize);
    cudaDeviceSynchronize();

    cudaMemcpy(img, d_img, imageSize, cudaMemcpyDeviceToHost);
    cudaFree(d_img);

    // Zapis wyniku
    FILE* out = fopen(outputPath, "wb");
    if (!out) {
        printf("Nie można otworzyć pliku wyjściowego: %s\n", outputPath);
        free(img);
        return;
    }

    fwrite(&header, sizeof(BMPHeader), 1, out);
    fwrite(&info, sizeof(BMPInfoHeader), 1, out);
    fseek(out, header.offset, SEEK_SET);
    fwrite(img, 1, imageSize, out);
    fclose(out);

    free(img);
    printf("Zapisano: %s\n", outputPath);
}
