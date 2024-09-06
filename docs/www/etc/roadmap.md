---
description: roadmap
---



# Road to 1.0 version


## System

- stackalloc (predefined stack allocation)
- stacksize auto calc
- call conv ishtar
- NAPI (Ishtar API)
- AVX, etc
- Natural Structs


## Network

- PAL for sockets (win, unix)


## Primitives

- Vectors, Matrix, Bounds, AABB, Raycasts
- Int128, UInt128, Int256, UInt256, Int512, UInt512
- Float128, Float256
- QBit, QResult, QMatrix, QVector

# SugarDaddy (Language features)

- primary constructors
- disposable pattern (defer deconstructors)
- try pattern
--
try(methodName()) {
    result => ...,
    err => ...,
}
-- 
- support transformer 
- object pattern marching

# Quantum

- QASM Emitter
