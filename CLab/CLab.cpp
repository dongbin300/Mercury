#include <stdio.h>
#include <time.h>

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
	for (int i = 0; i < arraySize; i++) {
		r[i] = 1;
		for (int j = 1; j <= a[i]; j++)
		{
			r[i] += (j % 2 == 0 ? -1 : 1) * j % 2 + 1;
		}
	}
	time_t end_time = clock();

	// Output
	/*for (int i = 0; i < arraySize; i++) {
		printf("%d ", r[i]);
	}*/
	double elapsed_time = ((double)(end_time - start_time)) / CLOCKS_PER_SEC;
	printf("%.3f 초\n", elapsed_time);

	return 0;
}