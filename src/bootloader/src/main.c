#include <avr/io.h>
#include <avr/pgmspace.h>
#include <avr/eeprom.h>
#pragma message "Arduino Wave bootloader"

#define WAVE_VER_MAJOR 1
#define WAVE_VER_MINOR 1
#define MAKESTR(a) #a
#define MAKEVER(a, b) MAKESTR(a*256+b)

asm("  .section .version\n"
        "wave_boot_version:  .word " MAKEVER(WAVE_VER_MAJOR, WAVE_VER_MINOR) "\n"
        "  .section .text\n");

#define LED_DDR  DDRB
#define LED      PINB5



void (*app_start)(void) = 0x0000;

int main(void)
{
    asm volatile("nop\n\t");
    //sbi(LED_DDR, LED);
}