# Voltcraft DL-120TH Management Tool
The Temperature and Humidity Logger DL-120TH was sold by Conrad Electronics,
but the available software and driver are not compatible with Windows 10.

This package contains:
- command line tool vdl120cli
- self-signed driver package for Windows 10

## Installation
### Download the latest release
Click on the latest release and download the `vdl120.zip` file.
Unpack the zip into desired installation folder.

### Install the self-signed certificate
- open a Command Prompt with Administrator privileges.
- change to installation folder.
- run `certutil -addstore root .\Driver\farcast.cer` or manually import it to Trusted Root Certification Authorities

### Install the WinUSB driver package
- plug in the logger	
- run `pnputil -i -a .\Driver\vdl120.inf` or manually install the Driver using Device Manager

### Using the command line interface
- open a Command Prompt
- change to installation folder.
- plug in the logger
- run `vdl120cli.exe` without options to show the call syntax

