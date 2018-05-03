@echo off
:: Wrapper script for launching the AIA command line tool in Windows

where java >nul 2>nul
if %errorlevel% NEQ 0 (
  echo ERROR - The java command was not found.
  echo Ensure that you have installed the Java Runtime Environment, and
  echo that its location has been added to your PATH.
  exit /b 1
)

java -jar %~dp0\aia.jar %*
