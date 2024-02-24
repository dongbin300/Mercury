#include "cuda_runtime.h"
#include "device_launch_parameters.h"

#include "cryptodata.h"

void UseCuda(int*, const int*, unsigned int, unsigned int);

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
	read_data_1M("SOLUSDT");

	const int arraySize = 256;
	int a[arraySize];
	int r[arraySize] = { 0 };

	// Input
	for (int i = 0; i < arraySize; i++) {
		a[i] = i + 1;
	}

	// Compute
	UseCuda(r, a, 16, 16);

	// Output
	for (int i = 0; i < arraySize; i++) {
		printf("%d ", r[i]);
	}

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
	size_t size = b_size * t_size * sizeof(int);

	cudaSetDevice(0);
	cudaMalloc((void**)&d_r, size);
	cudaMalloc((void**)&d_a, size);
	cudaMemcpy(d_a, a, size, cudaMemcpyHostToDevice);

	FactorialKernel << <b_size, t_size >> > (d_r, d_a);

	cudaGetLastError();
	cudaDeviceSynchronize();
	cudaMemcpy(r, d_r, size, cudaMemcpyDeviceToHost);
	cudaFree(d_r);
	cudaFree(d_a);
}
