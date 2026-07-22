if exist "C:\Users\ulyss\Downloads\Atlus-Script-Tools\AtlusScriptCompiler.exe" (
    set compiler="C:\Users\ulyss\Downloads\Atlus-Script-Tools\AtlusScriptCompiler.exe"
) else (
    set compiler="G:\Tools\Atlus Script Tools\AtlusScriptCompiler.exe"
)

echo "Building BF Files"

for %%f in (.//src//*.flow) do (
    %compiler% ./src/%%f -Compile -Out ./bin/%%~nf.bf -OutFormat V3BE -Library P5R -Encoding P5R
    
    set destFile=./src/%%~nf.dst
    call echo %%destFile%%
    setlocal enableDelayedExpansion
    if exist !destFile! (
        for /f "delims=" %%x in (!destFile!) do set localDest=%%x
        set "localDest=!localDest:*/=/!"
        set dest="../P5REssentials!localDest!"
        set "winDest=!dest:/=\!"
        if not exist "!winDest!" mkdir "!winDest!"
        copy /Y "bin\%%~nf.bf" "!winDest!"
    )
    endlocal
)