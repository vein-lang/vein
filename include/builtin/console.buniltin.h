static WaveObject* i_call_console_println(WaveObject* o)
{
    auto* str = WaveConvert<WaveString>::unbox(o);
    w_print(str);
    return nullptr;
}
static WaveObject* i_call_console_print(WaveObject* o)
{
    auto* str = WaveConvert<WaveString>::unbox(o);
    d_print(str);
    return nullptr;
}
