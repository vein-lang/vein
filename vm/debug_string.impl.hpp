// fuck C++
// used only in entry.cpp with include directive
#pragma once
#include "etc/debug_string.hpp"

wstring wdebug<WaveMethod*>::toString(WaveMethod* f)
{
    return f->Name;
}
wstring wdebug<WaveField*>::toString(WaveField* f)
{
    return f->FullName->FullName;
}
wstring wdebug<WaveClass*>::toString(WaveClass* f)
{
    auto an1 = f->FullName->get_name();
    auto an2 = f->FullName->get_namespace();
    return f->FullName->get_namespace() + f->FullName->get_name();
}
template<typename T>
wstring toString(T s)
{
    return wdebug<T>::toString(s);
}

