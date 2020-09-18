@echo off
SET DEBUG=false
SET PLATFORM=android

:loop
IF NOT "%1"=="" (
  @echo arg=%1
  if "%1"=="-debug" (
    SET DEBUG=true
  )
  if "%1"=="-platform" (
    SHIFT
    SET PLATFORM=%1
  )

  SHIFT
  GOTO :loop
  )


if %DEBUG%==true (
  @echo is debug
)

if NOT %DEBUG%==true (
  @echo not debug
)