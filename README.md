# WpfKenBurns
[![Build Status](https://img.shields.io/github/workflow/status/nicoco007/WpfKenBurns/build?style=flat-square)](https://github.com/nicoco007/WpfKenBurns/actions)
[![License](https://img.shields.io/github/license/nicoco007/WpfKenBurns?style=flat-square)](https://github.com/nicoco007/WpfKenBurns/blob/master/LICENSE)

A simple, no-nonsense image screensaver for Windows inspired by macOS's Ken Burns screensaver.

## Features
* Highly configurable
* Images can be sourced from many different folders
* Supports high DPI monitors and scaling
* Supports monitor hotplugging (useful for DisplayPort monitors)
* Program denylist to prevent the screensaver from running when certain programs are open

## Installing
Get [the latest build (usually stable)](https://nightly.link/nicoco007/WpfKenBurns/workflows/build/main/WpfKenBurns.zip) and copy it into `C:\Windows\System32` to install it system-wide. You will need to install the [.NET Runtime 6 for Desktop Apps](https://dotnet.microsoft.com/download/dotnet/6.0/runtime) if you do not already have it.

To enable and configure the screensaver, open the Settings app, navigate to _Personalization_ > _Lock Screen_, and select _Screen Saver_ in the additional settings section. In the Screen Saver Settings window that opens, choose "Ken Burns" from the drop-down list and press "Settings&hellip;" to configure the screensaver.
