# Voltcraft DL-120TH Management Tool
The Temperature and Humidity Logger DL-120TH was sold by Conrad Electronics,
but the available software and driver are not compatible with Windows 10.

This package contains:
- command line tool vdl120cli
- self-signed driver package for Windows 10

## Installation
### Download the latest release zip file. Unpack it to desired installation folder.
### Install the self-signed certificate
- open a Command Prompt wit Administrator priviledges.
- change to installation folder.
- run `certutil -addstore root .\Driver\farcast.cer` or manually import it to Trusted Root Certification Authorities
- plug in the logger	
- run `pnputil -i -a driver\vdl120.inf` or manually install the Driver using Device Manager
### Using the command line interface
- open a Command Prompt
- change to installation folder.
- plug in the logger
- run `vdl120cli.exe` without options to show the call syntax

