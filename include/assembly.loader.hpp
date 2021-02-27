#pragma once
#include <map>

#include "emit/WaveModue.hpp"
#include "etc/executable_path.hpp"
#include <filesystem>


#include "api/elf_reader.hpp"
#include "utils/string.eq.hpp"
using namespace std;
namespace fs = std::filesystem;

static map<wstring, WaveModule> __assembly_cache;

static string get_env(string name)
{
    char* buf = nullptr;
    size_t sz = 0;
    if (_dupenv_s(&buf, &sz, name.c_str()) == 0 && buf != nullptr)
        return buf;
    return "";
}

static const fs::directory_entry& get_file(const wstring& name, const wstring& directory) 
{
    const auto* const ext = L".wll";
    for (const auto& p : fs::recursive_directory_iterator(directory))
    {
        if (p.path().extension() == ext)
        {
            if (equals(p.path().filename().stem().wstring(),name, CompareFlag::CaseInsensitive))
                return p;
        }
    }
    return fs::directory_entry();
}
WaveModule* FindModule(const wstring& name)
{
    //auto env = get_env("WAVE_HOME");
    auto file = get_file(name, getExecutableDir());

    if (file.exists())
    {
        return readILfromElf(file.path().stem().string().c_str());
    }
}