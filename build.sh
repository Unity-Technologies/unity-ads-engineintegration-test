#!/bin/bash

# NOTE: This is a test script used by Unity Ads team to verify Ads SDK versions
# against different Unity versions. Parameters and usage of this script may change
# in future versions, but is included here as example of how to automate this
# process, which might be relevant to others

if [ -z "$2" ]; then
    echo "Build Unity Ads SDK example project on OS X"
    echo
    echo "./build.sh platform(s) unity-path [Ads SDK url]"
    echo
    echo "Examples:"
    echo "  ./build.sh android \"/Applications/Unity 5.4.0f3\""
    echo "  ./build.sh android,ios \"/Applications/Unity 5.4.0f3\" http://cdn.unityads.unity3d.com/unitypackage/2.0.2/UnityAds.unitypackage"
    echo
    echo "Unity version e.g. \"5.4.0f3\" must be installed on the machine in specified folder"
    echo "If specifying SDK url, this will be downloaded and imported into the project before building"
    exit 1
fi

# parameters
PLATFORMS=$1
UNITY="$2/Unity.app/Contents/MacOS/Unity"
SDK_URL=$3

PROJECT_PATH="$(pwd)/UnityAdsAssetStoreTest"
EDITOR_LOG_MSG="Please check ~/Library/Logs/Unity/Editor.log"

if [ ! -f "$UNITY" ]; then
    echo "Could not find Unity executable in '$UNITY'. Please verify it's installed and available in that location"
    exit 1
fi

if [ ! -z "$SDK_URL" ]; then
    echo Downloading Ads SDK from $SDK_URL...
    curl -s -o UnityAds.unitypackage $SDK_URL
    rc=$?; if [[ $rc != 0 ]]; then
        echo "Failed to download package"
        exit $rc
    fi

    # Import package into project
    echo "Importing Ads SDK plugin..."
    "$UNITY" -projectPath "$PROJECT_PATH" -importPackage "$(pwd)/UnityAds.unitypackage" -batchMode -quit
    rc=$?; if [[ $rc != 0 ]]; then
        echo "Importing package failed. $EDITOR_LOG_MSG"
        exit $rc
    fi

    # UNITY_ADS define is used to be able to import plugin from command line without compile erros in project
    echo "Setting UNITY_ADS define..."
    "$UNITY" -projectPath "$PROJECT_PATH" -executeMethod AutoBuilder.EnableAds -batchMode -quit
    rc=$?; if [[ $rc != 0 ]]; then
        echo "Defining UNITY_ADS failed. $EDITOR_LOG_MSG"
        exit $rc
    fi
fi

if [[ $PLATFORMS =~ .*android.* ]]; then
    echo Building project for Android...
    APK_PATH="UnityAdsAssetStoreTest/Builds/Android.apk"
    if [ -f "$APK_PATH" ]; then
        rm "$APK_PATH"
    fi
    "$UNITY" -projectPath "$PROJECT_PATH" -executeMethod AutoBuilder.PerformAndroidBuild -batchMode -quit
    rc=$?; if [[ $rc != 0 ]]; then
        echo "Unity build for Android failed. $EDITOR_LOG_MSG"
        exit $rc
    fi

    if [ ! -f "$APK_PATH" ]; then
        echo "Failed to build APK file ($APK_PATH). $EDITOR_LOG_MSG"
        exit 1
    fi

    # TODO: Deploy to device
    # echo Installing on Android device...
    # adb install -r Builds/Android.apk
    # adb shell am start -S -a android.intent.action.MAIN -n com.unity3d.UnityAdsAssetStoreTest/com.unity3d.player.UnityPlayerActivity
fi

if [[ $PLATFORMS =~ .*ios.* ]]; then
    echo Building project for iOS...
    IOS_PATH="UnityAdsAssetStoreTest/Builds/iOS"
    if [ -d "$IOS_PATH" ]; then
        rm -rf "$IOS_PATH"
    fi
    "$UNITY" -projectPath "$PROJECT_PATH" -executeMethod AutoBuilder.PerformiOSBuild -batchMode -quit
    rc=$?; if [[ $rc != 0 ]]; then
        echo "Unity build for iOS failed. $EDITOR_LOG_MSG"
        exit $rc
    fi

    XCODEPROJ_PATH="$IOS_PATH/Unity-iPhone.xcodeproj"
    if [ ! -d "$XCODEPROJ_PATH" ]; then
        echo "Failed to build iOS project ($XCODEPROJ_PATH). $EDITOR_LOG_MSG"
        exit 1
    fi

    # TODO: Deploy to device
    # open Builds/iOS/Unity-iPhone.xcodeproj
    # xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj
fi
