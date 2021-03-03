#pragma once

enum WaveNativeException
{
    WAVE_EXCEPTION_NONE = 0,
    WAVE_EXCEPTION_MISSING_METHOD,
    WAVE_EXCEPTION_MISSING_FIELD,
    WAVE_EXCEPTION_TYPE_LOAD,
    WAVE_EXCEPTION_MEMBER_ACCESS,
    WAVE_EXCEPTION_STATE_CORRUPT
};

static const wchar_t* wave_exception_names[] = {
    L"WAVE_EXCEPTION_NONE",
    L"WAVE_EXCEPTION_MISSING_METHOD",
    L"WAVE_EXCEPTION_MISSING_FIELD",
    L"WAVE_EXCEPTION_TYPE_LOAD",
    L"WAVE_EXCEPTION_MEMBER_ACCESS",
    L"WAVE_EXCEPTION_STATE_CORRUPT"
};

struct NativeException
{
    WaveNativeException code;
    wstring msg;
};

struct WaveKernelData
{
    NativeException* exception;
};

static inline WaveKernelData* kernel_data;


inline void init_kernel_data()
{
    if (kernel_data == nullptr)
        kernel_data = new WaveKernelData();
}

WaveKernelData* get_kernel_data()
{
    init_kernel_data();
    return kernel_data;
}



inline void set_failure(WaveNativeException exceptionCode, const wstring& msg)
{
    init_kernel_data();

    if (kernel_data->exception)
        return;
    kernel_data->exception = new NativeException {
        exceptionCode, msg
    };
}
