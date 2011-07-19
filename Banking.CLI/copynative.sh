#!/bin/bash

# due to a bug (?) in mdtool we need to copy over the native .so wrapper
# use this script as custom command in monodevelop to run after build
# sh ${ProjectDir}/copynative.sh ${SolutionDir} ${TargetDir}
cp $1/Banking.Provider.AqBanking.Native/bin/*.so $2/
#echo $1/Banking.Provider.AqBanking.Native/bin/*.so $2/
