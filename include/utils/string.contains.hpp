#pragma once
#include <string>

using namespace std;

inline bool contains(string& s1, string& s2)
{
    return s1.find(s2) != string::npos;
}