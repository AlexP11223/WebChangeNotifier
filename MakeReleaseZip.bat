set "project=WebChangeNotifier"
set "outDir=%cd%\ReleaseOutput\"
set "outAppDir=%cd%\ReleaseOutput\%project%\"
set "projectDir=%cd%\%project%\"
set "binDir=%projectDir%bin\Release\"

rmdir /s /q %outDir%

:: copy files
robocopy %bindir% %outAppDir% * /xf *.vshost.exe
robocopy %projectDir% %outAppDir% *.example.*
robocopy %cd% %outAppDir% *.md

:: rename example config
rm %outAppDir%config.json
mv %outAppDir%config.example.json %outAppDir%config.json

:: generate HTML readme from Markdown
pipenv run grip README.md --export %outAppDir%README.html --context=AlexP11223/WebChangeNotifier

:: create ZIP archive
pipenv run python %cd%\tools\archive\archive.py %outDir%%project% %outDir% %project%
:: or call WinRAR/7zip/...
:: cd  %outdir%
:: "C:\Program Files\WinRAR\WinRAR.exe" a -r "%project%.zip"
:: cd ..