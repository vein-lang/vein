#pragma once
#ifndef __TPUART_H__
#define __TPUART_H__
#include "board.h"
#define TPUART_TIMEOUT 160000
class TPUART
{
public:
    TPUART(USARTClass *serial)
    {
        this->_serial = serial;
        if (this->_serial->getTimeout() < TPUART_TIMEOUT)
            this->_serial->setTimeout(TPUART_TIMEOUT);
    }

    String readBlock()
    {
        auto result = _serial->readStringUntil(this->terminatorChar);
        if (this->timeoutResult == result)
            return this->readBlock();
        return result;
    }

protected:
    const char terminatorChar = '\n';
    const String timeoutResult;
    USARTClass *_serial;
};

#endif