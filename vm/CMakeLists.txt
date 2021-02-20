cmake_minimum_required (VERSION 3.8)

add_executable (insmvm "entry.cpp")
target_compile_features(insmvm PRIVATE cxx_std_20)
TARGET_LINK_LIBRARIES(insmvm PUBLIC fmt::fmt-header-only)
TARGET_LINK_LIBRARIES(insmvm INTERFACE fmt::fmt-header-only)