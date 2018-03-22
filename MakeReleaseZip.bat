set "project=WebChangeNotifier"
set "outDir=%cd%\ReleaseOutput\"
set "outAppDir=%cd%\ReleaseOutput\%project%\"
set "projectDir=%cd%\%project%\"
set "binDir=%projectDir%bin\Release\"

rmdir /s /q %outDir%

robocopy %bindir% %outAppDir% * /xf *.vshost.exe
robocopy %projectDir% %outAppDir% *.example.*
robocopy %cd% %outAppDir% *.md

rm %outAppDir%config.json
mv %outAppDir%config.example.json %outAppDir%config.json

pipenv run grip README.md --export %outAppDir%README.html --context=AlexP11223/WebChangeNotifier

cd  %outdir%\
"C:\Program Files\WinRAR\WinRAR.exe" a -r "%project%.zip"
cd ..