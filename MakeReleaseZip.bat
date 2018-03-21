set "project=WebChangeNotifier"
set "outDir=%cd%\ReleaseOutput\"
set "outAppDir=%cd%\ReleaseOutput\%project%\"
set "projectDir=%cd%\%project%\"
set "binDir=%projectDir%bin\Release\"

rmdir /s /q %outDir%

robocopy %bindir% %outAppDir% * /xf *.vshost.exe
robocopy %projectDir% %outAppDir% *.example

rm %outAppDir%config.json
mv %outAppDir%config.json.example %outAppDir%config.json

cd  %outdir%\
"C:\Program Files\WinRAR\WinRAR.exe" a -r "%project%.zip"
cd ..