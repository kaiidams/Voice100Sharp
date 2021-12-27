cmake_minimum_required(VERSION 3.6.0)
project(voice100_native)

set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN 1)

include_directories(${WORLD_INC})

add_library(
    voice100_native
    SHARED
    vocoder.cpp)

target_link_libraries(voice100_native ${WORLD_LIB}/Release/world.lib)
