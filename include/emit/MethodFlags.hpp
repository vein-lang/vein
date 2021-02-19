#pragma once

enum MethodFlags
{
        MethodPublic = 1 << 0,
        MethodStatic = 1 << 1,
        MethodInternal = 1 << 2,
        MethodProtected = 1 << 3,
        MethodPrivate = 1 << 4,
        MethodExtern = 1 << 5,
        MethodVirtual = 1 << 6
};