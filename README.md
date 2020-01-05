![](https://github.com/LoL-Fantome/LeagueFileTranslator/blob/master/images/leaguefiletranslatorbanner.png)

# Introduction
LeagueFileTranslator is a plugin for Maya 2019 that allows you to import and export League of Legends models, animations and more.

# Requirements
* **Maya2019** is required for the plugin. In the future other versions of Maya might be supported.

# **How to install**


# How to build
## Step 1
You will need to download [Maya 2019](https://www.autodesk.com/education/free-software/maya) and the [Maya 2019 DevKit](https://s3-us-west-2.amazonaws.com/autodesk-adn-transfer/ADN+Extranet/M%26E/Maya/devkit+2019/Autodesk_Maya_2019_DEVKIT_Windows.zip)

## Step 2
After you've downloaded the DevKit, you'll need to copy the folders in the **devkitBase** folder to your **Maya 2019 installation directory** (should be **C:\Program Files\Autodesk\Maya2019**)

## Step 3
You will need to add the following environment variables to your OS:

**MAYA_LOCATION** - Path to your Maya installation

**DEVKIT_LOCATION** - Path to the Maya devkit

![](https://github.com/LoL-Fantome/LeagueFileTranslator/blob/master/images/envrionmentvariables.png)

You can do this by right clicking on **This PC** in Explorer and clicking **Properties**, after that you'll need to click **Advanced System Settings** on the left and add the respective environemnt variables.

## Step 4
After all this is done you should be good to go and build the plugin. Make sure to always build release, so Maya is able to load it properly.
