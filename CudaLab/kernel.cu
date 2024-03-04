#include "cuda_runtime.h"
#include "device_launch_parameters.h"

#include <stdlib.h>

//int main()
//{
//	//read_data_1M("SOLUSDT");
//
//	const int arraySize = 256;
//	int a[arraySize];
//	int r[arraySize] = { 0 };
//
//	// Input
//	for (int i = 0; i < arraySize; i++) {
//		a[i] = i + 1;
//	}
//
//	// Compute
//	UseCuda(r, a, 16, 16);
//
//	// Output
//	for (int i = 0; i < arraySize; i++) {
//		printf("%d ", r[i]);
//	}
//
//	// Free
//	cudaDeviceReset();
//	return 0;
//}

__device__ double average(double* values, int count, int startIndex) {
	double sum = 0;
	for (int i = startIndex; i < startIndex + count; i++) {
		sum += values[i];
	}
	return sum / count;
}

__device__ void ema(double* result, double* values, int length, int period, int startIndex) {
	double alpha = 2.0 / (period + 1);

	for (int i = 0; i < length; i++) {
		if (i < startIndex + period - 1) {
			result[i] = 0;
			continue;
		}

		if (i == startIndex + period - 1) {
			result[i] = average(values, period, startIndex);
			continue;
		}

		result[i] = alpha * values[i] + (1 - alpha) * result[i - 1];
	}
}

__global__ void calculate_ema_kernel(double* r, double* close, int length, int period)
{
	int i = blockIdx.x * blockDim.x + threadIdx.x;

	ema(r, close, length, period, 0);
}

/// <summary>
/// block 1개일때 thread max 1024
/// block 256개일때 thread max 256 (=65536)
/// </summary>
/// <param name="c"></param>
/// <param name="a"></param>
/// <param name="size"></param>
/// <returns></returns>
extern "C" __declspec(dllexport) void use_cuda(double *r, double* close, int length, int period, unsigned int b_size, unsigned int t_size)
{
	double* d_r;
	double* d_close;

	cudaSetDevice(0);

	size_t r_size = b_size * t_size * length * sizeof(double);
	size_t close_size = b_size * t_size * length * sizeof(double);
	cudaMalloc((void**)&d_r, r_size);
	cudaMalloc((void**)&d_close, close_size);
	cudaMemcpy(d_close, close, close_size, cudaMemcpyHostToDevice);

	calculate_ema_kernel << <b_size, t_size >> > (d_r, d_close, length, period);

	cudaGetLastError();
	cudaDeviceSynchronize();

	cudaMemcpy(r, d_r, r_size, cudaMemcpyDeviceToHost);

	cudaFree(d_r);
	cudaFree(d_close);
}
