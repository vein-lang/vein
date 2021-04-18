#pragma once


enum class WaveMemberKind
{
    KindCtor    = 1 << 1,
    KindDtor    = 1 << 2,
    Field       = 1 << 3,
    Method      = 1 << 4,
    Type        = 1 << 5
};