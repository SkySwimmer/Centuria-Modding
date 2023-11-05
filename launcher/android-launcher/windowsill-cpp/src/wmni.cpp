#include "org_asf_windowsill_WMNI.h"
#include "mono/utils/mono-publib.h"
#include "mono/utils/mono-logger.h"
#include "mono/metadata/appdomain.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/class.h"
#include "mono/metadata/mono-debug.h"
#include "mono/metadata/mono-gc.h"
#include "mono/metadata/exception.h"
#include "mono/metadata/object.h"
#include "mono/jit/jit.h"
#include "mono/jit/mono-private-unstable.h"
#include <string.h>
#include <dlfcn.h>
#include <unistd.h>

// Type defs
typedef MonoDomain* mono_jit_init_ptr(const char *file);
typedef void mono_set_dirs_ptr (const char *assembly_dir, const char *config_dir);
typedef void mono_set_assemblies_path_ptr (const char *path);

// Methods
JNIEXPORT jlong JNICALL Java_org_asf_windowsill_WMNI_loadMonoLib (JNIEnv* env, jclass, jstring path) {
	const char* pth = env->GetStringUTFChars(path, NULL);

	// Load
	void *monoLib = dlopen(pth, RTLD_NOW | RTLD_LOCAL);

	// Return mono pointer
	return (long) monoLib;
}

JNIEXPORT jlong JNICALL Java_org_asf_windowsill_WMNI_initRuntime (JNIEnv* env, jclass, jlong mono, jstring name, jstring root, jstring libsDirJ, jstring etcDirJ) {
	// Load parameters
	const char* domainName = env->GetStringUTFChars(name, NULL);
	const char* libsDir = env->GetStringUTFChars(libsDirJ, NULL);
	const char* etcDir = env->GetStringUTFChars(etcDirJ, NULL);
	const char* rootDir = env->GetStringUTFChars(root, NULL);
	void *monoLib = (void *)mono;

	// Setup calls
	mono_jit_init_ptr* mono_jit_init = (mono_jit_init_ptr*)(dlsym(monoLib, "mono_jit_init"));
	mono_set_dirs_ptr* mono_set_dirs = (mono_set_dirs_ptr*)(dlsym(monoLib, "mono_set_dirs"));
	mono_set_assemblies_path_ptr* mono_set_assemblies_path = (mono_set_assemblies_path_ptr*)(dlsym(monoLib, "mono_set_dirs"));

	// Set directories
	chdir(rootDir);
	mono_set_assemblies_path(libsDir);
	mono_set_dirs(libsDir, etcDir);

	// Create domain
	MonoDomain *domain = mono_jit_init(domainName);

	// Return
	return (long) domain;
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
