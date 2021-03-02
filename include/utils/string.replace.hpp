#pragma once
#include <string>
using namespace std;
wstring replace_string(wstring str, const wstring& from, const wstring& to) {
    size_t start_pos = 0;
    while((start_pos = str.find(from, start_pos)) != std::string::npos) {
        str.replace(start_pos, from.length(), to);
        start_pos += to.length(); 
    }
    return str;
}