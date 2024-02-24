#pragma once

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>

struct quote
{
	char time[30];
	double o;
	double h;
	double l;
	double c;
	double v;
};

struct month_data {
	int year;
	int month;
	double o_total;
	double h_max;
	double l_min;
	double c_last;
	double v_total;
};

struct year_data {
	struct month_data months[12];
};

void read_data_1M(char* symbol)
{
	FILE* file;
	char line[100];
	char path[100];
	struct quote data[2000];
	struct year_data years[4] = { 0 };
	int data_i = 0;

	sprintf(path, "C:/Users/Gaten/AppData/Roaming/Gaten/BinanceFuturesData/1D/%s.csv", symbol);
	file = fopen(path, "r");

    while (fgets(line, sizeof(line), file) != NULL) {
        char* token = strtok(line, ",");
        strcpy(data[data_i].time, token);

        // �ð�, �ְ�, ������, ����, �ŷ��� �Ľ�
        double o = atof(strtok(NULL, ","));
        double h = atof(strtok(NULL, ","));
        double l = atof(strtok(NULL, ","));
        double c = atof(strtok(NULL, ","));
        double v = atof(strtok(NULL, ","));

        // �ð� ������ �����Ͽ� �⵵�� ���� ����
        int year, month, day;
        sscanf(data[data_i].time, "%d-%d-%d", &year, &month, &day);

        // �ش� �⵵�� �����Ϳ� ���� �����͸� ����
        years[year - 2020].months[month - 1].year = year;
        years[year - 2020].months[month - 1].month = month;
        years[year - 2020].months[month - 1].o_total += o;
        if (h > years[year - 2020].months[month - 1].h_max) years[year - 2020].months[month - 1].h_max = h;
        if (l < years[year - 2020].months[month - 1].l_min || years[year - 2020].months[month - 1].l_min == 0) years[year - 2020].months[month - 1].l_min = l;
        years[year - 2020].months[month - 1].c_last = c;
        years[year - 2020].months[month - 1].v_total += v;

        data_i++;
    }

    for (int i = 0; i < 4; i++) {
        for (int j = 0; j < 12; j++) {
            if (years[i].months[j].year != 0) {
                strcpy(data[data_i].time, "");
                sprintf(data[data_i].time, "%d-%02d", years[i].months[j].year, years[i].months[j].month);
                data[data_i].o = years[i].months[j].o_total;
                data[data_i].h = years[i].months[j].h_max;
                data[data_i].l = years[i].months[j].l_min;
                data[data_i].c = years[i].months[j].c_last;
                data[data_i].v = years[i].months[j].v_total;
                printf("%s, %.4f, %.4f, %.4f, %.4f, %.4f\n",
                    data[data_i].time, data[data_i].o, data[data_i].h, data[data_i].l, data[data_i].c, data[data_i].v);
                data_i++;
            }
        }
    }

	fclose(file);
}