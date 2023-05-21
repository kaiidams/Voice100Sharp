project("webrtc_vad")

set(CMAKE_CXX_VISIBILITY_PRESET hidden)
set(CMAKE_VISIBILITY_INLINES_HIDDEN 1)

include_directories(".")

add_library(
    webrtc_vad
    SHARED
    common_audio/vad/vad_core.c
    common_audio/vad/vad_filterbank.c
    common_audio/vad/vad_filterbank.h
    common_audio/vad/vad_gmm.c
    common_audio/vad/vad_sp.c
    common_audio/vad/webrtc_vad.c
    common_audio/signal_processing/division_operations.c 
    common_audio/signal_processing/energy.c
    common_audio/signal_processing/get_scaling_square.c 
    common_audio/signal_processing/resample_48khz.c
    common_audio/signal_processing/resample_by_2_internal.c
    common_audio/signal_processing/resample_fractional.c
    common_audio/signal_processing/spl_inl.c
)

install(TARGETS webrtc_vad
        LIBRARY DESTINATION)
