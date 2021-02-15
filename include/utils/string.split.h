#pragma once
#include <iostream>
#include <sstream>
#include <vector>

vector<string> split (const string &s, char delim) {
    vector<string> result;
    stringstream ss (s);
    string item;

    while (getline (ss, item, delim))
        result.push_back (item);

    return result;
}
