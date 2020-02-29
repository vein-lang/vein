#include <Arduino.h>
#pragma message "Arduino Wave bootloader"

bool ready = false;
void setup()
{
    pinMode(13, OUTPUT);
    Serial.begin(9600);
    Serial.setTimeout(150000);
}
String readBlock()
{
    String timeOutResult;
    auto result = Serial.readStringUntil('\n');
    if(timeOutResult == result)
        return readBlock();
    return result;
}
void log(String s)
{
    Serial.println("<WAVE_BOOT>: " + s);
}
void handleFirmware()
{
    log("start download firmware...");
    auto bl = readBlock();
    log(bl);
    if(!(bl == F("~#UP"))) 
    {
        ready = true;
        return;
    }
    if (serialEventRun) serialEventRun();
    float* firmwareBlocks = nullptr;
    int index = 0;
    for (;;)
    {
        auto block = readBlock();
        log("readed block: " + block);
        if(block == F("~#DOWN")) 
            break;
        if(block == F("~#SIZE"))
        {
            block = readBlock();
            auto size = 0;
            size = block.toInt();
            log("size: " + String(size));
            firmwareBlocks = (float*)new float[size];
            continue;
        }
        log("result: " + String(block.toFloat()));
        firmwareBlocks[index++] = block.toFloat();
    }
    ready = true;
}

void loop()
{

    for(int i = 255; i >= 0; i--) 
    {
        analogWrite(13, i);
        delay(10);
    }
    for(int i = 0; i <= 255; i++) 
    {
        analogWrite(13, i);
        delay(10);
    }
    //asm volatile("nop\n\t");
    if(!ready)
        handleFirmware();
}
/*
#include <Arduino.h>
#pragma message "Arduino Wave bootloader"
//#include "headers/str.h"
//#include "headers/version.h"
#ifdef UART
#undef UART
#endif
auto UART = Serial;

//using namespace str;

//string s;



*/