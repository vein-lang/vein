#pragma once

#include <string>
using namespace std;

enum class CompareFlag
{
    NONE,
    CaseInsensitive
};

bool equals(const string& str1, const string& str2, CompareFlag flag)
{
    if (str1.size() != str2.size())
        return false;
    if (flag == CompareFlag::CaseInsensitive)
        return std::equal(str1.begin(), str1.end(), str2.begin(),
                          [](auto a, auto b) { return std::tolower(a) == std::tolower(b); });
     return std::equal(str1.begin(), str1.end(), str2.begin());
}

bool equals(const wstring& str1, const wstring& str2, CompareFlag flag)
{
    if (str1.size() != str2.size())
        return false;
    if (flag == CompareFlag::CaseInsensitive)
        return std::equal(str1.begin(), str1.end(), str2.begin(),
                          [](auto a, auto b) { return std::tolower(a) == std::tolower(b); });
     return std::equal(str1.begin(), str1.end(), str2.begin());
}