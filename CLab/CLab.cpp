#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define MAX_LINE_LENGTH 100
#define MAX_DATA_ENTRIES 3000

// 구조체 정의
typedef struct {
    char date[MAX_LINE_LENGTH];
    double open;
    double high;
    double low;
    double close;
    double volume;
} DataEntry;

int read_csv(const char* filename, const char* start_date, const char* end_date, DataEntry* data);

int main()
{
    DataEntry data[MAX_DATA_ENTRIES];
    int num_entries = read_csv("C:\\Users\\Gaten\\AppData\\Roaming\\Gaten\\BinanceFuturesData\\5m\\AAVEUSDT.csv", 
        "2020-10-16 08:00:00", "2020-10-16 10:00:00", data);
    return 0;
}

int read_csv(const char* filename, const char* start_date, const char* end_date, DataEntry* data) {
    FILE* file;
    fopen_s(&file, filename, "r");
    if (file == NULL) {
        printf("Error opening file.\n");
        return -1;
    }

    char line[MAX_LINE_LENGTH];
    int num_entries = 0;

    while (fgets(line, MAX_LINE_LENGTH, file) != NULL && num_entries < MAX_DATA_ENTRIES) {
        char* token;
        char* next_token;
        token = strtok_s(line, ",", &next_token);
        strcpy_s(data[num_entries].date, sizeof(data[num_entries].date), token); // 날짜
        if (strcmp(data[num_entries].date, start_date) >= 0 && strcmp(data[num_entries].date, end_date) <= 0) {
            token = strtok_s(NULL, ",", &next_token);
            data[num_entries].open = atof(token); // 시가
            token = strtok_s(NULL, ",", &next_token);
            data[num_entries].high = atof(token); // 고가
            token = strtok_s(NULL, ",", &next_token);
            data[num_entries].low = atof(token); // 저가
            token = strtok_s(NULL, ",", &next_token);
            data[num_entries].close = atof(token); // 종가
            token = strtok_s(NULL, ",", &next_token);
            data[num_entries].volume = atof(token); // 거래량
            num_entries++;
        }
    }

    fclose(file);
    return num_entries; // 읽은 데이터의 개수 반환
}