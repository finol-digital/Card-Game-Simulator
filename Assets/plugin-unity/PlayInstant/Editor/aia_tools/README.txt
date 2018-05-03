Prerequisites
=============
This Instant Apps CLI requires the Java Runtime Environment version 1.8
or greater ("Java 8"). Compatible JREs can be found at the following websites:

Oracle:     https://www.java.com/
OpenJDK:    http://openjdk.java.net/

Java also needs to be listed in the system PATH. Check the documentation for
your version of the JRE on how to do this.

Installation
============
Unzip the archive into a convenient place on your system. All files in the
distribution must be kept together for the CLI to work properly.

MacOS and Linux
---------------
And add the "aia" file to your PATH. In Bash:
    echo 'export PATH="$PATH:<full path of aia>"' >> ~/.bashrc && . ~/.bashrc

Windows
-------
Add "aia.bat" to your PATH:
    setx PATH "%PATH%;<full path of aia.bat>"

Usage
=====
The aia program has a built-in help describing the available commands and
options. You can view it by running "aia help" from the command line.

Exit Codes
==========
The exit code of the aia command indicates whether it completed successfully.
The following codes are defined:

0   Success
1   General runtime error
2   Invalid command line or argument
3   "aia check" completed, but one or more errors were found with the app
