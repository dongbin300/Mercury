#include "cuda_runtime.h"
#include "device_launch_parameters.h"

#include <stdio.h>
#include <time.h>

void UseCuda(int*, const int*, unsigned int, unsigned int);

__global__ void addKernel(int* c, const int* a, const int* b)
{
	int i = threadIdx.x;
	c[i] = a[i] * b[i];
}

__global__ void FactorialKernel(int* result, const int* a)
{
	int i = blockIdx.x * blockDim.x + threadIdx.x;

	result[i] = 1;
	for (int j = 1; j <= a[i]; j++)
	{
		result[i] += (j % 2 == 0 ? -1 : 1) * j % 2 + 1;
	}
}

extern "C" __declspec(dllexport) void factorial(int n)
{

}

int main()
{
	const int arraySize = 65536;
	int a[arraySize];
	int r[arraySize] = { 0 };

	// Input
	for (int i = 0; i < arraySize; i++) {
		a[i] = i + 1;
	}

	time_t start_time = clock();
	// Compute
	UseCuda(r, a, 256, 256);
	time_t end_time = clock();

	// Output
	/*for (int i = 0; i < arraySize; i++) {
		printf("%d ", r[i]);
	}*/
	double elapsed_time = ((double)(end_time - start_time)) / CLOCKS_PER_SEC;
	printf("%.3f 초\n", elapsed_time);

	// Free
	cudaDeviceReset();
	return 0;
}

/// <summary>
/// block 1개일때 thread max 1024
/// block 256개일때 thread max 256 (=65536)
/// </summary>
/// <param name="c"></param>
/// <param name="a"></param>
/// <param name="size"></param>
/// <returns></returns>
void UseCuda(int* r, const int* a, unsigned int b_size, unsigned int t_size)
{
	int* d_a = 0;
	int* d_r = 0;

	cudaSetDevice(0);
	cudaMalloc((void**)&d_r, b_size * t_size * sizeof(int));
	cudaMalloc((void**)&d_a, b_size * t_size * sizeof(int));
	cudaMemcpy(d_a, a, b_size * t_size * sizeof(int), cudaMemcpyHostToDevice);

	FactorialKernel << <b_size, t_size >> > (d_r, d_a);

	cudaGetLastError();
	cudaDeviceSynchronize();
	cudaMemcpy(r, d_r, b_size * t_size * sizeof(int), cudaMemcpyDeviceToHost);

	cudaFree(d_r);
	cudaFree(d_a);
}
