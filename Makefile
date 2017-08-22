.PHONY: clean all

PREFIX = /usr/local/lib

OUTPUT_DIR = ./bin

IODINE = $(OUTPUT_DIR)/iodine.exe

IODINE_DEPS += ./bin/LibIodine.dll

# These are the only CANONICAL modules in ./modules
# Anything else is experimental or outdated

IODINE_MODS += ./modules/argparse.id
IODINE_MODS += ./modules/base64.id
IODINE_MODS += ./modules/cryptoutils.id
IODINE_MODS += ./modules/collections.id
IODINE_MODS += ./modules/configfile.id
IODINE_MODS += ./modules/dis.id
IODINE_MODS += ./modules/events.id
IODINE_MODS += ./modules/ints.id
IODINE_MODS += ./modules/iterutils.id
IODINE_MODS += ./modules/functools.id
IODINE_MODS += ./modules/json.id
IODINE_MODS += ./modules/logging.id
IODINE_MODS += ./modules/semver.id
IODINE_MODS += ./modules/testing.id

IODINE_MODS += ./modules/_whirlpool.id

IODINE_NETMODS += ./modules/net/http.id
IODINE_NETMODS += ./modules/net/dns.id

all:
	mkdir -p $(OUTPUT_DIR)
	cd ./src/Iodine && nuget restore
	xbuild ./src/Iodine/Iodine.sln /p:Configuration=Release /p:DefineConstants="COMPILE_EXTRAS" /t:Build "/p:Mono=true;BaseConfiguration=Release"
install:
	mkdir -p $(PREFIX)/iodine/modules/net
	mkdir -p $(PREFIX)/iodine/extensions
	cp $(IODINE) $(PREFIX)/iodine/iodine.exe
	cp -f $(IODINE_DEPS) $(PREFIX)/iodine
	cp -f $(IODINE_MODS) $(PREFIX)/iodine/modules
	cp -f ./bin/extensions/*.dll $(PREFIX)/iodine/extensions
	cp -f $(IODINE_NETMODS) $(PREFIX)/iodine/modules/net
	echo "#! /bin/bash" > /bin/iodine
	echo "/usr/bin/mono $(PREFIX)/iodine/iodine.exe \"\$$@\"" >> /usr/bin/iodine
	chmod +x /bin/iodine

