cmake_minimum_required (VERSION 3.8)

project ("ishtar")

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
add_compile_options(-std=c++20)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++20 -fexceptions -frtti")

include_directories("include")
add_subdirectory ("src")


add_subdirectory("external/fmt")