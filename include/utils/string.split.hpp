#pragma once
#include <sstream>
#include <vector>

vector<wstring> split (const wstring &s, wchar_t delim) {
    vector<wstring> result;
    wstringstream ss (s);
    wstring item;

    while (getline (ss, item, delim))
        result.push_back (item);

    return result;
}
