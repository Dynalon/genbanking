PREFIX=/usr/local/
GWENHYWFAR_CFLAGS=$(shell pkg-config --cflags gwenhywfar)
GWENHYWFAR_LDFLAGS=$(shell pkg-config --libs gwenhywfar)
AQBANKING_CFLAGS=$(shell pkg-config --cflags aqbanking)
AQBANKING_LDFLAGS=$(shell pkg-config --libs aqbanking)
# we HAVE to avoid dots in the .so because windows does not append .dll if 
# there is already a dot in the name within the DllImport parameter
AQBANKING_VERSION=$(subst .,-,$(shell pkg-config --modversion aqbanking))
# get the MAJOR Version of aqbanking
AQBANKING_MAJOR=$(shell echo -n $(AQBANKING_VERSION) | cut -f1 -d'-')
NAMESPACE=AqBanking

SWIG=$(shell which swig)
GMCS=$(shell which gmcs)
CC=$(shell which cc)

# input swig interface file which contains wrapper specification
SWIG_INTERFACE=aqbanking.i

# name of target native wrapper library
WRAPPER_NAME=aqbankingNET$(AQBANKING_MAJOR)-native
# on linux mono expects the library to have a 'lib' prefix and .so suffix
WRAPPER_LIB=lib$(WRAPPER_NAME).so
CIL_NAME=aqbankingNET$(AQBANKING_MAJOR)
CIL_DLL=$(CIL_NAME).dll
BUILD_OUTPUT_PATH=bin/
CS_OUTPUT_PATH=csharp-tmp/


# Flags for compiling & linking into a shared .so
CFLAGS=-Wno-deprecated-declarations -fPIC -shared -Wl,-soname,$(WRAPPER_LIB)

all: checks gen_wrapper build_cswrapper build_wrapper 

checks:
	@which gmcs > /dev/null
	@which swig > /dev/null
	@which gcc > /dev/null
	@which pkg-config > /dev/null

gen_wrapper:	$(SWIG_INTERFACE)
	### Autogenerate C# wrappers from Aqbanking & Gwenhywfar headers
	@mkdir -p $(CS_OUTPUT_PATH) 
	$(SWIG) $(AQBANKING_CFLAGS) $(GWENHYWFAR_CFLAGS) \
		-outdir $(CS_OUTPUT_PATH) \
		-dllimport $(WRAPPER_NAME) \
		-namespace $(NAMESPACE) \
		-csharp $(SWIG_INTERFACE)

build_cswrapper:
	@mkdir -p $(BUILD_OUTPUT_PATH)
	$(GMCS) -t:library -keyfile:mono.snk -out:$(BUILD_OUTPUT_PATH)$(CIL_DLL) $(CS_OUTPUT_PATH)/*.cs  

build_wrapper:
	### Compiling the C wrapper libary: $(WRAPPER_LIB)
	$(CC) $(CFLAGS) $(GWENHYWFAR_CFLAGS) $(AQBANKING_CFLAGS) \
		$(GWENHYWFAR_LDFLAGS) $(AQBANKING_LDFLAGS) \
		-o $(BUILD_OUTPUT_PATH)$(WRAPPER_LIB) \
		aqbanking_wrap.c

install:
	### Copying $(WRAPPER_LIB) to $(PREFIX)/lib/
	cp $(BUILD_OUTPUT_PATH)/$(WRAPPER_LIB) $(PREFIX)/lib/
	### Installing $(CIL_DLL) to global assembly cache (GAC)	
	gacutil -package aqbankingNET -i $(BUILD_OUTPUT_PATH)/$(CIL_DLL)

uninstall:
	### removing $(PREFIX)/lib/$(WRAPPER_LIB)
	rm -rf $(PREFIX)/lib/$(WRAPPER_LIB)
	### Uninstalling $(CIL_NAME) from global assembly cache (GAC)	
	gacutil -u $(CIL_NAME)

clean: 
	@rm -rf *.o *.c $(CS_OUTPUT_PATH)
	@rm -rf *.so rm -rf *.dll *.tmp $(BUILD_OUTPUT_PATH)
