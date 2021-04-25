#pragma once
template<class T> struct wdebug {};


template<typename T>
wstring toString(T s);


template<> struct wdebug<WaveMethod*>
{
    wstring static toString(WaveMethod* f);
};
template<> struct wdebug<WaveField*>
{
    wstring static toString(WaveField* f);
};
template<> struct wdebug<WaveClass*>
{
    wstring static toString(WaveClass* f);
};