// DummyApp.cpp : Defines the entry point for the console application.
//

#include "stdafx.h";
#include <stdio.h>;
#include <iostream>;
#include <conio.h>
using namespace std;

int _tmain(int argc, _TCHAR* argv[])
{
	const wchar_t message_uni[] = L"Hello World Unicode";
	const char* message = "Hello World ASCII";
	int integer = 0;
	const int cycleLength = 10;

	for(int cycleIndex = 0; cycleIndex < cycleLength; cycleIndex++) {
		cout << "Values in cycle " << cycleIndex << " from " << 10 << ":" << endl;
		wcout << "Unicode message: " << message_uni << endl;
		cout << "ASCII message: " << message << endl;
		cout << "Current incrementing integer value: " << integer++ << endl << endl;
		system("pause");
	}
	return 0;
}