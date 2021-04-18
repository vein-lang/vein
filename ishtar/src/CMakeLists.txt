cmake_minimum_required (VERSION 3.8)

add_executable (ishtar "entry.cpp")
target_compile_features(ishtar PRIVATE cxx_std_20)
TARGET_LINK_LIBRARIES(ishtar PUBLIC fmt::fmt-header-only)
TARGET_LINK_LIBRARIES(ishtar INTERFACE fmt::fmt-header-only)