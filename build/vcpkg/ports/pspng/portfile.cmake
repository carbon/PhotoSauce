set(LIBPNG_APNG_PATCH_PATH "")
if ("apng" IN_LIST FEATURES)
    if(VCPKG_HOST_IS_WINDOWS)
        vcpkg_acquire_msys(MSYS_ROOT PACKAGES gawk gzip NO_DEFAULT_PACKAGES)
        vcpkg_add_to_path("${MSYS_ROOT}/usr/bin")
    endif()

    set(LIBPNG_APNG_PATCH_NAME "libpng-${VERSION}-apng.patch")
    vcpkg_download_distfile(LIBPNG_APNG_PATCH_ARCHIVE
        URLS "https://downloads.sourceforge.net/project/libpng-apng/libpng16/${VERSION}/${LIBPNG_APNG_PATCH_NAME}.gz"
        FILENAME "${LIBPNG_APNG_PATCH_NAME}.gz"
        SHA512 2389BC36FA4FB01A668882A07FD0CD0BE07D1A50D00F5DA342DB55DA8686C178F02B953BD4E3F6CCFA3FEB7997768E92047F0110B8FE3B7E4D1A6D3F81806346
    )
    set(LIBPNG_APNG_PATCH_PATH "${CURRENT_BUILDTREES_DIR}/src/${LIBPNG_APNG_PATCH_NAME}")
    if (NOT EXISTS "${LIBPNG_APNG_PATCH_PATH}")
        file(INSTALL "${LIBPNG_APNG_PATCH_ARCHIVE}" DESTINATION "${CURRENT_BUILDTREES_DIR}/src")
        vcpkg_execute_required_process(
            COMMAND gzip -d "${LIBPNG_APNG_PATCH_NAME}.gz"
            WORKING_DIRECTORY "${CURRENT_BUILDTREES_DIR}/src"
            ALLOW_IN_DOWNLOAD_MODE
            LOGNAME extract-patch.log
        )
    endif()
endif()

vcpkg_from_github(
    OUT_SOURCE_PATH SOURCE_PATH
    REPO pnggroup/libpng
    REF v${VERSION}
    SHA512 65F54D805E1F7C46A5FC335B984E4CBD4F934E0F02FBF6673C13800B49A4C11FBEB4098EEBFB33079527A56C3D933E97631F91AB68DBB31442982784F9241ACE
    HEAD_REF libpng16
    PATCHES
        "${LIBPNG_APNG_PATCH_PATH}"
        pspng-customize-build.patch
        pspng-customize-code.patch
)

file(COPY "${CURRENT_PORT_DIR}/pngusr.h" "${CURRENT_PORT_DIR}/pspng.h"
          "${CURRENT_PORT_DIR}/pspng.c" "${CURRENT_PORT_DIR}/pspng.ver" DESTINATION "${SOURCE_PATH}")

set(VCPKG_C_FLAGS -DPNG_USER_CONFIG)
set(VCPKG_CXX_FLAGS -DPNG_USER_CONFIG)

vcpkg_cmake_configure(
    SOURCE_PATH "${SOURCE_PATH}"
    OPTIONS
        -DPNG_STATIC=OFF
        -DPNG_SHARED=OFF
        -DPNG_FRAMEWORK=OFF
        -DPNG_TESTS=OFF
        -DSKIP_INSTALL_ALL=ON
    MAYBE_UNUSED_VARIABLES
        PNG_ARM_NEON
)
vcpkg_cmake_install()
vcpkg_copy_pdbs()

file(REMOVE_RECURSE "${CURRENT_PACKAGES_DIR}/debug/share"
                    "${CURRENT_PACKAGES_DIR}/debug/include"
)
vcpkg_install_copyright(FILE_LIST "${SOURCE_PATH}/LICENSE")
