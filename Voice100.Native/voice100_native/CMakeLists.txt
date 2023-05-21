project(voice100_native)

set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN 1)

include_directories(${WORLD_INC})

add_library(
    voice100_native
    SHARED
    vocoder.cpp)

if(WIN32)    
    target_link_libraries(voice100_native ${WORLD_LIB}/world.lib)
else(WIN32)
    target_link_libraries(voice100_native ${WORLD_LIB}/libworld.a)
endif(WIN32)

install(TARGETS voice100_native
        LIBRARY DESTINATION)
