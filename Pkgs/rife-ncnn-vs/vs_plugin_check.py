import sys, os, glob
import vapoursynth as vs
import winreg
import platform

def print_version(core):
	vs_version = core.version()
	print('#######################################')
	print(vs_version)
	
	print("Architecture", platform.architecture()[0], "-", platform.platform())
	print("Python build:", platform.python_build(), "\n")
	print('#######################################')

	
def getWindowsPluginPath():
	try:
		aReg = winreg.ConnectRegistry(None, winreg.HKEY_LOCAL_MACHINE)
		aKey = winreg.OpenKey(aReg, r"SOFTWARE\VapourSynth")
		q = winreg.QueryValueEx(aKey, 'Plugins')
		return q[0]
	except Exception as e:
		pass
		
	return None

def main(argv):
	
	if(len(sys.argv) > 1):
		path = argv[1]
	else:
		path = getWindowsPluginPath()
		if not path:
			print("\nNo VapourSynth installation found. Please specify a path to your plugins folder")
			exit("\n\rUsage: vs_plugin_check.py <path-to-vapoursynth-plugins-folder>\n\r")
		#print("\nFound the following path:", path)
	
	core = vs.core
	print_version(core)
	plugin_dir = glob.glob(path + '/*.dll')
	
	print("checking dlls in", path)
	print('#######################################\n')
	
	error = []
	error32bit = []
	errorNoEntry = []
	notice = []
	for dll in plugin_dir:
		try:
			core.std.LoadPlugin(path=dll)
		except Exception as e:
			if "already loaded" not in str(e):
				if "returned 193" in str(e):
					error32bit.append(e)
					continue
					
				#https://github.com/HomeOfVapourSynthEvolution/VapourSynth-Waifu2x-w2xc
				if "libiomp5md.dll" in str(e):
					notice.append("libiomp5md.dll is part of the Waifu2x-w2xc filter")
					continue
				if "w2xc.dll" in str(e):
					notice.append("w2xc.dll is part of the Waifu2x-w2xc filter")
					continue
				if "svml_dispmd.dll" in str(e):
					notice.append("svml_dispmd.dll is part of the Waifu2x-w2xc filter")
					continue
					
				if "cudart64_80.dll" in str(e):
					notice.append("cudart64_80.dll some dll for CUDA GPU stuff")
					continue
					
				if "libmfxsw64.dll" in str(e):
					notice.append("libmfxsw64.dll is part of the DGMVCSourceVS filter")
					continue
					
				if "libfftw3f-3.dll" in str(e):
					notice.append("libfftw3f-3.dll is a dependency by fft3dfilter or mvtools-sf")
					continue
				if "libfftw3-3.dll" in str(e):
					notice.append("libfftw3-3.dll is a dependency by fft3dfilter or mvtools-sf")
					continue
				
				if "No entry point found" in str(e):
					errorNoEntry.append(e)
					continue

				error.append(e)
	
	error_count = len(error) + len(error32bit) + len(errorNoEntry)
	if error:
		print("Errors:")
		print("-------")
		for err in error:
			print(err)
	
	if errorNoEntry:
		print()
		print("Errors: Not a VS-Plugin")
		print("-------")
		for err in errorNoEntry:
			print(err)
			
	if error32bit:
		print()
		print("Errors: incorrect bitness (32bit instead of 64bit) or corrupt file.")
		print("-------")
		for err in error32bit:
			print(err)
			
	if notice:
		print()
		print("Notices:")
		print("-------")
		for n in notice:
			print(n)
			
	if error_count == 0:
		print("No problems found, nice!")
		
	print('#######################################')
	print("Found", len(plugin_dir), "dlls. Errors:", error_count, "Notices:", len(notice))
	print()


if __name__ == "__main__":
	main(sys.argv)