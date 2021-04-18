#pragma once
#include <locale>

using namespace std;

const string whitespace = " \n\r\t";

inline string trimLeft(const string& s)
{
    const auto startpos = s.find_first_not_of(whitespace);
    return (startpos == string::npos) ? "" : s.substr(startpos);
}

inline string trimRight(const std::string& s)
{
    const auto endpos = s.find_last_not_of(whitespace);
    return (endpos == string::npos) ? "" : s.substr(0, endpos+1);
}

inline string trimLeft(const string& s, const string& chars)
{
    const auto startpos = s.find_first_not_of(chars);
    return (startpos == string::npos) ? "" : s.substr(startpos);
}

inline string trimRight(const string& s, const string& chars)
{
    const auto endpos = s.find_last_not_of(chars);
    return (endpos == string::npos) ? "" : s.substr(0, endpos+1);
}

inline string trim(const string& s)
{
    return trimRight(trimLeft(s));
}

inline string trim(const string& s, const string& chars)
{
    return trimRight(trimLeft(s, chars), chars);
}
