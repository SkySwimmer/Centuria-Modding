#include "org_asf_windowsill_WMNI.h"
#include "coreclrhost.h"
#include <string.h>
#include <dlfcn.h>

JNIEXPORT jlong JNICALL Java_org_asf_windowsill_WMNI_loadCoreCLR (JNIEnv* env, jclass, jstring path) {
	const char* pth = env->GetStringUTFChars(path, NULL);

	// Load
	void *coreclr = dlopen(pth, RTLD_NOW | RTLD_LOCAL);

	// Return CoreCLR pointer
	return (long) coreclr;
}

JNIEXPORT jstring JNICALL Java_org_asf_windowsill_WMNI_dlLoadError (JNIEnv* env, jclass) {
	return env->NewStringUTF(dlerror());
}

JNIEXPORT jlong JNICALL Java_org_asf_windowsill_WMNI_loadLibrary (JNIEnv* env, jclass, jstring path) {
	const char* pth = env->GetStringUTFChars(path, NULL);

	// Load
	void *lib = dlopen(pth, RTLD_NOW | RTLD_LOCAL);

	// Return pointer
	return (long) lib;
}

JNIEXPORT void JNICALL Java_org_asf_windowsill_WMNI_closeLibrary (JNIEnv* env, jclass, jlong ptr) {
	void *lib = (void*)ptr;
	dlclose(lib);
}
